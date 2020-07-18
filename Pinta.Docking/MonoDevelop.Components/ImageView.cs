//
// ImageView.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using Pinta.Docking;

namespace MonoDevelop.Components
{
	class ImageView: Gtk.DrawingArea
	{
        Gdk.Pixbuf image;

		public ImageView ()
		{
			AppPaintable = true;
			HasWindow = false;
		}

		public ImageView (Gdk.Pixbuf image): this ()
		{
			this.image = image;
		}

		public Gdk.Pixbuf Image {
			get { return image; }
			set {
				image = value;
				QueueDraw ();
				QueueResize ();
			}
		}

		float xalign = 0.5f;
		public float Xalign {
			get { return xalign; }
			set {
				xalign = (float)(value * IconScale);
				QueueDraw ();
			}
		}

		float yalign = 0.5f;
		public float Yalign {
			get { return yalign; }
			set {
				yalign = (float)(value * IconScale);
				QueueDraw ();
			}
		}

		double IconScale {
            get { return GtkWorkarounds.GetPixelScale (); }
		}

        protected override void OnGetPreferredWidth(out int minimum_width, out int natural_width)
        {
			if (image != null)
				minimum_width = natural_width = (int)(image.Width * IconScale);
			else
				base.OnGetPreferredWidth(out minimum_width, out natural_width);
        }

        protected override void OnGetPreferredHeight(out int minimum_height, out int natural_height)
        {
			if (image != null)
				minimum_height = natural_height = (int)(image.Height * IconScale);
			else
				base.OnGetPreferredHeight(out minimum_height, out natural_height);
        }

        protected override bool OnDrawn(Context ctx)
        {
			base.OnDrawn(ctx);
            if (image != null)
            {
                var x = Math.Round((Allocation.Width - image.Width * IconScale) * Xalign);
                var y = Math.Round((Allocation.Height - image.Height * IconScale) * Yalign);
                ctx.Save();
                ctx.Scale(IconScale, IconScale);
                ctx.DrawImage(this, image, x / IconScale, y / IconScale);
                ctx.Restore();
            }
            return true;
        }
    }
}

