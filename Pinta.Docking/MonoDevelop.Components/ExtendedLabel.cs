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
using Cairo;
using Gdk;
using Pinta.Core;

namespace MonoDevelop.Components
{
	class ExtendedLabel : Gtk.Label
	{
		public ExtendedLabel()
		{
		}

		public ExtendedLabel(string text) : base(text)
		{
		}

		protected override bool OnDrawn(Cairo.Context ctx)
		{
			Pango.Layout la = new Pango.Layout(PangoContext);
			int w, h;
			if (UseMarkup)
				la.SetMarkup(Text);
			else
				la.SetText(Text);

			la.GetPixelSize(out w, out h);

			int tx = Allocation.X + (int)Xpad + (int)((float)(Allocation.Width - (int)(Xpad * 2) - w) * Xalign);
			int ty = Allocation.Y + (int)Ypad + (int)((float)(Allocation.Height - (int)(Ypad * 2) - h) * Yalign);

			{
				var color = StyleContext.GetColor(StateFlags).ToCairoColor();
				ctx.SetSourceColor(color);
				ctx.MoveTo(tx, ty);

				// In order to get the same result as in MonoDevelop.Components.DockNotebook.TabStrip.DrawTab()
				// (document tabs) we need to draw using a LinearGradient (because of issues below),
				// but we don't want to mask the actual text here, like in the doc tabs.
				// Therefore we use a LinearGradient and mask only the last vertical pixel line
				// of the label with 0.99 alpha, which forces Cairo to render the whole layout
				// in the desired way.

				// Semi-transparent gradient disables sub-pixel rendering of the label (reverting to grayscale antialiasing).
				// As Mac sub-pixel font rendering looks stronger than grayscale rendering, the label used in pad tabs
				// looked different. We need to simulate same gradient treatment as we have in document tabs.

				using (var lg = new LinearGradient(tx + w - 1, 0, tx + w, 0))
				{
					lg.AddColorStop(0, color);
					color.A = 0.99;
					lg.AddColorStop(1, color);
					ctx.SetSource(lg);
					Pango.CairoHelper.ShowLayout(ctx, la);
				}
			}

			la.Dispose();
			return true;
		}
	}
}