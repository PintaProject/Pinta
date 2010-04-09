// 
// PanTool.cs
//  
// Author:
//       Olivier Dufour
// 
// Copyright (c) 2010 Olivier Dufour
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
	public class PanTool : BaseTool
	{
		public override string Name {
			get { return "Pan"; }
		}
		public override string Icon {
			get { return "Tools.Pan.png"; }
		}
		public override string StatusBarText {
			get { return "When zoomed in close, click and drag to navigate image."; }
		}
		public override Gdk.Cursor DefaultCursor {
			get { return new Gdk.Cursor (PintaCore.Chrome.DrawingArea.Display, PintaCore.Resources.GetIcon ("Tools.Pan.png"), 0, 0); }
		}
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.H; } }
		
		private bool active;
		private PointD last_point;
		
		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, PointD point)
		{
			// Don't scroll if the whole canvas fits (no scrollbars)
			if (!PintaCore.Workspace.CanvasFitsInWindow)
				active = true;
				
			last_point = new PointD (args.Event.XRoot, args.Event.YRoot);
		}
		
		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, PointD point)
		{
			active = false;
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, PointD point)
		{
			if (active) {
				PintaCore.Workspace.ScrollCanvas ((int)(last_point.X - args.Event.XRoot), (int)(last_point.Y - args.Event.YRoot));
				last_point = new PointD (args.Event.XRoot, args.Event.YRoot);
			}
		}
	}
}
