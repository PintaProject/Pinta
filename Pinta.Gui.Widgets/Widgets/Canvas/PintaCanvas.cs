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
	[System.ComponentModel.ToolboxItem (true)]
	public class PintaCanvas : DrawingArea
	{
		Cairo.ImageSurface canvas;
		CanvasRenderer cr;

		public PintaCanvas ()
		{
			cr = new CanvasRenderer (true);
			
			// Keep the widget the same size as the canvas
			PintaCore.Workspace.CanvasSizeChanged += delegate (object sender, EventArgs e) {
				SetRequisition (PintaCore.Workspace.CanvasSize);
			};

			// Update the canvas when the image changes
			PintaCore.Workspace.CanvasInvalidated += delegate (object sender, CanvasInvalidatedEventArgs e) {
				if (e.EntireSurface)
					GdkWindow.Invalidate ();
				else
					GdkWindow.InvalidateRect (e.Rectangle, false);
			};

			// Give mouse press events to the current tool
			ButtonPressEvent += delegate (object sender, ButtonPressEventArgs e) {
				if (PintaCore.Workspace.HasOpenDocuments)
					PintaCore.Tools.CurrentTool.DoMouseDown (this, e, PintaCore.Workspace.WindowPointToCanvas (e.Event.X, e.Event.Y));
			};

			// Give mouse release events to the current tool
			ButtonReleaseEvent += delegate (object sender, ButtonReleaseEventArgs e) {
				if (PintaCore.Workspace.HasOpenDocuments)
					PintaCore.Tools.CurrentTool.DoMouseUp (this, e, PintaCore.Workspace.WindowPointToCanvas (e.Event.X, e.Event.Y));
			};

			// Give mouse move events to the current tool
			MotionNotifyEvent += delegate (object sender, MotionNotifyEventArgs e) {
				if (!PintaCore.Workspace.HasOpenDocuments)
					return;
					
				Cairo.PointD point = PintaCore.Workspace.ActiveWorkspace.WindowPointToCanvas (e.Event.X, e.Event.Y);

				if (PintaCore.Workspace.ActiveWorkspace.PointInCanvas (point))
					PintaCore.Chrome.LastCanvasCursorPoint = point.ToGdkPoint ();

				if (PintaCore.Tools.CurrentTool != null)
					PintaCore.Tools.CurrentTool.DoMouseMove (sender, e, point);
			};
		}

		#region Protected Methods
		protected override bool OnExposeEvent (EventExpose e)
		{
			base.OnExposeEvent (e);

			if (!PintaCore.Workspace.HasOpenDocuments)
				return true;
				
			double scale = PintaCore.Workspace.Scale;

			int x = (int)PintaCore.Workspace.Offset.X;
			int y = (int)PintaCore.Workspace.Offset.Y;

			// Translate our expose area for the whole drawingarea to just our canvas
			Rectangle canvas_bounds = new Rectangle (x, y, PintaCore.Workspace.CanvasSize.Width, PintaCore.Workspace.CanvasSize.Height);
			canvas_bounds.Intersect (e.Area);

			if (canvas_bounds.IsEmpty)
				return true;

			canvas_bounds.X -= x;
			canvas_bounds.Y -= y;

			// Resize our offscreen surface to a surface the size of our drawing area
			if (canvas == null || canvas.Width != canvas_bounds.Width || canvas.Height != canvas_bounds.Height) {
				if (canvas != null)
					(canvas as IDisposable).Dispose ();

				canvas = new Cairo.ImageSurface (Cairo.Format.Argb32, canvas_bounds.Width, canvas_bounds.Height);
			}

			cr.Initialize (PintaCore.Workspace.ImageSize, PintaCore.Workspace.CanvasSize);

			using (Cairo.Context g = CairoHelper.Create (GdkWindow)) {
				// Draw our canvas drop shadow
				g.DrawRectangle (new Cairo.Rectangle (x - 1, y - 1, PintaCore.Workspace.CanvasSize.Width + 2, PintaCore.Workspace.CanvasSize.Height + 2), new Cairo.Color (.5, .5, .5), 1);
				g.DrawRectangle (new Cairo.Rectangle (x - 2, y - 2, PintaCore.Workspace.CanvasSize.Width + 4, PintaCore.Workspace.CanvasSize.Height + 4), new Cairo.Color (.8, .8, .8), 1);
				g.DrawRectangle (new Cairo.Rectangle (x - 3, y - 3, PintaCore.Workspace.CanvasSize.Width + 6, PintaCore.Workspace.CanvasSize.Height + 6), new Cairo.Color (.9, .9, .9), 1);

				// Set up our clip rectangle
				g.Rectangle (new Cairo.Rectangle (x, y, PintaCore.Workspace.CanvasSize.Width, PintaCore.Workspace.CanvasSize.Height));
				g.Clip ();

				g.Translate (x, y);

				// Render all the layers to a surface
				var layers = PintaCore.Layers.GetLayersToPaint ();
				if (layers.Count == 0) {
					canvas.Clear ();
				}
				cr.Render (layers, canvas, canvas_bounds.Location);

				// Paint the surface to our canvas
				g.SetSourceSurface (canvas, canvas_bounds.X + (int)(0 * scale), canvas_bounds.Y + (int)(0 * scale));
				g.Paint ();

				// Selection outline
				if (PintaCore.Layers.ShowSelection) {
	                                bool fillSelection = PintaCore.Tools.CurrentTool.Name.Contains ("Select") &&
						!PintaCore.Tools.CurrentTool.Name.Contains ("Selected");
					PintaCore.Workspace.ActiveDocument.Selection.Draw (g, scale, fillSelection);
				}
			}

			return true;
		}

		protected override bool OnScrollEvent (EventScroll evnt)
		{
			// Allow the user to zoom in/out with Ctrl-Mousewheel
			if (FilterModifierKeys(evnt.State) == ModifierType.ControlMask) {
				switch (evnt.Direction) {
					case ScrollDirection.Down:
					case ScrollDirection.Right:
						PintaCore.Workspace.ActiveWorkspace.ZoomOutFromMouseScroll (new Cairo.PointD (evnt.X, evnt.Y));
						return true;
					case ScrollDirection.Left:
					case ScrollDirection.Up:
						PintaCore.Workspace.ActiveWorkspace.ZoomInFromMouseScroll (new Cairo.PointD (evnt.X, evnt.Y));
						return true;
				}
			}
			
			return base.OnScrollEvent (evnt);
		}
		#endregion

		#region Private Methods
		private void SetRequisition (Size size)
		{
			Requisition req = new Requisition ();
			req.Width = size.Width;
			req.Height = size.Height;
			Requisition = req;

			QueueResize ();
		}

		public void DoKeyPressEvent (object o, KeyPressEventArgs e)
		{
			// Give the current tool a chance to handle the key press
			PintaCore.Tools.CurrentTool.DoKeyPress (this, e);

			// If the tool didn't consume it, see if its a toolbox shortcut
			if (e.RetVal == null || !(bool)e.RetVal)
				if (FilterModifierKeys (e.Event.State) == ModifierType.None)
					PintaCore.Tools.SetCurrentTool (e.Event.Key);
		}

		public void DoKeyReleaseEvent (object o, KeyReleaseEventArgs e)
		{
			PintaCore.Tools.CurrentTool.DoKeyRelease (this, e);
		}

		/// <summary>
		/// Filters out all modifier keys except Ctrl/Shift/Alt. This prevents Caps Lock, Num Lock, etc
		/// from appearing as active modifier keys.
		/// </summary>
		private ModifierType FilterModifierKeys (Gdk.ModifierType current_state)
		{
			ModifierType state = ModifierType.None;

			state |= (current_state & ModifierType.ControlMask);
			state |= (current_state & ModifierType.ShiftMask);
			state |= (current_state & ModifierType.Mod1Mask);

			return state;
		}
		#endregion
	}
}
