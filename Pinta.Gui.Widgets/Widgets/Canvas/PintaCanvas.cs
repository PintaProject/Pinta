// 
// PintaCanvas.cs
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
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class PintaCanvas : DrawingArea
	{
		private readonly CanvasRenderer cr;
		private readonly Document document;

		private Cairo.ImageSurface? canvas;

		public CanvasWindow CanvasWindow { get; private set; }

		public PintaCanvas (CanvasWindow window, Document document)
		{
			CanvasWindow = window;
			this.document = document;

			cr = new CanvasRenderer (true);

			// Keep the widget the same size as the canvas
			document.Workspace.CanvasSizeChanged += delegate (object? sender, EventArgs e) {
				SetRequisition (document.Workspace.CanvasSize);
			};

			// Update the canvas when the image changes
			document.Workspace.CanvasInvalidated += delegate (object? sender, CanvasInvalidatedEventArgs e) {
				// If GTK+ hasn't created the canvas window yet, no need to invalidate it
				if (Window == null)
					return;

				if (e.EntireSurface)
					Window.Invalidate ();
				else
					Window.InvalidateRect (e.Rectangle, false);
			};

			// Give mouse press events to the current tool
			ButtonPressEvent += delegate (object sender, ButtonPressEventArgs e) {
				// The canvas gets the button press before the tab system, so
				// if this click is on a canvas that isn't currently the ActiveDocument yet, 
				// we need to go ahead and make it the active document for the tools
				// to use it, even though right after this the tab system would have switched it
				if (PintaCore.Workspace.ActiveDocument != document)
					PintaCore.Workspace.SetActiveDocument (document);

				PintaCore.Tools.DoMouseDown (document, e);
			};

			// Give mouse release events to the current tool
			ButtonReleaseEvent += delegate (object sender, ButtonReleaseEventArgs e) {
				PintaCore.Tools.DoMouseUp (document, e);
			};

			// Give mouse move events to the current tool
			MotionNotifyEvent += delegate (object sender, MotionNotifyEventArgs e) {
				var point = document.Workspace.WindowPointToCanvas (e.Event.X, e.Event.Y);

				if (document.Workspace.PointInCanvas (point))
					PintaCore.Chrome.LastCanvasCursorPoint = point.ToGdkPoint ();

				if (PintaCore.Tools.CurrentTool != null)
					PintaCore.Tools.DoMouseMove (document, e);
			};
		}

		protected override bool OnDrawn (Cairo.Context context)
		{
			base.OnDrawn (context);

			var scale = document.Workspace.Scale;

			var x = (int) document.Workspace.Offset.X;
			var y = (int) document.Workspace.Offset.Y;

			// Translate our expose area for the whole drawingarea to just our canvas
			var canvas_bounds = new Rectangle (x, y, document.Workspace.CanvasSize.Width, document.Workspace.CanvasSize.Height);
			Rectangle expose_rect;

			if (Gdk.CairoHelper.GetClipRectangle (context, out expose_rect))
				canvas_bounds.Intersect (expose_rect);

			if (canvas_bounds.IsEmpty)
				return true;

			canvas_bounds.X -= x;
			canvas_bounds.Y -= y;

			// Resize our offscreen surface to a surface the size of our drawing area
			if (canvas == null || canvas.Width != canvas_bounds.Width || canvas.Height != canvas_bounds.Height) {
				canvas?.Dispose ();
				canvas = CairoExtensions.CreateImageSurface (Cairo.Format.Argb32, canvas_bounds.Width, canvas_bounds.Height);
			}

			cr.Initialize (document.ImageSize, document.Workspace.CanvasSize);

			var g = context;

			// Draw our canvas drop shadow
			g.DrawRectangle (new Cairo.Rectangle (x - 1, y - 1, document.Workspace.CanvasSize.Width + 2, document.Workspace.CanvasSize.Height + 2), new Cairo.Color (.5, .5, .5), 1);
			g.DrawRectangle (new Cairo.Rectangle (x - 2, y - 2, document.Workspace.CanvasSize.Width + 4, document.Workspace.CanvasSize.Height + 4), new Cairo.Color (.8, .8, .8), 1);
			g.DrawRectangle (new Cairo.Rectangle (x - 3, y - 3, document.Workspace.CanvasSize.Width + 6, document.Workspace.CanvasSize.Height + 6), new Cairo.Color (.9, .9, .9), 1);

			// Set up our clip rectangle
			g.Rectangle (new Cairo.Rectangle (x, y, document.Workspace.CanvasSize.Width, document.Workspace.CanvasSize.Height));
			g.Clip ();

			g.Translate (x, y);

			// Render all the layers to a surface
			var layers = document.Layers.GetLayersToPaint ();

			if (layers.Count == 0)
				canvas.Clear ();

			cr.Render (layers, canvas, canvas_bounds.Location);

			// Paint the surface to our canvas
			g.SetSourceSurface (canvas, canvas_bounds.X + (int) (0 * scale), canvas_bounds.Y + (int) (0 * scale));
			g.Paint ();

			// Selection outline
			if (document.Selection.Visible) {
				var tool_name = PintaCore.Tools.CurrentTool?.GetType ().Name ?? string.Empty;
				var fillSelection = tool_name.Contains ("Select") && !tool_name.Contains ("Selected");
				document.Selection.Draw (g, scale, fillSelection);
			}

			return true;
		}

		private void SetRequisition (Size size)
		{
			WidthRequest = size.Width;
			HeightRequest = size.Height;

			QueueResize ();
		}

		public void DoKeyPressEvent (object o, KeyPressEventArgs e)
		{
			// Give the current tool a chance to handle the key press
			PintaCore.Tools.DoKeyDown (document, e);
		}

		public void DoKeyReleaseEvent (object o, KeyReleaseEventArgs e)
		{
			PintaCore.Tools.DoKeyUp (document, e);
		}
	}
}
