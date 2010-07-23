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
using Cairo;
using Mono.Unix;
using System.Collections.Generic;

namespace Pinta.Core
{
	public class WorkspaceManager
	{
		private int active_document_index = -1;
		
		public WorkspaceManager ()
		{
			OpenDocuments = new List<Document> ();
			CreateAndActivateDocument ();
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

		public string DocumentPath {
			get { return ActiveDocument.Pathname; }
			set { ActiveDocument.Pathname = value; }
		}
		
		public string Filename {
			get { return ActiveDocument.Filename; }
			set { ActiveDocument.Filename = value; }
		}
		
		public Gdk.Size ImageSize {
			get { return ActiveDocument.ImageSize; }
			set { ActiveDocument.ImageSize = value; }
		}

		public Gdk.Size CanvasSize {
			get { return ActiveWorkspace.CanvasSize; }
			set { ActiveWorkspace.CanvasSize = value; }
		}
		
		public bool IsDirty {
			get { return ActiveDocument.IsDirty; }
			set { ActiveDocument.IsDirty = value; }
		}
		
		public PointD Offset {
			get { return ActiveWorkspace.Offset; }
		}
		
		public double Scale {
			get { return ActiveWorkspace.Scale; }
			set { ActiveWorkspace.Scale = value; }
		}
		
		public List<Document> OpenDocuments { get; private set; }
		public bool HasOpenDocuments { get { return active_document_index >= 0; } }
		
		public void CreateAndActivateDocument ()
		{
			OpenDocuments.Add (new Document ());
			active_document_index = OpenDocuments.Count - 1;
		}

		public void Invalidate ()
		{
			ActiveWorkspace.Invalidate ();
		}
		
		public void Invalidate (Gdk.Rectangle rect)
		{
			ActiveWorkspace.Invalidate (rect);
		}
		
		public void ZoomIn ()
		{
			ActiveWorkspace.ZoomIn ();
		}
		
		public void ZoomOut ()
		{
			ActiveWorkspace.ZoomOut ();
		}

		public void ZoomToRectangle (Rectangle rect)
		{
			ActiveWorkspace.ZoomToRectangle (rect);
		}
		
		public void RecenterView (double x, double y)
		{
			ActiveWorkspace.RecenterView (x, y);
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

		public bool PointInCanvas (Cairo.PointD point)
		{
			return ActiveWorkspace.PointInCanvas (point);
		}

		public Gdk.Rectangle ClampToImageSize (Gdk.Rectangle r)
		{
			return ActiveDocument.ClampToImageSize (r);
		}

		public bool CanvasFitsInWindow {
			get { return ActiveWorkspace.CanvasFitsInWindow; }
		}

		public bool ImageFitsInWindow {
			get { return ActiveWorkspace.CanvasFitsInWindow; }
		}
		
		public void ScrollCanvas (int dx, int dy)
		{
			ActiveWorkspace.ScrollCanvas (dx, dy);
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
			
			active_document_index = index;
			
			OnActiveDocumentChanged (EventArgs.Empty);
		}
		
		public void SetActiveDocument (Document document)
		{
			int index = OpenDocuments.IndexOf (document);
		}
		
		#region Protected Methods
		protected void OnActiveDocumentChanged (EventArgs e)
		{
			if (ActiveDocumentChanged != null)
				ActiveDocumentChanged (this, EventArgs.Empty);
		}
		
		protected internal void OnCanvasInvalidated (CanvasInvalidatedEventArgs e)
		{
			if (CanvasInvalidated != null)
				CanvasInvalidated (this, e);
		}

		protected internal void OnCanvasSizeChanged ()
		{
			if (CanvasSizeChanged != null)
				CanvasSizeChanged (this, EventArgs.Empty);
		}
		#endregion

		#region Public Events
		public event EventHandler ActiveDocumentChanged;
		public event EventHandler<CanvasInvalidatedEventArgs> CanvasInvalidated;
		public event EventHandler CanvasSizeChanged;
		#endregion
		
	}
}
