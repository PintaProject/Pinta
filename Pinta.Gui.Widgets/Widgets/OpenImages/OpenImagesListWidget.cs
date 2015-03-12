// 
// OpenImagesListWidget.cs
//  
// Author:
//       Cameron White <cameronwhite91@gmail.com>
// 
// Copyright (c) 2011 Cameron White
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class OpenImagesListWidget : ScrolledWindow
	{
		private TreeView tree;
		private ListStore store;
		private CanvasRenderer canvas_renderer;

		private CellRendererPixbuf file_close_cell;
		private TreeViewColumn file_name_column;
		private TreeViewColumn file_preview_column;
		private TreeViewColumn file_close_column;

		private const int PreviewWidth = 60;
		private const int PreviewHeight = 40;
		private const int PreviewColumnWidth = 70;
		private const int CloseColumnWidth = 30;

		private const int FilePreviewColumnIndex = 0;
		private const int FileNameColumnIndex = 1;
		private const int FileCloseColumnIndex = 2;

		private Gdk.Pixbuf close_icon = PintaCore.Resources.GetIcon (Stock.Close);

		public OpenImagesListWidget ()
		{
			CanFocus = false;
			SetSizeRequest (200, 200);
			SetPolicy (PolicyType.Automatic, PolicyType.Automatic);

			canvas_renderer = new CanvasRenderer (false);

			tree = new TreeView ();
			tree.CanFocus = false;
			tree.HeadersVisible = false;
			tree.EnableGridLines = TreeViewGridLines.None;
			tree.Selection.Mode = SelectionMode.Single;
			tree.Selection.SelectFunction = HandleDocumentSelected;

			var file_preview_cell = new CellRendererSurface (PreviewWidth, PreviewHeight);
			file_preview_column = new TreeViewColumn ("File Preview", file_preview_cell, "surface", FilePreviewColumnIndex);
			file_preview_column.Sizing = TreeViewColumnSizing.Fixed;
			file_preview_column.FixedWidth = PreviewColumnWidth;
			tree.AppendColumn (file_preview_column);

			var textCell = new CellRendererText ();
			textCell.Ellipsize = Pango.EllipsizeMode.End;
			file_name_column = new TreeViewColumn ("File Name", textCell, "text", FileNameColumnIndex);
			file_name_column.Expand = true;
			tree.AppendColumn (file_name_column);

			file_close_cell = new CellRendererPixbuf ();
			file_close_column = new TreeViewColumn ("Close File", file_close_cell, "pixbuf", FileCloseColumnIndex);
			file_close_column.Sizing = TreeViewColumnSizing.Fixed;
			file_close_column.FixedWidth = CloseColumnWidth;
			tree.AppendColumn (file_close_column);

			store = new ListStore (typeof (Cairo.ImageSurface), typeof (string), typeof (Gdk.Pixbuf));
			tree.Model = store;
			tree.ButtonPressEvent += HandleTreeButtonPressEvent;

			Add (tree);
			ShowAll ();

			PintaCore.Workspace.DocumentOpened += HandleDocumentOpenedOrClosed;
			PintaCore.Workspace.DocumentClosed += HandleDocumentOpenedOrClosed;
			PintaCore.Workspace.DocumentCreated += HandleDocumentOpenedOrClosed;
			PintaCore.Workspace.ActiveDocumentChanged += HandleActiveDocumentChanged;

			// update the thumbnails whenever the image is modified
			PintaCore.History.HistoryItemAdded += HandleDocumentModified;
			PintaCore.History.ActionRedone += HandleDocumentModified;
			PintaCore.History.ActionUndone += HandleDocumentModified;
		}

		/// <summary>
		/// Update the preview image for a modified document.
		/// </summary>
		void HandleDocumentModified (object sender, EventArgs e)
		{
			int docIndex = PintaCore.Workspace.ActiveDocumentIndex;

			if (docIndex != -1)
			{
				TreeIter iter;
				if (store.GetIter (out iter, new TreePath (new int[] { docIndex })))
				{
					var surface = (Cairo.ImageSurface)store.GetValue (iter, FilePreviewColumnIndex);
					(surface as IDisposable).Dispose ();

					store.SetValue (iter, FilePreviewColumnIndex, CreateImagePreview (PintaCore.Workspace.ActiveDocument));
				}
			}
		}

		/// <summary>
		/// Attempt to close the selected document if the close button is clicked
		/// </summary>
		[GLib.ConnectBefore]
		void HandleTreeButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			double click_x = args.Event.X;
			double click_y = args.Event.Y;

			int start_pos, width;
			file_close_column.CellGetPosition (file_close_cell, out start_pos, out width);

			start_pos += file_preview_column.Width + file_name_column.Width;

			// if the close button was clicked, find the row that was clicked and close that document
			if (start_pos <= click_x && start_pos + width > click_x)
			{
				TreePath path;
				if (tree.GetPathAtPos ((int)click_x, (int)click_y, out path))
                {
                    PintaCore.Workspace.SetActiveDocument (path.Indices[0]);
                    PintaCore.Actions.File.Close.Activate ();
                    UpdateSelectedDocument ();
                }
			}
		}

		/// <summary>
		/// If the active document is changed elsewhere, update the selected document in this widget
		/// </summary>
		private void HandleActiveDocumentChanged (object sender, EventArgs e)
		{
			UpdateSelectedDocument ();
		}

		private void UpdateSelectedDocument ()
		{
			if (PintaCore.Workspace.HasOpenDocuments)
			{
				int doc_index = PintaCore.Workspace.ActiveDocumentIndex;
			
				if (doc_index != -1)
				{
					var path = new TreePath (new int[] { doc_index });
					tree.Selection.SelectPath (path);
				}
			}
		}

		private void RebuildDocumentList ()
		{
			// Ensure that the old image previews are disposed.
			foreach (object[] row in store)
			{
				var imageSurface = (Cairo.ImageSurface)row[FilePreviewColumnIndex];
				(imageSurface as IDisposable).Dispose ();
			}

			store.Clear ();

			foreach (Document doc in PintaCore.Workspace.OpenDocuments)
			{
				doc.Renamed -= HandleDocRenamed;
				doc.Renamed += HandleDocRenamed;

				store.AppendValues (CreateImagePreview (doc), doc.Filename, close_icon);
			}
		}

		/// <summary>
		/// Creates a thumbnail image preview of a document.
		/// </summary>
		private Cairo.ImageSurface CreateImagePreview (Document doc)
		{
			var surface = new Cairo.ImageSurface (Cairo.Format.Argb32, PreviewWidth, PreviewHeight);
			canvas_renderer.Initialize (doc.ImageSize, new Gdk.Size (PreviewWidth, PreviewHeight));
			canvas_renderer.Render (doc.GetLayersToPaint (), surface, Gdk.Point.Zero);
			return surface;
		}

		/// <summary>
		/// Rebuilds the list of documents after a document is opened or closed
		/// </summary>
		private void HandleDocumentOpenedOrClosed (object sender, DocumentEventArgs e)
		{
			RebuildDocumentList ();
		}

		/// <summary>
		/// If a document is renamed, just rebuild the list of open documents
		/// </summary>
		private void HandleDocRenamed (object sender, EventArgs e)
		{
			RebuildDocumentList ();
			UpdateSelectedDocument ();
		}

		/// <summary>
		/// Sets the active document as selected by the user
		/// </summary>
		private bool HandleDocumentSelected (TreeSelection selection, TreeModel model, TreePath path, bool path_currently_selected)
		{
			int index = path.Indices[0];

			if (!path_currently_selected && index != PintaCore.Workspace.ActiveDocumentIndex)
			{
				PintaCore.Workspace.SetActiveDocument (index);
			}

			return true;
		}
	}
}

