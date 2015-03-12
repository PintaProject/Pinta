//
// PlaceholderWindow.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using Gdk;
using Gtk;
using MonoDevelop.Components;

namespace Pinta.Docking
{
	internal class PlaceholderWindow: Gtk.Window
	{
		Gdk.GC redgc;
		uint anim;
		int rx, ry, rw, rh;
		bool allowDocking;
		
		public bool AllowDocking {
			get {
				return allowDocking;
			}
			set {
				allowDocking = value;
			}
		}
		
		public PlaceholderWindow (DockFrame frame): base (Gtk.WindowType.Popup)
		{
			SkipTaskbarHint = true;
			Decorated = false;
			TransientFor = (Gtk.Window) frame.Toplevel;
			TypeHint = WindowTypeHint.Utility;
			
			// Create the mask for the arrow
			
			Realize ();
			redgc = new Gdk.GC (GdkWindow);
	   		redgc.RgbFgColor = frame.Style.Background (StateType.Selected);
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			GdkWindow.Opacity = 0.6;
		}
		
		void CreateShape (int width, int height)
		{
			Gdk.Color black, white;
			black = new Gdk.Color (0, 0, 0);
			black.Pixel = 1;
			white = new Gdk.Color (255, 255, 255);
			white.Pixel = 0;
			
			Gdk.Pixmap pm = new Pixmap (this.GdkWindow, width, height, 1);
			Gdk.GC gc = new Gdk.GC (pm);
			gc.Background = white;
			gc.Foreground = white;
			pm.DrawRectangle (gc, true, 0, 0, width, height);
			
			gc.Foreground = black;
			pm.DrawRectangle (gc, false, 0, 0, width - 1, height - 1);
			pm.DrawRectangle (gc, false, 1, 1, width - 3, height - 3);
			
			this.ShapeCombineMask (pm, 0, 0);
		}
		
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			CreateShape (allocation.Width, allocation.Height);
		}

		
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			//base.OnExposeEvent (args);
			int w, h;
			this.GetSize (out w, out h);
			this.GdkWindow.DrawRectangle (redgc, false, 0, 0, w-1, h-1);
			this.GdkWindow.DrawRectangle (redgc, false, 1, 1, w-3, h-3);
	  		return true;
		}
		
		public void Relocate (int x, int y, int w, int h, bool animate)
		{
			Gdk.Rectangle geometry = GtkWorkarounds.GetUsableMonitorGeometry (Screen, Screen.GetMonitorAtPoint (x, y));
			if (x < geometry.X)
				x = geometry.X;
			if (x + w > geometry.Right)
				x = geometry.Right - w;
			if (y < geometry.Y)
				y = geometry.Y;
			if (y > geometry.Bottom - h)
				y = geometry.Bottom - h;

			if (x != rx || y != ry || w != rw || h != rh) {
				Resize (w, h);
				Move (x, y);

				rx = x; ry = y; rw = w; rh = h;
				
				if (anim != 0) {
					GLib.Source.Remove (anim);
					anim = 0;
				}
				if (animate && w < 150 && h < 150) {
					int sa = 7;
					Move (rx-sa, ry-sa);
					Resize (rw+sa*2, rh+sa*2);
					anim = GLib.Timeout.Add (10, RunAnimation);
				}
			}
		}
		
		bool RunAnimation ()
		{
			int cx, cy, ch, cw;
			GetSize (out cw, out ch);
			GetPosition	(out cx, out cy);
			
			if (cx != rx) {
				cx++; cy++;
				ch-=2; cw-=2;
				Move (cx, cy);
				Resize (cw, ch);
				return true;
			}
			anim = 0;
			return false;
		}

		public DockDelegate DockDelegate { get; private set; }
		public Gdk.Rectangle DockRect { get; private set; }

		public void SetDockInfo (DockDelegate dockDelegate, Gdk.Rectangle rect)
		{
			DockDelegate = dockDelegate;
			DockRect = rect;
		}
	}

	class PadTitleWindow: Gtk.Window
	{
		public PadTitleWindow (DockFrame frame, DockItem draggedItem): base (Gtk.WindowType.Popup)
		{
			SkipTaskbarHint = true;
			Decorated = false;
			TransientFor = (Gtk.Window) frame.Toplevel;
			TypeHint = WindowTypeHint.Utility;

			VBox mainBox = new VBox ();

			HBox box = new HBox (false, 3);
			if (draggedItem.Icon != null) {
				var img = new ImageView (draggedItem.Icon);
				box.PackStart (img, false, false, 0);
			}
			Gtk.Label la = new Label ();
			la.Markup = draggedItem.Label;
			box.PackStart (la, false, false, 0);

			mainBox.PackStart (box, false, false, 0);

/*			if (draggedItem.Widget.IsRealized) {
				var win = draggedItem.Widget.GdkWindow;
				var alloc = draggedItem.Widget.Allocation;
				Gdk.Pixbuf img = Gdk.Pixbuf.FromDrawable (win, win.Colormap, alloc.X, alloc.Y, 0, 0, alloc.Width, alloc.Height);

				double mw = 140, mh = 140;
				if (img.Width > img.Height)
					mw *= 2;
				else
					mh *= 2;

				double r = Math.Min (mw / img.Width, mh / img.Height);
				img = img.ScaleSimple ((int)(img.Width * r), (int)(img.Height * r), Gdk.InterpType.Hyper);
				mainBox.PackStart (new Gtk.Image (img), false, false, 0);
			}*/

			CustomFrame f = new CustomFrame ();
			f.SetPadding (12, 12, 12, 12);
			f.SetMargins (1, 1, 1, 1);
			f.Add (mainBox);

			Add (f);
			ShowAll ();
		}
	}
}
