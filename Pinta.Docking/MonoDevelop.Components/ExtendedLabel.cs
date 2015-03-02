//
// ExtendedLabel.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc
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

namespace MonoDevelop.Components
{
	class ExtendedLabel: Gtk.Label
	{
		bool dropShadowVisible;

		public ExtendedLabel ()
		{
		}

		public ExtendedLabel (string text): base (text)
		{
		}

		public bool DropShadowVisible {
			get { return dropShadowVisible; }
			set {
				dropShadowVisible = value;
				QueueDraw ();
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (!dropShadowVisible)
				return base.OnExposeEvent (evnt);

			Pango.Layout la = new Pango.Layout (PangoContext);
			int w, h;
			if (UseMarkup)
				la.SetMarkup (Text);
			else
				la.SetText (Text);

			la.GetPixelSize (out w, out h);

			int tx = Allocation.X + (int) Xpad + (int) ((float)(Allocation.Width - (int)(Xpad*2) - w) * Xalign);
			int ty = Allocation.Y + (int) Ypad + (int) ((float)(Allocation.Height - (int)(Ypad*2) - h) * Yalign);
			
			GdkWindow.DrawLayout (Style.TextGC (State), tx, ty, la);
			
			la.Dispose ();
			return true;
		}
	}
}

