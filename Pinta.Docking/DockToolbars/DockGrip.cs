//
// DockGrip.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gtk;
using Gdk;
using Pinta.Docking;

namespace Pinta.Docking.DockToolbars
{
	internal class DockGrip: ToolItem
	{
		static int GripSize = Platform.IsWindows? 4 : 6; //wimp theme engine looks ugly with width 6
		const int MarginLeft = 1;
		const int MarginRight = 3;
		
		public DockGrip ()
		{
		}
		
		protected override void OnSizeRequested (ref Requisition req)
		{
			if (Orientation == Orientation.Horizontal) {
				req.Width = GripSize + MarginLeft + MarginRight;
				req.Height = 0;
			} else {
				req.Width = 0;
				req.Height = GripSize + MarginLeft + MarginRight;
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			Rectangle rect = Allocation;
			if (Orientation == Orientation.Horizontal) {
				rect.Width = GripSize;
				rect.X += MarginLeft;
			} else {
				rect.Height = GripSize;
				rect.Y += MarginLeft;
			}
			
			Gtk.Orientation or = Orientation == Gtk.Orientation.Horizontal ? Gtk.Orientation.Vertical : Gtk.Orientation.Horizontal;
			Gtk.Style.PaintHandle (this.Style, this.ParentWindow, this.State, Gtk.ShadowType.None, args.Area, this, "grip", rect.X, rect.Y, rect.Width, rect.Height, or);
			return true;
		}
	}
}
