// 
// SelectTool.cs
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
using Gtk;
using Pinta.Core;

namespace Pinta.Tools
{
	public abstract class SelectTool : ShapeTool
	{
		private PointD reset_origin;
		private PointD shape_end;
		private ToolControl [] controls = new ToolControl [8];
		protected SelectionHistoryItem hist;
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.S; } }
		protected override bool ShowAntialiasingButton { get { return false; } }
		bool handler_active = false;
		private Gdk.Cursor cursor_hand;
		bool is_hand_cursor = false;

		public SelectTool ()
		{
			CreateHandler ();
			cursor_hand = new Gdk.Cursor (PintaCore.Chrome.Canvas.Display, PintaCore.Resources.GetIcon ("Tools.Pan.png"), 0, 0);
		}

		#region ToolBar
		// We don't want the ShapeTool's toolbar
		protected override void BuildToolBar (Toolbar tb)
		{
		}
		#endregion
		
		#region Mouse Handlers
		protected override void OnMouseDown (DrawingArea canvas, ButtonPressEventArgs args, Cairo.PointD point)
		{
			// Ignore extra button clicks while drawing
			if (is_drawing)
				return;

			reset_origin = args.Event.GetPoint ();

			Document doc = PintaCore.Workspace.ActiveDocument; 
			if (!handler_active || !HandleResize (point.X, point.Y)) {			
				double x = Utility.Clamp (point.X, 0, doc.ImageSize.Width - 1);
				double y = Utility.Clamp (point.Y, 0, doc.ImageSize.Height - 1);

				shape_origin = new PointD (x, y);
				is_drawing = true;
			}
			hist = new SelectionHistoryItem (Icon, Name);
			hist.TakeSnapshot ();
		}

		protected override void OnMouseUp (DrawingArea canvas, ButtonReleaseEventArgs args, Cairo.PointD point)
		{

			// If the user didn't move the mouse, they want to deselect
			int tolerance = 0;
			if (Math.Abs (reset_origin.X - args.Event.X) <= tolerance && Math.Abs (reset_origin.Y - args.Event.Y) <= tolerance) {
				PintaCore.Actions.Edit.Deselect.Activate ();
				hist.Dispose ();
				hist = null;
				handler_active = false;
				Document doc = PintaCore.Workspace.ActiveDocument;
				doc.ToolLayer.Clear ();
			} else {
				if (hist != null)
					PintaCore.Workspace.ActiveDocument.History.PushNewItem (hist);

				handler_active = true;
				hist = null;
			}

			is_drawing = false;
		}

		protected override void OnDeactivated ()
		{
			base.OnDeactivated ();
			handler_active = false;
			if (PintaCore.Workspace.HasOpenDocuments) {
				Document doc = PintaCore.Workspace.ActiveDocument;
				doc.ToolLayer.Clear ();
			}
		}

		protected override void OnCommit ()
		{
			base.OnCommit ();
			handler_active = false;
			if (PintaCore.Workspace.HasOpenDocuments) {
				Document doc = PintaCore.Workspace.ActiveDocument;
				doc.ToolLayer.Clear ();
			}
		}

		protected override void OnMouseMove (object o, MotionNotifyEventArgs args, Cairo.PointD point)
		{
			if (!is_drawing) {
				CheckHandlerCursor (point.X, point.Y);
				return;
			}
			Document doc = PintaCore.Workspace.ActiveDocument;

			double x = Utility.Clamp (point.X, 0, doc.ImageSize.Width - 1);
			double y = Utility.Clamp (point.Y, 0, doc.ImageSize.Height - 1);

			shape_end = new PointD (x, y);
			ReDraw (args.Event.State);
		}

		protected void RefreshHandler (Cairo.Rectangle r)
		{
			/*controls[0].Position = new PointD (r.X, r.Y);
			controls[1].Position = new PointD (r.X, r.GetBottom ());
			controls[2].Position = new PointD (r.GetRight (), r.Y);
			controls[3].Position = new PointD (r.GetRight (), r.GetBottom ());
			controls[4].Position = new PointD (r.X, r.Y + (r.Height / 2));
			controls[5].Position = n(ew PointD (r.X + (r.Width / 2), r.Y);
			controls[6].Position = new PointD (r.GetRight (), r.Y + (r.Height / 2));
			controls[7].Position = new PointD (r.X + (r.Width / 2), r.GetBottom ());
			*/
			controls[0].Position = new PointD (shape_origin.X, shape_origin.Y);
			controls[1].Position = new PointD (shape_origin.X, shape_end.Y);
			controls[2].Position = new PointD (shape_end.X, shape_origin.Y);
			controls[3].Position = new PointD (shape_end.X, shape_end.Y);
			controls[4].Position = new PointD (shape_origin.X, (shape_origin.Y + shape_end.Y) / 2);
			controls[5].Position = new PointD ((shape_origin.X + shape_end.X) / 2, shape_origin.Y);
			controls[6].Position = new PointD (shape_end.X, (shape_origin.Y + shape_end.Y) / 2);
			controls[7].Position = new PointD ((shape_origin.X + shape_end.X) / 2, shape_end.Y);

		}

		public void ReDraw (Gdk.ModifierType state)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			doc.ShowSelection = true;
			doc.ToolLayer.Hidden = false;
			bool constraint = (state & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask;
			if (constraint) {
				double dx = Math.Abs (shape_end.X - shape_origin.X);
				double dy = Math.Abs (shape_end.Y - shape_origin.Y);
				if (dx <= dy)
					if (shape_end.X >= shape_origin.X)
						shape_end.X = shape_origin.X + dy;
					else
						shape_end.X = shape_origin.X - dy;
				else
					if (shape_end.Y >= shape_origin.Y)
						shape_end.Y = shape_origin.Y + dx;
					else
						shape_end.Y = shape_origin.Y - dx;
			}
			Cairo.Rectangle rect = Utility.PointsToRectangle (shape_origin, shape_end, constraint);
			RefreshHandler (rect);
			Rectangle dirty = DrawShape (rect, doc.SelectionLayer);

			DrawHandler (doc.ToolLayer);
			doc.Workspace.Invalidate ();

			last_dirty = dirty;
		}

		protected void CreateHandler ()
		{
			controls[0] = new ToolControl ((x, y, s) => {
				shape_origin.X = x;
				shape_origin.Y = y;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					if (shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
						shape_origin.X = shape_end.X - shape_end.Y + shape_origin.Y;
					else
						shape_origin.Y = shape_end.Y - shape_end.X + shape_origin.X;
				}
				ReDraw (s);
			});
			controls[1] = new ToolControl ((x, y, s) => {
				shape_origin.X = x;
				shape_end.Y = y;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					if (shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
						shape_origin.X = shape_end.X - shape_end.Y + shape_origin.Y;
					else
						shape_end.Y = shape_origin.Y + shape_end.X - shape_origin.X;
				}
				ReDraw (s);
			});
			controls[2] = new ToolControl ((x, y, s) => {
				shape_end.X = x;
				shape_origin.Y = y;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					if (shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
						shape_end.X = shape_origin.X + shape_end.Y - shape_origin.Y;
					else
						shape_origin.Y = shape_end.Y - shape_end.X + shape_origin.X;
				}
				ReDraw (s);
			});
			controls[3] = new ToolControl ((x, y, s) => {
				shape_end.X = x;
				shape_end.Y = y;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					if (shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
						shape_end.X = shape_origin.X + shape_end.Y - shape_origin.Y;
					else
						shape_end.Y = shape_origin.Y + shape_end.X - shape_origin.X;
				}
				ReDraw (s);
			});
			controls[4] = new ToolControl ((x, y, s) => {
				shape_origin.X = x;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					double d = shape_end.X - shape_origin.X;
					shape_origin.Y = (shape_origin.Y + shape_end.Y - d) / 2;
					shape_end.Y = (shape_origin.Y + shape_end.Y + d) / 2;
				}
				ReDraw (s);
			});
			controls[5] = new ToolControl ((x, y, s) => {
				shape_origin.Y = y;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					double d = shape_end.Y - shape_origin.Y;
					shape_origin.X = (shape_origin.X + shape_end.X - d) / 2;
					shape_end.X = (shape_origin.X + shape_end.X + d) / 2;
				}
				ReDraw (s);
			});
			controls[6] = new ToolControl ((x, y, s) => {
				shape_end.X = x;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					double d = shape_end.X - shape_origin.X;
					shape_origin.Y = (shape_origin.Y + shape_end.Y - d) / 2;
					shape_end.Y = (shape_origin.Y + shape_end.Y + d) / 2;
				}
				ReDraw (s);
			});
			controls[7] = new ToolControl ((x, y, s) => {
				shape_end.Y = y;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					double d = shape_end.Y - shape_origin.Y;
					shape_origin.X = (shape_origin.X + shape_end.X - d) / 2;
					shape_end.X = (shape_origin.X + shape_end.X + d) / 2;
				}
				ReDraw (s);
			});
		}

		public bool HandleResize (double x, double y)
		{
			foreach (ToolControl ct in controls) {
				if (ct.Handle (this, new PointD (x, y ))) {

					return true;
				}
			}
			return false;

		}

		public void DrawHandler (Layer layer)
		{
			layer.Clear ();
			foreach (ToolControl ct in controls)
				ct.Render (layer);
		}

		public void CheckHandlerCursor (double x, double y)
		{
			foreach (ToolControl ct in controls) {
				if (ct.IsInside (x, y)) {
					if (!is_hand_cursor) {
						SetCursor (cursor_hand);
						is_hand_cursor = true;
					}
					return;
				}
			}
			if (is_hand_cursor) {
				SetCursor (DefaultCursor);
				is_hand_cursor = false;
			}
		}

		#endregion
	}
}
