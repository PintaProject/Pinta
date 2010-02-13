// 
// PanTool.cs
//  
// Author:
//       dufoli <>
// 
// Copyright (c) 2010 dufoli
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
			get { return "Click and drag to move in the image."; }
		}
		public override bool Enabled {
			get { return true; }
		}
		
		private bool active;
		private PointD lastPt;
		
		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			active = true;
			lastPt = point;
			base.OnMouseDown (canvas, args, point);
		}
		
		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			active = false;
			base.OnMouseUp (canvas, args, point);
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			if (active) 
			{
				PointD lastScrollPosition = PintaCore.Workspace.CenterPosition;
				
                lastScrollPosition.X -= (point.X - lastPt.X);
                lastScrollPosition.Y -= (point.Y - lastPt.Y);
                PintaCore.Workspace.CenterPosition = lastScrollPosition;
			}
		}
	}
}
