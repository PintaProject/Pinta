//
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
			History = new DocumentHistory (document);
		}

		#region Public Events
		public event EventHandler<CanvasInvalidatedEventArgs>? CanvasInvalidated;
		public event EventHandler? CanvasSizeChanged;
		#endregion

		#region Public Properties
		public Gtk.DrawingArea Canvas { get; set; } = null!; // NRT - This is set soon after creation

		public bool CanvasFitsInWindow {
			get {
				Gtk.Viewport view = (Gtk.Viewport) Canvas.Parent!;

				int window_x = view.GetAllocatedWidth ();
				int window_y = view.GetAllocatedHeight ();

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
					OnCanvasSizeChanged ();
				}
			}
		}

		public DocumentHistory History { get; private set; }

		public bool ImageFitsInWindow {
			get {
				Gtk.Viewport view = (Gtk.Viewport) Canvas.Parent!;

				int window_x = view.GetAllocatedWidth ();
				int window_y = view.Child!.GetAllocatedHeight ();

				if (document.ImageSize.Width <= window_x && document.ImageSize.Height <= window_y)
					return true;

				return false;
			}
		}

		public PointD Offset {
			get { return new PointD ((Canvas.GetAllocatedWidth () - canvas_size.Width) / 2, (Canvas.GetAllocatedHeight () - canvas_size.Height) / 2); }
		}

		public double Scale {
			get { return (double) CanvasSize.Width / (double) document.ImageSize.Width; }
			set {
				if (value != (double) CanvasSize.Width / (double) document.ImageSize.Width || value != (double) CanvasSize.Height / (double) document.ImageSize.Height) {
					if (document.ImageSize.Width == 0) {
						document.ImageSize = new Size (1, document.ImageSize.Height);
					}

					if (document.ImageSize.Height == 0) {
						document.ImageSize = new Size (document.ImageSize.Width, 1);
					}

					int new_x = Math.Max ((int) (document.ImageSize.Width * value), 1);
					int new_y = Math.Max ((int) (((long) new_x * document.ImageSize.Height) / document.ImageSize.Width), 1);

					CanvasSize = new Size (new_x, new_y);
					Invalidate ();

#if false // TODO-GTK4
					if (PintaCore.Tools.CurrentTool?.CursorChangesOnZoom == true) {
						//The current tool's cursor changes when the zoom changes.
						PintaCore.Tools.CurrentTool.SetCursor (PintaCore.Tools.CurrentTool.DefaultCursor);
					}
#else
					throw new NotImplementedException ();
#endif
				}
			}
		}

		#endregion

		#region Public Methods
		public void Invalidate ()
		{
			OnCanvasInvalidated (new CanvasInvalidatedEventArgs ());
		}

		/// <summary>
		/// Repaints a rectangle region on the canvas.
		/// </summary>
		/// <param name='canvasRect'>
		/// The rectangle region of the canvas requiring repainting
		/// </param>
		public void Invalidate (RectangleI canvasRect)
		{
			var canvasTopLeft = new PointD (canvasRect.Left, canvasRect.Top);
			var canvasBtmRight = new PointD (canvasRect.Right + 1, canvasRect.Bottom + 1);

			var winTopLeft = CanvasPointToWindow (canvasTopLeft.X, canvasTopLeft.Y);
			var winBtmRight = CanvasPointToWindow (canvasBtmRight.X, canvasBtmRight.Y);

			RectangleI winRect = CairoExtensions.PointsToRectangle (winTopLeft, winBtmRight).ToInt ();

			OnCanvasInvalidated (new CanvasInvalidatedEventArgs (winRect));
		}

		/// <summary>
		/// Repaints a rectangle region in the window.
		/// Note that this overload uses window coordinates, whereas Invalidate() uses canvas coordinates.
		/// </summary>
		public void InvalidateWindowRect (RectangleI windowRect)
		{
			OnCanvasInvalidated (new CanvasInvalidatedEventArgs (windowRect));
		}

		/// <summary>
		/// Determines whether the rectangle lies (at least partially) outside the canvas area.
		/// </summary>
		public bool IsPartiallyOffscreen (RectangleI rect)
		{
			return (rect.IsEmpty || rect.Left < 0 || rect.Top < 0);
		}

		public bool PointInCanvas (PointD point)
		{
			if (point.X < 0 || point.Y < 0)
				return false;

			if (point.X >= document.ImageSize.Width || point.Y >= document.ImageSize.Height)
				return false;

			return true;
		}

		public void RecenterView (double x, double y)
		{
			Gtk.Viewport view = (Gtk.Viewport) Canvas.Parent!;

			var h_adjust = view.GetHadjustment ();
			h_adjust.Value = Utility.Clamp (x * Scale - h_adjust.PageSize / 2, h_adjust.Lower, h_adjust.Upper);
			var v_adjust = view.GetVadjustment ();
			v_adjust.Value = Utility.Clamp (y * Scale - v_adjust.PageSize / 2, v_adjust.Lower, v_adjust.Upper);
		}

		public void ScrollCanvas (int dx, int dy)
		{
			Gtk.Viewport view = (Gtk.Viewport) Canvas.Parent!;

			var h_adjust = view.GetHadjustment ();
			h_adjust.Value = Utility.Clamp (dx + h_adjust.Value, h_adjust.Lower, h_adjust.Upper - h_adjust.PageSize);
			var v_adjust = view.GetVadjustment ();
			v_adjust.Value = Utility.Clamp (dy + v_adjust.Value, v_adjust.Lower, v_adjust.Upper - v_adjust.PageSize);
		}

		/// <summary>
		/// Converts a point from window coordinates to canvas coordinates
		/// </summary>
		/// <param name='x'>
		/// The X coordinate of the window point
		/// </param>
		/// <param name='y'>
		/// The Y coordinate of the window point
		/// </param>
		public PointD WindowPointToCanvas (double x, double y)
		{
			var sf = new ScaleFactor (document.ImageSize.Width, CanvasSize.Width);
			var pt = sf.ScalePoint (new PointD (x - Offset.X, y - Offset.Y));
			return new PointD (pt.X, pt.Y);
		}

		/// <summary>
		/// Converts a point from canvas coordinates to window coordinates
		/// </summary>
		/// <param name='x'>
		/// The X coordinate of the canvas point
		/// </param>
		/// <param name='y'>
		/// The Y coordinate of the canvas point
		/// </param>
		public PointD CanvasPointToWindow (double x, double y)
		{
			var sf = new ScaleFactor (document.ImageSize.Width, CanvasSize.Width);
			var pt = sf.UnscalePoint (new PointD (x, y));
			return new PointD (pt.X + Offset.X, pt.Y + Offset.Y);
		}

		public void ZoomIn ()
		{
			ZoomAndRecenterView (ZoomType.ZoomIn, new PointD (-1, -1)); // Zoom in relative to the center of the viewport.
		}

		public void ZoomOut ()
		{
			ZoomAndRecenterView (ZoomType.ZoomOut, new PointD (-1, -1)); // Zoom out relative to the center of the viewport.
		}

		public void ZoomInFromMouseScroll (in PointD point)
		{
			ZoomAndRecenterView (ZoomType.ZoomIn, point); // Zoom in relative to mouse position.
		}

		public void ZoomOutFromMouseScroll (in PointD point)
		{
			ZoomAndRecenterView (ZoomType.ZoomOut, point); // Zoom out relative to mouse position.
		}

		public void ZoomManually ()
		{
			ZoomAndRecenterView (ZoomType.ZoomManually, new PointD (-1, -1));
		}

		public void ZoomToRectangle (RectangleD rect)
		{
			double ratio;

			if (document.ImageSize.Width / rect.Width <= document.ImageSize.Height / rect.Height)
				ratio = document.ImageSize.Width / rect.Width;
			else
				ratio = document.ImageSize.Height / rect.Height;

#if false // TODO-GTK4
			PintaCore.Actions.View.ZoomComboBox.ComboBox.Entry.Text = ViewActions.ToPercent (ratio);
			Gtk.Main.Iteration (); //Force update of scrollbar upper before recenter
			RecenterView (rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
#else
			throw new NotImplementedException ();
#endif
		}
		#endregion

		#region Private Methods
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

		private void ZoomAndRecenterView (ZoomType zoomType, in PointD point)
		{
#if false // TODO-GTK4
			if (zoomType == ZoomType.ZoomOut && (CanvasSize.Width == 1 || CanvasSize.Height == 1))
				return; //Can't zoom in past a 1x1 px canvas

			double zoom;

			if (!ViewActions.TryParsePercent (PintaCore.Actions.View.ZoomComboBox.ComboBox.ActiveText, out zoom))
				zoom = Scale * 100;

			zoom = Math.Min (zoom, 3600);

			if (Canvas.Window != null)
				Canvas.Window.FreezeUpdates ();

			PintaCore.Actions.View.SuspendZoomUpdate ();

			Gtk.Viewport view = (Gtk.Viewport) Canvas.Parent;

			bool adjustOnMousePosition = point.X >= 0.0 && point.Y >= 0.0;

			double center_x = adjustOnMousePosition ?
				point.X : view.Hadjustment.Value + (view.Hadjustment.PageSize / 2.0);
			double center_y = adjustOnMousePosition ?
				point.Y : view.Vadjustment.Value + (view.Vadjustment.PageSize / 2.0);

			center_x = (center_x - Offset.X) / Scale;
			center_y = (center_y - Offset.Y) / Scale;

			if (zoomType == ZoomType.ZoomIn || zoomType == ZoomType.ZoomOut) {
				int i = 0;

				Predicate<string> UpdateZoomLevel = zoomInList => {
					double zoom_level;
					if (!ViewActions.TryParsePercent (zoomInList, out zoom_level))
						return false;

					switch (zoomType) {
						case ZoomType.ZoomIn:
							if (zoomInList == Translations.GetString ("Window") || zoom_level <= zoom) {
								PintaCore.Actions.View.ZoomComboBox.ComboBox.Active = i - 1;
								return true;
							}

							break;

						case ZoomType.ZoomOut:
							if (zoomInList == Translations.GetString ("Window"))
								return true;

							if (zoom_level < zoom) {
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
			if (Canvas.Window != null)
				Canvas.Window.ThawUpdates ();
#else
			throw new NotImplementedException ();
#endif
		}
		#endregion
	}
}
