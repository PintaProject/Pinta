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

		protected SelectionHistoryItem hist;
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.S; } }
		protected override bool ShowAntialiasingButton { get { return false; } }
		
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
			
			double x = Utility.Clamp (point.X, 0, doc.ImageSize.Width - 1);
			double y = Utility.Clamp (point.Y, 0, doc.ImageSize.Height - 1);

			shape_origin = new PointD (x, y);
			is_drawing = true;
			
			hist = new SelectionHistoryItem (Icon, Name);
			hist.TakeSnapshot ();
		}

		protected override void OnMouseUp (DrawingArea canvas, ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			double x = point.X;
			double y = point.Y;
			
			// If the user didn't move the mouse, they want to deselect
			int tolerance = 0;

			if (Math.Abs (reset_origin.X - args.Event.X) <= tolerance && Math.Abs (reset_origin.Y - args.Event.Y) <= tolerance) {
				PintaCore.Actions.Edit.Deselect.Activate ();
				hist.Dispose ();
				hist = null;
			} else {
				if (hist != null)
					PintaCore.Workspace.ActiveDocument.History.PushNewItem (hist);
					
				hist = null;
			}

			is_drawing = false;
		}
		
		protected override void OnMouseMove (object o, MotionNotifyEventArgs args, Cairo.PointD point)
		{
			if (!is_drawing)
				return;

			Document doc = PintaCore.Workspace.ActiveDocument;

			double x = Utility.Clamp (point.X, 0, doc.ImageSize.Width - 1);
			double y = Utility.Clamp (point.Y, 0, doc.ImageSize.Height - 1);

			doc.ShowSelection = true;

			Rectangle dirty = DrawShape (Utility.PointsToRectangle (shape_origin, new PointD (x, y), (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask), doc.SelectionLayer);

			doc.Workspace.Invalidate ();
			
			last_dirty = dirty;
		}
		#endregion
	}
}
