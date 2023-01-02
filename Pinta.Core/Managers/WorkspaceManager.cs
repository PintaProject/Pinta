// 
// WorkspaceManager.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using Gtk;

namespace Pinta.Core
{
	public interface IWorkspaceService
	{
		Document ActiveDocument { get; }
		DocumentWorkspace ActiveWorkspace { get; }
		event EventHandler? ActiveDocumentChanged;
		RectangleI ClampToImageSize (RectangleI r);
		bool HasOpenDocuments { get; }
		event EventHandler? SelectionChanged;
		SelectionModeHandler SelectionHandler { get; }
	}

	public class WorkspaceManager : IWorkspaceService
	{
		private int active_document_index = -1;
		private int new_file_name = 1;

		public WorkspaceManager ()
		{
			OpenDocuments = new List<Document> ();
			SelectionHandler = new SelectionModeHandler ();
		}

		public int ActiveDocumentIndex {
			get {
				return active_document_index;
			}
		}

		public Document ActiveDocument {
			get {
				if (HasOpenDocuments)
					return OpenDocuments[active_document_index];

				throw new InvalidOperationException ("Tried to get WorkspaceManager.ActiveDocument when there are no open Documents.  Check HasOpenDocuments first.");
			}
		}

		public Document? ActiveDocumentOrDefault => HasOpenDocuments ? OpenDocuments[active_document_index] : null;

		public SelectionModeHandler SelectionHandler { get; private set; }

		public DocumentWorkspace ActiveWorkspace {
			get {
				if (HasOpenDocuments)
					return OpenDocuments[active_document_index].Workspace;

				throw new InvalidOperationException ("Tried to get WorkspaceManager.ActiveWorkspace when there are no open Documents.  Check HasOpenDocuments first.");
			}
		}

		public Size ImageSize {
			get { return ActiveDocument.ImageSize; }
			set { ActiveDocument.ImageSize = value; }
		}

		public Size CanvasSize {
			get { return ActiveWorkspace.CanvasSize; }
			set { ActiveWorkspace.CanvasSize = value; }
		}

		public PointD Offset {
			get { return ActiveWorkspace.Offset; }
		}

		public double Scale {
			get { return ActiveWorkspace.Scale; }
			set { ActiveWorkspace.Scale = value; }
		}

		public List<Document> OpenDocuments { get; private set; }
		public bool HasOpenDocuments { get { return OpenDocuments.Count > 0; } }

		public Document CreateAndActivateDocument (Gio.File? file, Size size)
		{
			Document doc = new Document (size);

			if (file is not null)
				doc.File = file;
			else
				doc.DisplayName = Translations.GetString ("Unsaved Image {0}", new_file_name++);

			OpenDocuments.Add (doc);
			OnDocumentCreated (new DocumentEventArgs (doc));

			SetActiveDocument (doc);

			return doc;
		}

		public void CloseActiveDocument ()
		{
			CloseDocument (ActiveDocument);
		}

		public void CloseDocument (Document document)
		{
			int index = OpenDocuments.IndexOf (document);
			OpenDocuments.Remove (document);

			if (index == active_document_index) {
				// If there's other documents open, switch to one of them
				if (HasOpenDocuments) {
					if (index > 0)
						SetActiveDocument (index - 1);
					else
						SetActiveDocument (index);
				} else {
					active_document_index = -1;
					OnActiveDocumentChanged (EventArgs.Empty);
				}
			}

			document.Close ();

			OnDocumentClosed (new DocumentEventArgs (document));
		}

		public void Invalidate ()
		{
			if (HasOpenDocuments)
				ActiveWorkspace.Invalidate ();
		}

		public void Invalidate (RectangleI rect)
		{
			ActiveWorkspace.Invalidate (rect);
		}

		public void InvalidateWindowRect (RectangleI windowRect)
		{
			ActiveWorkspace.InvalidateWindowRect (windowRect);
		}

		public Document NewDocument (Size imageSize, Color backgroundColor)
		{
			Document doc = CreateAndActivateDocument (null, imageSize);
			doc.Workspace.CanvasSize = imageSize;

			// Start with an empty white layer
			Layer background = doc.Layers.AddNewLayer (Translations.GetString ("Background"));

			if (backgroundColor.A != 0) {
				var g = new Cairo.Context (background.Surface);
				g.SetSourceColor (backgroundColor);
				g.Paint ();
			}

			doc.Workspace.History.PushNewItem (new BaseHistoryItem (Resources.StandardIcons.DocumentNew, Translations.GetString ("New Image")));
			doc.Workspace.History.SetClean ();

			return doc;
		}

