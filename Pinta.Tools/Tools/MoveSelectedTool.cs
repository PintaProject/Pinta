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
		private bool is_dragging;
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
			if (is_dragging)
				return;

			Document doc = PintaCore.Workspace.ActiveDocument;

			origin_offset = point;
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
			if (!is_dragging)
				return;

			Document doc = PintaCore.Workspace.ActiveDocument;

			PointD new_offset = new PointD (point.X, point.Y);
			
			double dx = origin_offset.X - new_offset.X;
			double dy = origin_offset.Y - new_offset.Y;

			Path path = doc.SelectionPath;

			using (Cairo.Context g = new Cairo.Context (doc.CurrentLayer.Surface)) {
				g.AppendPath (path);
				g.Translate (dx, dy);
				doc.SelectionPath = g.CopyPath ();
			}

			(path as IDisposable).Dispose ();

			doc.SelectionLayer.Offset = new PointD (doc.SelectionLayer.Offset.X - dx, doc.SelectionLayer.Offset.Y - dy);
			
			origin_offset = new_offset;
			
			(o as Gtk.DrawingArea).GdkWindow.Invalidate ();
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			is_dragging = false;

			if (hist != null)
				PintaCore.History.PushNewItem (hist);

			hist = null;
		}
		#endregion

		protected override void OnCommit ()
		{
			PintaCore.Workspace.ActiveDocument.FinishSelection ();
		}

		protected override void OnDeactivated ()
		{
			base.OnDeactivated ();

			PintaCore.Workspace.ActiveDocument.FinishSelection ();
		}
	}
}
