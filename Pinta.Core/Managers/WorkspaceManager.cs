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

namespace Pinta.Core
{
	public class Document
	{
		public Document () {
			IsDirty = false;
			HasFile = false;
		}

		public bool HasFile { get; set; }

		private string pathname;

		public string Pathname {
			get { return (pathname != null) ? pathname : string.Empty; }
			set { pathname = value; }
		}

		public string Filename {
			get {
				return System.IO.Path.GetFileName (Pathname);
			}

			set {
				if (value != null) {
					Pathname = System.IO.Path.Combine (Pathname, value);
				}
			}
		}

		public bool IsDirty { get; set; }
	}


	public class WorkspaceManager
	{
		private Point canvas_size;

		private Document Document { get; set; }

		public Point ImageSize { get; set; }
		
		public Point CanvasSize {
			get { return canvas_size; }
			set {
				if (canvas_size.X != value.X || canvas_size.Y != value.Y) {
					canvas_size = value;
					OnCanvasSizeChanged ();
				}
			}
		}
		
		public PointD Offset {
			get { return new PointD ((PintaCore.Chrome.DrawingArea.Allocation.Width - canvas_size.X) / 2, (PintaCore.Chrome.DrawingArea.Allocation.Height - CanvasSize.Y) / 2); }
		}
		
		public WorkspaceManager ()
		{
			ActiveDocument = Document = new Document ();
			CanvasSize = new Point (800, 600);
			ImageSize = new Point (800, 600);
		}
		
		public double Scale {
			get { return (double)CanvasSize.X / (double)ImageSize.X; }
			set {
				if (Scale != value) {
					int new_x = (int)(ImageSize.X * value);
					int new_y = (int)((new_x * ImageSize.Y) / ImageSize.X);

					CanvasSize = new Point (new_x, new_y);
					Invalidate ();
				}
			}
		}
		
		public void Invalidate ()
		{
			OnCanvasInvalidated (new CanvasInvalidatedEventArgs ());
		}
			
		public void Invalidate (Gdk.Rectangle rect)
		{
			rect = new Gdk.Rectangle ((int)((rect.X) * Scale + Offset.X), (int)((rect.Y) * Scale + Offset.Y), (int)(rect.Width * Scale), (int)(rect.Height * Scale));
			OnCanvasInvalidated (new CanvasInvalidatedEventArgs (rect));
		}
		
		public void ZoomIn ()
		{
			double zoom;

			if (!double.TryParse (PintaCore.Actions.View.ZoomComboBox.ComboBox.ActiveText.Trim ('%'), out zoom))
				zoom = Scale * 100;

			zoom = Math.Min (zoom, 3600);

			int i = 0;

			foreach (object item in (PintaCore.Actions.View.ZoomComboBox.ComboBox.Model as Gtk.ListStore)) {
				if (((object[])item)[0].ToString () == "Window" || int.Parse (((object[])item)[0].ToString ().Trim ('%')) <= zoom) {
					PintaCore.Actions.View.ZoomComboBox.ComboBox.Active = i - 1;
					return;
				}
				
				i++;
			}
		}
		
		public void ZoomOut ()
		{
			double zoom;
			
			if (!double.TryParse (PintaCore.Actions.View.ZoomComboBox.ComboBox.ActiveText.Trim ('%'), out zoom))
				zoom = Scale * 100;
				
			zoom = Math.Min (zoom, 3600);
			
			int i = 0;

			foreach (object item in (PintaCore.Actions.View.ZoomComboBox.ComboBox.Model as Gtk.ListStore)) {
				if (((object[])item)[0].ToString () == "Window")
					return;

				if (int.Parse (((object[])item)[0].ToString ().Trim ('%')) < zoom) {
					PintaCore.Actions.View.ZoomComboBox.ComboBox.Active = i;
					return;
				}

				i++;
			}
		}