		/// <summary>
		/// Creates a new Document with a specified image as content.
		/// Primarily used for Paste Into New Image.
		/// </summary>
		public Document NewDocumentFromImage (GdkPixbuf.Pixbuf image)
		{
			var doc = NewDocument (new Size (image.Width, image.Height), new Color (0, 0, 0, 0));

			var g = new Context (doc.Layers[0].Surface);
			g.DrawPixbuf (image, 0, 0);

			// A normal document considers the "New Image" history to not be dirty, as it's just a
			// blank background. We put an image there, so we should try to save if the user closes it.
			doc.Workspace.History.SetDirty ();

			return doc;
		}

		// TODO: Standardize add to recent files
		public bool OpenFile (Gio.File file, Window? parent = null)
		{
#if false // TODO-GTK4
			bool fileOpened = false;

			if (parent == null)
				parent = PintaCore.Chrome.MainWindow;

			try {
				// Open the image and add it to the layers
				IImageImporter? importer = PintaCore.ImageFormats.GetImporterByFile (file.GetDisplayName ());
				if (importer is not null) {
					importer.Import (file, parent);
				} else {
					// Unknown extension, so try every loader.
					var errors = new StringBuilder ();
					bool loaded = false;
					foreach (var format in PintaCore.ImageFormats.Formats.Where (f => !f.IsWriteOnly ())) {
						try {
							format.Importer!.Import (file, parent);
							loaded = true;
							break;
						} catch (UnauthorizedAccessException) {
							// If the file can't be accessed, don't retry for every format.
							ShowFilePermissionErrorDialog (parent, file.ParsedName);
							return false;
						} catch (Exception e) {
							// Record errors in case none of the formats work.
							errors.AppendLine ($"Failed to load image as {format.Filter.Name}:");
							errors.Append (e.ToString ());
							errors.AppendLine ();
						}
					}

					if (!loaded) {
						ShowUnsupportedFormatDialog (parent, file.ParsedName,
							Translations.GetString ("Unsupported file format"), errors.ToString ());
						return false;
					}
				}

				PintaCore.Workspace.ActiveWorkspace.History.PushNewItem (new BaseHistoryItem (Resources.StandardIcons.DocumentOpen, Translations.GetString ("Open Image")));
				PintaCore.Workspace.ActiveDocument.History.SetClean ();

				fileOpened = true;
			} catch (UnauthorizedAccessException) {
				ShowFilePermissionErrorDialog (parent, file.ParsedName);
			} catch (Exception e) {
				ShowOpenFileErrorDialog (parent, file.ParsedName, e.Message, e.ToString ());
			}

			return fileOpened;
#else
			throw new NotImplementedException ();
#endif
		}

		public void ResizeImage (int width, int height)
		{
			ActiveDocument.ResizeImage (width, height);
		}

		public void ResizeCanvas (int width, int height, Anchor anchor, CompoundHistoryItem? compoundAction)
		{
			ActiveDocument.ResizeCanvas (width, height, anchor, compoundAction);
		}

		/// <summary>
		/// Converts a point from the active document's window coordinates to canvas coordinates.
		/// </summary>
		/// <param name='canvas_pos'>
		/// The position of the window point
		/// </param>
		public PointD WindowPointToCanvas (PointD window_pos)
		{
			return ActiveWorkspace.WindowPointToCanvas (window_pos.X, window_pos.Y);
		}

		/// <summary>
		/// Converts a point from the active document's canvas coordinates to window coordinates.
		/// </summary>
		/// <param name='canvas_pos'>
		/// The position of the canvas point
		/// </param>
		public PointD CanvasPointToWindow (PointD canvas_pos)
		{
			return ActiveWorkspace.CanvasPointToWindow (canvas_pos.X, canvas_pos.Y);
		}

		public RectangleI ClampToImageSize (RectangleI r)
		{
			return ActiveDocument.ClampToImageSize (r);
		}

		public bool ImageFitsInWindow {
			get { return ActiveWorkspace.ImageFitsInWindow; }
		}

		internal void ResetTitle ()
		{
			if (HasOpenDocuments)
				PintaCore.Chrome.MainWindow.Title = string.Format ("{0}{1} - Pinta", ActiveDocument.DisplayName, ActiveDocument.IsDirty ? "*" : "");
			else
				PintaCore.Chrome.MainWindow.Title = "Pinta";
		}

