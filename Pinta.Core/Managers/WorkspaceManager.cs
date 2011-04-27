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
using System.Linq;
using Cairo;
using Mono.Unix;
using System.Collections.Generic;
using Gtk;

namespace Pinta.Core
{
	public class WorkspaceManager
	{
		private int active_document_index = -1;
		private int new_file_name = 1;
		
		public WorkspaceManager ()
		{
			OpenDocuments = new List<Document> ();
		}
		
		public Document ActiveDocument {
			get {
				if (HasOpenDocuments)
					return OpenDocuments[active_document_index];
				
				throw new InvalidOperationException ("Tried to get WorkspaceManager.ActiveDocument when there are no open Documents.  Check HasOpenDocuments first.");
			}
		}

		public DocumentWorkspace ActiveWorkspace {
			get {
				if (HasOpenDocuments)
					return OpenDocuments[active_document_index].Workspace;

				throw new InvalidOperationException ("Tried to get WorkspaceManager.ActiveWorkspace when there are no open Documents.  Check HasOpenDocuments first.");
			}
		}

		public Gdk.Size ImageSize {
			get { return ActiveDocument.ImageSize; }
			set { ActiveDocument.ImageSize = value; }
		}

		public Gdk.Size CanvasSize {
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
		
		public Document CreateAndActivateDocument (string filename, Gdk.Size size)
		{
			Document doc = new Document (size);
			
			if (string.IsNullOrEmpty (filename))
				doc.Filename = string.Format (Catalog.GetString ("Unsaved Image {0}"), new_file_name++);
			else
				doc.PathAndFileName = filename;
			
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
			if (PintaCore.Workspace.HasOpenDocuments)
				ActiveWorkspace.Invalidate ();
			else
				OnCanvasInvalidated (new CanvasInvalidatedEventArgs ());
		}
		
		public void Invalidate (Gdk.Rectangle rect)
		{
			ActiveWorkspace.Invalidate (rect);
		}

		public Document NewDocument (Gdk.Size imageSize, bool transparent)
		{
			Document doc = CreateAndActivateDocument (null, imageSize);
			doc.Workspace.CanvasSize = imageSize;

			// Start with an empty white layer
			Layer background = doc.AddNewLayer (Catalog.GetString ("Background"));

			if (!transparent) {
				using (Cairo.Context g = new Cairo.Context (background.Surface)) {
					g.SetSourceRGB (1, 1, 1);
					g.Paint ();
				}
			}

			doc.Workspace.History.PushNewItem (new BaseHistoryItem (Stock.New, Catalog.GetString ("New Image")));
			doc.IsDirty = false;

			PintaCore.Actions.View.ZoomToWindow.Activate ();

			return doc;
		}

		// TODO: Standardize add to recent files
		public bool OpenFile (string file)
		{
			bool fileOpened = false;

			try {
				// Open the image and add it to the layers
				IImageImporter importer = PintaCore.System.ImageFormats.GetImporterByFile (file);
				importer.Import (file);

				PintaCore.Workspace.ActiveDocument.PathAndFileName = file;
				PintaCore.Workspace.ActiveWorkspace.History.PushNewItem (new BaseHistoryItem (Stock.Open, Catalog.GetString ("Open Image")));
				PintaCore.Workspace.ActiveDocument.IsDirty = false;
				PintaCore.Workspace.ActiveDocument.HasFile = true;
				PintaCore.Actions.View.ZoomToWindow.Activate ();
				PintaCore.Workspace.Invalidate ();

				fileOpened = true;
			} catch {
				MessageDialog md = new MessageDialog (PintaCore.Chrome.MainWindow, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, Catalog.GetString ("Could not open file: {0}"), file);
				md.Title = Catalog.GetString ("Error");

				md.Run ();
				md.Destroy ();
			}

			return fileOpened;
		}
		
		public void ResizeImage (int width, int height)
		{
			ActiveDocument.ResizeImage (width, height);
		}
		
		public void ResizeCanvas (int width, int height, Anchor anchor)
		{
			ActiveDocument.ResizeCanvas (width, height, anchor);
		}
		
		public Cairo.PointD WindowPointToCanvas (double x, double y)
		{
			return ActiveWorkspace.WindowPointToCanvas (x, y);
		}

		public Gdk.Rectangle ClampToImageSize (Gdk.Rectangle r)
		{
			return ActiveDocument.ClampToImageSize (r);
		}

		public bool ImageFitsInWindow {
			get { return ActiveWorkspace.ImageFitsInWindow; }
		}
		
		internal void ResetTitle ()
		{
			if (HasOpenDocuments)
				PintaCore.Chrome.MainWindow.Title = string.Format ("{0}{1} - Pinta", ActiveDocument.Filename, ActiveDocument.IsDirty ? "*" : "");
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
			RadioAction action = PintaCore.Actions.Window.OpenWindows.Where (p => p.Name == document.Guid.ToString ()).FirstOrDefault ();

			if (action == null)
				throw new ArgumentOutOfRangeException ("Tried to WorkspaceManager.SetActiveDocument.  Could not find document.");

			action.Activate ();
		}

		internal void SetActiveDocumentInternal (Document document)
		{
			// Work around a case where we closed a document but haven't updated
			// the active_document_index yet and it points to the closed document
			if (HasOpenDocuments && OpenDocuments.Count > active_document_index)
				PintaCore.Tools.Commit ();

			int index = OpenDocuments.IndexOf (document);
			active_document_index = index;

			OnActiveDocumentChanged (EventArgs.Empty);
		}
		
		#region Protected Methods
		protected void OnActiveDocumentChanged (EventArgs e)
		{
			if (ActiveDocumentChanged != null)
				ActiveDocumentChanged (this, EventArgs.Empty);
				
			ResetTitle ();
		}
		
		protected internal void OnCanvasInvalidated (CanvasInvalidatedEventArgs e)
		{
			if (CanvasInvalidated != null)
				CanvasInvalidated (this, e);
		}

		public void OnCanvasSizeChanged ()
		{
			if (CanvasSizeChanged != null)
				CanvasSizeChanged (this, EventArgs.Empty);
		}

		protected internal void OnDocumentCreated (DocumentEventArgs e)
		{
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
		#endregion

		#region Public Events
		public event EventHandler ActiveDocumentChanged;
		public event EventHandler<CanvasInvalidatedEventArgs> CanvasInvalidated;
		public event EventHandler CanvasSizeChanged;
		public event EventHandler<DocumentEventArgs> DocumentCreated;
		public event EventHandler<DocumentEventArgs> DocumentOpened;
		public event EventHandler<DocumentEventArgs> DocumentClosed;
		#endregion
		
	}
}
