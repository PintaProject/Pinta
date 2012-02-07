﻿//
// DocumentWorkspace.cs
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
using Gdk;
using Mono.Unix;

namespace Pinta.Core
{
	public class DocumentWorkspace
	{
		private Document document;
		private Size canvas_size;
		private enum ZoomType
		{
			ZoomIn,
			ZoomOut,
			ZoomManually
		}

		internal DocumentWorkspace (Document document)
		{
			this.document = document;
			History = new DocumentWorkspaceHistory (document, this);
		}

		#region Public Properties
		public bool CanvasFitsInWindow {
			get {
				Gtk.Viewport view = (Gtk.Viewport)PintaCore.Chrome.Canvas.Parent;
				
				int window_x = view.Allocation.Width;
				int window_y = view.Children[0].Allocation.Height;
				
				if (CanvasSize.Width <= window_x && CanvasSize.Height <= window_y)
					return true;
				
				return false;
			}
		}

		public Size CanvasSize {
			get { return canvas_size; }
			set {
				if (canvas_size.Width != value.Width || canvas_size.Height != value.Height) {
					canvas_size = value;
					PintaCore.Workspace.OnCanvasSizeChanged ();
				}
			}
		}

		public DocumentWorkspaceHistory History { get; private set; }

		public bool ImageFitsInWindow {
			get {
				Gtk.Viewport view = (Gtk.Viewport)PintaCore.Chrome.Canvas.Parent;
				
				int window_x = view.Allocation.Width;
				int window_y = view.Children[0].Allocation.Height;
				
				if (document.ImageSize.Width <= window_x && document.ImageSize.Height <= window_y)
					return true;
				
				return false;
			}
		}

		public Cairo.PointD Offset {
			get { return new Cairo.PointD ((PintaCore.Chrome.Canvas.Allocation.Width - canvas_size.Width) / 2, (PintaCore.Chrome.Canvas.Allocation.Height - canvas_size.Height) / 2); }
		}

		public double Scale {
			get { return (double)CanvasSize.Width / (double)document.ImageSize.Width; }
			set {
				if (value != (double)CanvasSize.Width / (double)document.ImageSize.Width || value != (double)CanvasSize.Height / (double)document.ImageSize.Height) {
					int new_x = Math.Max ((int)(document.ImageSize.Width * value), 1);
					int new_y = Math.Max ((int)((new_x * document.ImageSize.Height) / document.ImageSize.Width), 1);

					CanvasSize = new Gdk.Size (new_x, new_y);
					Invalidate ();
				}
			}
		}

		#endregion

		#region Public Methods
		public void Invalidate ()
		{
			PintaCore.Workspace.OnCanvasInvalidated (new CanvasInvalidatedEventArgs ());
		}

		public void Invalidate (Gdk.Rectangle rect)
		{
			rect = new Gdk.Rectangle ((int)((rect.X) * Scale + Offset.X), (int)((rect.Y) * Scale + Offset.Y), (int)(rect.Width * Scale), (int)(rect.Height * Scale));
			PintaCore.Workspace.OnCanvasInvalidated (new CanvasInvalidatedEventArgs (rect));
		}

		public bool PointInCanvas (Cairo.PointD point)
		{
			if (point.X < 0 || point.Y < 0)
				return false;
			
			if (point.X >= document.ImageSize.Width || point.Y >= document.ImageSize.Height)
				return false;
			
			return true;
		}

		public void RecenterView (double x, double y)
		{
			Gtk.Viewport view = (Gtk.Viewport)PintaCore.Chrome.Canvas.Parent;
			
			view.Hadjustment.Value = Utility.Clamp (x * Scale - view.Hadjustment.PageSize / 2, view.Hadjustment.Lower, view.Hadjustment.Upper);
			view.Vadjustment.Value = Utility.Clamp (y * Scale - view.Vadjustment.PageSize / 2, view.Vadjustment.Lower, view.Vadjustment.Upper);
		}

		public void ScrollCanvas (int dx, int dy)
		{
			Gtk.Viewport view = (Gtk.Viewport)PintaCore.Chrome.Canvas.Parent;
			
			view.Hadjustment.Value = Utility.Clamp (dx + view.Hadjustment.Value, view.Hadjustment.Lower, view.Hadjustment.Upper - view.Hadjustment.PageSize);
			view.Vadjustment.Value = Utility.Clamp (dy + view.Vadjustment.Value, view.Vadjustment.Lower, view.Vadjustment.Upper - view.Vadjustment.PageSize);
		}

		public Cairo.PointD WindowPointToCanvas (double x, double y)
		{
			return new Cairo.PointD (Math.Floor ((x - Offset.X) / PintaCore.Workspace.Scale), Math.Floor ((y - Offset.Y) / PintaCore.Workspace.Scale));
		}