		public void ZoomToRectangle (Rectangle rect)
		{
			double ratio;
			
			if (ImageSize.X / rect.Width <= ImageSize.Y / rect.Height)
				ratio = ImageSize.X / rect.Width;
			else
				ratio = ImageSize.Y / rect.Height;
			
			(PintaCore.Actions.View.ZoomComboBox.ComboBox as Gtk.ComboBoxEntry).Entry.Text = String.Format ("{0:F}%", ratio * 100.0);
			Gtk.Main.Iteration (); //Force update of scrollbar upper before recenter
			RecenterView (rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
		}
		
		public void RecenterView (double x, double y)
		{
			Gtk.Viewport view = (Gtk.Viewport)PintaCore.Chrome.DrawingArea.Parent;

			view.Hadjustment.Value = Utility.Clamp (x * Scale - view.Hadjustment.PageSize / 2 , view.Hadjustment.Lower, view.Hadjustment.Upper);
			view.Vadjustment.Value = Utility.Clamp (y * Scale - view.Vadjustment.PageSize / 2  , view.Vadjustment.Lower, view.Vadjustment.Upper);
		}
		
		public void ResizeImage (int width, int height)
		{
			if (ImageSize.X == width && ImageSize.Y == height)
				return;
				
			PintaCore.Layers.FinishSelection ();
			
			ResizeHistoryItem hist = new ResizeHistoryItem (ImageSize.X, ImageSize.Y);
			hist.TakeSnapshotOfImage ();
			
			ImageSize = new Point (width, height);
			CanvasSize = new Point (width, height);
			
			foreach (var layer in PintaCore.Layers)
				layer.Resize (width, height);
			
			PintaCore.History.PushNewItem (hist);
			
			PintaCore.Layers.ResetSelectionPath ();
			PintaCore.Workspace.Invalidate ();
		}
		
		public void ResizeCanvas (int width, int height, Anchor anchor)
		{
			if (ImageSize.X == width && ImageSize.Y == height)
				return;

			PintaCore.Layers.FinishSelection ();

			ResizeHistoryItem hist = new ResizeHistoryItem (ImageSize.X, ImageSize.Y);
			hist.Icon = "Menu.Image.CanvasSize.png";
			hist.Text = "Resize Canvas";
			hist.TakeSnapshotOfImage ();

			ImageSize = new Point (width, height);
			CanvasSize = new Point (width, height);

			foreach (var layer in PintaCore.Layers)
				layer.ResizeCanvas (width, height, anchor);

			PintaCore.History.PushNewItem (hist);

			PintaCore.Layers.ResetSelectionPath ();
			PintaCore.Workspace.Invalidate ();
		}
		
		public Cairo.PointD WindowPointToCanvas (double x, double y)
		{
			return new Cairo.PointD ((x - Offset.X) / PintaCore.Workspace.Scale, (y - Offset.Y) / PintaCore.Workspace.Scale);
		}

		public bool PointInCanvas (Cairo.PointD point)
		{
			if (point.X < 0 || point.Y < 0)
				return false;

			if (point.X >= PintaCore.Workspace.ImageSize.X || point.Y >= PintaCore.Workspace.ImageSize.Y)
				return false;

			return true;
		}

		public Gdk.Rectangle ClampToImageSize (Gdk.Rectangle r)
		{
			int x = Utility.Clamp (r.X, 0, ImageSize.X);
			int y = Utility.Clamp (r.Y, 0, ImageSize.Y);
			int width = Math.Min (r.Width, ImageSize.X - x);
			int height = Math.Min (r.Height, ImageSize.Y - y);

			return new Gdk.Rectangle (x, y, width, height);
		}

		public Document ActiveDocument { get; set; }

		public string DocumentPath {
			get { return Document.Pathname; }
			set { Document.Pathname = value; }
		}

		public string Filename {
			get { return Document.Filename; }
			set {
				if (Document.Filename != value) {
					Document.Filename = value;
					ResetTitle ();
				}
			}
		}
		
		public bool IsDirty {
			get { return Document.IsDirty; }
			set {
				if (Document.IsDirty != value) {
					Document.IsDirty = value;
					ResetTitle ();
				}
			}
		}
		
		public bool CanvasFitsInWindow {
			get {
				Gtk.Viewport view = (Gtk.Viewport)PintaCore.Chrome.DrawingArea.Parent;

				int window_x = view.Allocation.Width;
				int window_y = view.Children[0].Allocation.Height;

				if (CanvasSize.X <= window_x && CanvasSize.Y <= window_y)
					return true;

				return false;
			}
		}

		public bool ImageFitsInWindow {
			get {
				Gtk.Viewport view = (Gtk.Viewport)PintaCore.Chrome.DrawingArea.Parent;

				int window_x = view.Allocation.Width;
				int window_y = view.Children[0].Allocation.Height;

				if (ImageSize.X <= window_x && ImageSize.Y <= window_y)
					return true;

				return false;
			}
		}
		
		public void ScrollCanvas (int dx, int dy)
		{
			Gtk.Viewport view = (Gtk.Viewport)PintaCore.Chrome.DrawingArea.Parent;

			view.Hadjustment.Value = Utility.Clamp (dx + view.Hadjustment.Value, view.Hadjustment.Lower, view.Hadjustment.Upper - view.Hadjustment.PageSize);
			view.Vadjustment.Value = Utility.Clamp (dy + view.Vadjustment.Value, view.Vadjustment.Lower, view.Vadjustment.Upper - view.Vadjustment.PageSize);
		}
		
		private void ResetTitle ()
		{
			PintaCore.Chrome.MainWindow.Title = string.Format ("{0}{1} - Pinta", Filename, IsDirty ? "*" : "");
		}

		#region Protected Methods
		protected void OnCanvasInvalidated (CanvasInvalidatedEventArgs e)
		{
			if (CanvasInvalidated != null)
				CanvasInvalidated (this, e);
		}

		protected void OnCanvasSizeChanged ()
		{
			if (CanvasSizeChanged != null)
				CanvasSizeChanged (this, EventArgs.Empty);
		}
		#endregion

		#region Public Events
		public event EventHandler<CanvasInvalidatedEventArgs> CanvasInvalidated;
		public event EventHandler CanvasSizeChanged;
		#endregion
		
	}
}