		public void SetActiveDocument (int index)
		{
			if (index >= OpenDocuments.Count)
				throw new ArgumentOutOfRangeException ("Tried to WorkspaceManager.SetActiveDocument greater than OpenDocuments.");
			if (index < 0)
				throw new ArgumentOutOfRangeException ("Tried to WorkspaceManager.SetActiveDocument less that zero.");

			SetActiveDocument (OpenDocuments[index]);
		}

		public void SetActiveDocument (Document document)
		{
			PintaCore.Actions.Window.SetActiveDocument (document);
		}

		internal void SetActiveDocumentInternal (Document document)
		{
#if false // TODO-GTK4
			// Work around a case where we closed a document but haven't updated
			// the active_document_index yet and it points to the closed document
			if (HasOpenDocuments && active_document_index != -1 && OpenDocuments.Count > active_document_index)
				PintaCore.Tools.Commit ();
#endif

			int index = OpenDocuments.IndexOf (document);
			active_document_index = index;

			OnActiveDocumentChanged (EventArgs.Empty);
		}

		#region Protected Methods
		protected void OnActiveDocumentChanged (EventArgs e)
		{
			if (ActiveDocumentChanged != null)
				ActiveDocumentChanged (this, EventArgs.Empty);

			OnSelectionChanged ();

			ResetTitle ();
		}

		protected internal void OnDocumentCreated (DocumentEventArgs e)
		{
			e.Document.SelectionChanged += (sender, args) => {
				OnSelectionChanged ();
			};

			if (DocumentCreated != null)
				DocumentCreated (this, e);
		}

		protected internal void OnDocumentOpened (DocumentEventArgs e)
		{
			if (DocumentOpened != null)
				DocumentOpened (this, e);
		}

		protected internal void OnDocumentClosed (DocumentEventArgs e)
		{
			if (DocumentClosed != null)
				DocumentClosed (this, e);
		}

		private void OnSelectionChanged ()
		{
			if (SelectionChanged != null)
				SelectionChanged.Invoke (this, EventArgs.Empty);
		}
		#endregion

#if false // TODO-GTK4

		private void ShowOpenFileErrorDialog (Window parent, string filename, string primaryText, string details)
		{
			string markup = "<span weight=\"bold\" size=\"larger\">{0}</span>\n\n{1}";
			string secondaryText = string.Format (Translations.GetString ("Could not open file: {0}"), filename);
			string message = string.Format (markup, primaryText, secondaryText);
			PintaCore.Chrome.ShowErrorDialog (parent, message, details);
		}

		private void ShowUnsupportedFormatDialog (Window parent, string filename, string primaryText, string details)
		{
			string markup = "<span weight=\"bold\" size=\"larger\">{0}</span>\n\n{1}";

			string secondaryText = Translations.GetString ("Could not open file: {0}", filename);
			secondaryText += string.Format ("\n\n{0}\n", Translations.GetString ("Pinta supports the following file formats:"));
			var extensions = from format in PintaCore.ImageFormats.Formats
					 where format.Importer != null
					 from extension in format.Extensions
					 where char.IsLower (extension.FirstOrDefault ())
					 orderby extension
					 select extension;

			secondaryText += String.Join (", ", extensions);

			string message = string.Format (markup, primaryText, secondaryText);
			PintaCore.Chrome.ShowUnsupportedFormatDialog (parent, message, details);
		}
#endif

		private void ShowFilePermissionErrorDialog (Window parent, string filename)
		{
			string markup = "<span weight=\"bold\" size=\"larger\">{0}</span>\n\n{1}";
			string primary = Translations.GetString ("Failed to open image");
			// Translators: {0} is the name of a file that the user does not have permission to open.
			string secondary = Translations.GetString ("You do not have access to '{0}'.", filename);
			string message = string.Format (markup, primary, secondary);

#if false // TODO-GTK4 need bindings from gir.core
			using var md = MessageDialog.New (parent, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, message);
			md.Run ();
#else
			throw new NotImplementedException ();
#endif
		}

		#region Public Events
		public event EventHandler? ActiveDocumentChanged;
		public event EventHandler<DocumentEventArgs>? DocumentCreated;
		public event EventHandler<DocumentEventArgs>? DocumentOpened;
		public event EventHandler<DocumentEventArgs>? DocumentClosed;
		public event EventHandler? SelectionChanged;
		#endregion

	}
}