		public void ZoomIn ()
		{
			ZoomAndRecenterView (ZoomType.ZoomIn, new Cairo.PointD (-1, -1)); // Zoom in relative to the center of the viewport.
		}

		public void ZoomOut ()
		{
			ZoomAndRecenterView (ZoomType.ZoomOut, new Cairo.PointD (-1, -1)); // Zoom out relative to the center of the viewport.
		}

		public void ZoomInFromMouseScroll (Cairo.PointD point)
		{
			ZoomAndRecenterView (ZoomType.ZoomIn, point); // Zoom in relative to mouse position.
		}

		public void ZoomOutFromMouseScroll (Cairo.PointD point)
		{
			ZoomAndRecenterView (ZoomType.ZoomOut, point); // Zoom out relative to mouse position.
		}

		public void ZoomManually ()
		{
			ZoomAndRecenterView (ZoomType.ZoomManually, new Cairo.PointD (-1, -1));
		}

		public void ZoomToRectangle (Cairo.Rectangle rect)
		{
			double ratio;
			
			if (document.ImageSize.Width / rect.Width <= document.ImageSize.Height / rect.Height)
				ratio = document.ImageSize.Width / rect.Width;
			else
				ratio = document.ImageSize.Height / rect.Height;
			
			(PintaCore.Actions.View.ZoomComboBox.ComboBox as Gtk.ComboBoxEntry).Entry.Text = String.Format ("{0:F}%", ratio * 100.0);
			Gtk.Main.Iteration (); //Force update of scrollbar upper before recenter
			RecenterView (rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
		}
		#endregion

		#region Private Methods
		private void ZoomAndRecenterView (ZoomType zoomType, Cairo.PointD point)
		{
			if (zoomType == ZoomType.ZoomOut && (CanvasSize.Width == 1 || CanvasSize.Height ==1))
				return; //Can't zoom in past a 1x1 px canvas

			double zoom;
			
			if (!ViewActions.TryParsePercent (PintaCore.Actions.View.ZoomComboBox.ComboBox.ActiveText, out zoom))
				zoom = Scale * 100;
			
			zoom = Math.Min (zoom, 3600);
			
			PintaCore.Chrome.Canvas.GdkWindow.FreezeUpdates ();
			PintaCore.Actions.View.SuspendZoomUpdate ();
			
			Gtk.Viewport view = (Gtk.Viewport)PintaCore.Chrome.Canvas.Parent;
			
			bool adjustOnMousePosition = point.X >= 0.0 && point.Y >= 0.0;
			
			double center_x = adjustOnMousePosition ?
				point.X : view.Hadjustment.Value + (view.Hadjustment.PageSize / 2.0);
			double center_y = adjustOnMousePosition ?
				point.Y : view.Vadjustment.Value + (view.Vadjustment.PageSize / 2.0);
			
			center_x = (center_x - Offset.X) / Scale;
			center_y = (center_y - Offset.Y) / Scale;

			if (zoomType == ZoomType.ZoomIn || zoomType == ZoomType.ZoomOut) {
				int i = 0;
				
				Predicate<string> UpdateZoomLevel = zoomInList =>
				{
					switch (zoomType) {
					case ZoomType.ZoomIn:
						if (zoomInList == Catalog.GetString ("Window") || int.Parse (zoomInList.Trim ('%')) <= zoom) {
							PintaCore.Actions.View.ZoomComboBox.ComboBox.Active = i - 1;
							return true;
						}
						
						break;
					
					case ZoomType.ZoomOut:
						if (zoomInList == Catalog.GetString ("Window"))
							return true;
						
						if (int.Parse (zoomInList.Trim ('%')) < zoom) {
							PintaCore.Actions.View.ZoomComboBox.ComboBox.Active = i;
							return true;
						}
						
						break;
					}
					
					return false;
				};
				
				foreach (string item in PintaCore.Actions.View.ZoomCollection) {
					if (UpdateZoomLevel (item))
						break;
					
					i++;
				}
			}

			PintaCore.Actions.View.UpdateCanvasScale ();
			
			// Quick fix : need to manually update Upper limit because the value is not changing after updating the canvas scale.
			// TODO : I think there is an event need to be fired so that those values updated automatically.
			view.Hadjustment.Upper = CanvasSize.Width < view.Hadjustment.PageSize ? view.Hadjustment.PageSize : CanvasSize.Width;
			view.Vadjustment.Upper = CanvasSize.Height < view.Vadjustment.PageSize ? view.Vadjustment.PageSize : CanvasSize.Height;
			
			RecenterView (center_x, center_y);
			
			PintaCore.Actions.View.ResumeZoomUpdate ();
			PintaCore.Chrome.Canvas.GdkWindow.ThawUpdates ();
		}
		#endregion
	}
}
