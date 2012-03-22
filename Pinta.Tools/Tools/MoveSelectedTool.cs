// 
// MoveSelectedTool.cs
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
	public class MoveSelectedTool : BaseTool
	{
		private PointD origin_offset;
		private PointD selection_center;
		private bool is_dragging;
		private bool is_rotating;
		private MovePixelsHistoryItem hist;

		public override string Name {
			get { return Catalog.GetString ("Move Selected Pixels"); }
		}
		public override string Icon {
			get { return "Tools.Move.png"; }
		}
		public override string StatusBarText {
			get { return Catalog.GetString ("Drag the selection to move selected content."); }
		}
		public override Gdk.Cursor DefaultCursor {
			get { return new Gdk.Cursor (PintaCore.Chrome.Canvas.Display, PintaCore.Resources.GetIcon ("Tools.Move.png"), 0, 0); }
		}
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.M; } }
		public override int Priority { get { return 7; } }

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
				Gdk.Rectangle rc = doc.SelectionPath.GetBounds();
				selection_center = new PointD(rc.X + rc.Width / 2, rc.Y + rc.Height / 2);
			}
			else
				is_dragging = true;

			hist = new MovePixelsHistoryItem (Icon, Name, doc);
			hist.TakeSnapshot (!doc.ShowSelectionLayer);

			if (!doc.ShowSelectionLayer) {
				// Copy the selection to the temp layer
				doc.CreateSelectionLayer ();
				doc.ShowSelectionLayer = true;

				using (Cairo.Context g = new Cairo.Context (doc.SelectionLayer.Surface)) {
					g.AppendPath (doc.SelectionPath);
					g.FillRule = FillRule.EvenOdd;
					g.SetSource (doc.CurrentLayer.Surface);
					g.Clip ();
					g.Paint ();
				}

				Cairo.ImageSurface surf = doc.CurrentLayer.Surface;
				
				using (Cairo.Context g = new Cairo.Context (surf)) {
					g.AppendPath (doc.SelectionPath);
					g.FillRule = FillRule.EvenOdd;
					g.Operator = Cairo.Operator.Clear;
					g.Fill ();
				}
			}
			
			canvas.GdkWindow.Invalidate ();
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			if (!is_dragging && !is_rotating)
				return;

			Document doc = PintaCore.Workspace.ActiveDocument;

			PointD new_offset = new PointD (point.X, point.Y);
			
			double dx = origin_offset.X - new_offset.X;
			double dy = origin_offset.Y - new_offset.Y;

			double dy1 = origin_offset.Y-selection_center.Y;
			double dx1 = origin_offset.X-selection_center.X;
			double dy2 = new_offset.Y-selection_center.Y;
			double dx2 = new_offset.X-selection_center.X;

			double angle = Math.Atan2(dy1, dx1) - Math.Atan2(dy2,dx2);

			Path path = doc.SelectionPath;

			using (Cairo.Context g = new Cairo.Context (doc.CurrentLayer.Surface)) {
				g.AppendPath (path);

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

				doc.SelectionPath = g.CopyPath ();
			}

			(path as IDisposable).Dispose ();

			if(is_rotating)
			{
				double centerX = selection_center.X;
				double centerY = selection_center.Y;

				doc.SelectionLayer.Transform.Invert();
				doc.SelectionLayer.Transform.Translate(centerX, centerY);
				doc.SelectionLayer.Transform.Rotate(angle);
				doc.SelectionLayer.Transform.Translate(-centerX, -centerY);
				doc.SelectionLayer.Transform.Invert();
			}
			else
			{
				doc.SelectionLayer.Transform.Invert();
				doc.SelectionLayer.Transform.Translate(dx,dy);
				doc.SelectionLayer.Transform.Invert();
			}
			
			origin_offset = new_offset;
			
			(o as Gtk.DrawingArea).GdkWindow.Invalidate ();
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			is_dragging = false;
			is_rotating = false;

			if (hist != null)
				PintaCore.History.PushNewItem (hist);

			hist = null;
		}
		#endregion

		protected override void OnCommit (bool force)
		{
			if(force)
			{
				try {
					PintaCore.Workspace.ActiveDocument.FinishSelection ();
				} catch (Exception) {
					// Ignore an error where ActiveDocument fails.
				}
			}
		}

		protected override void OnDeactivated ()
		{
			base.OnDeactivated ();

			PintaCore.Workspace.ActiveDocument.FinishSelection ();
		}
	}
}
