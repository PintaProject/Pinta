// 
// MoveSelectionTool.cs
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
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Tools
{
	public class MoveSelectionTool : BaseTool
	{
		private PointD origin_offset;
		private PointD selection_center;
		private bool is_dragging;
		private bool is_rotating;
		private SelectionHistoryItem hist;
		private ToolControl[] control_points = new ToolControl[8];
		
		public override string Name {
			get { return Catalog.GetString ("Move Selection"); }
		}
		public override string Icon {
			get { return "Tools.MoveSelection.png"; }
		}
		public override string StatusBarText {
			get { return Catalog.GetString ("Drag the selection to move selection outline."); }
		}
		public override Gdk.Cursor DefaultCursor {
			get { return new Gdk.Cursor (PintaCore.Chrome.Canvas.Display, PintaCore.Resources.GetIcon ("Tools.MoveSelection.png"), 0, 0); }
		}
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.M; } }
		public override int Priority { get { return 11; } }

		#region Mouse Handlers
		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			// If we are already drawing, ignore any additional mouse down events
			if (is_dragging || is_rotating)
				return;

			Document doc = PintaCore.Workspace.ActiveDocument;

			origin_offset = point;

			if(args.Event.Button == MOUSE_RIGHT_BUTTON)
			{
				is_rotating = true;
				Gdk.Rectangle rc = doc.Selection.Path.GetBounds();
				selection_center = new PointD(rc.X + rc.Width / 2, rc.Y + rc.Height / 2);
			}
			else
				is_dragging = true;

			hist = new SelectionHistoryItem (Icon, Name);
			hist.TakeSnapshot ();
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			if (!is_dragging && !is_rotating)
				return;

			Document doc = PintaCore.Workspace.ActiveDocument;

			PointD new_offset = point;
			
			double dx = origin_offset.X - new_offset.X;
			double dy = origin_offset.Y - new_offset.Y;

			double dy1 = origin_offset.Y-selection_center.Y;
			double dx1 = origin_offset.X-selection_center.X;
			double dy2 = new_offset.Y-selection_center.Y;
			double dx2 = new_offset.X-selection_center.X;

			double angle = Math.Atan2(dy1, dx1) - Math.Atan2(dy2,dx2);

			using (Cairo.Context g = new Cairo.Context (doc.CurrentLayer.Surface)) {
				Path old = doc.Selection.Path;
				g.FillRule = FillRule.EvenOdd;
				g.AppendPath (doc.Selection.Path);

				if(is_rotating)
				{
					g.Translate(selection_center.X, selection_center.Y);
					g.Rotate(angle);
					g.Translate(-selection_center.X, -selection_center.Y);
				}
				else
				{
					g.Translate (dx, dy);
				}

				doc.Selection.Path = g.CopyPath ();
				(old as IDisposable).Dispose ();
			}

			origin_offset = new_offset;
			doc.ShowSelection = true;
			
			(o as Gtk.DrawingArea).GdkWindow.Invalidate ();
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			is_dragging = false;
			is_rotating = false;

			if (hist != null)
				PintaCore.Workspace.ActiveDocument.History.PushNewItem (hist);

			hist = null;
		}
		#endregion
	}
}
