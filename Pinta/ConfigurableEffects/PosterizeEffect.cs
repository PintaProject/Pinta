// 
// PosterizeEffect.cs
//  
// Author:
//       Krzysztof Marecki <marecki.krzysztof@gmail.com>
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

namespace Pinta.Core
{
	public class PosterizeEffect : BaseEffect
	{
		private int red;
		private int green;
		private int blue;

		UnaryPixelOp op;

		public override string Icon {
			get { return "Menu.Adjustments.Posterize.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Posterize"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override bool LaunchConfiguration ()
		{
			PosterizeDialog dialog = new PosterizeDialog ();

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok) {
				red = dialog.Red;
				green = dialog.Green;
				blue = dialog.Blue;

				dialog.Destroy ();

				return true;
			}

			dialog.Destroy ();

			return false;
		}

		public override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			op = new UnaryPixelOps.PosterizePixel (red, green, blue);

			op.Apply (dest, src, rois);
		}
	}
}
