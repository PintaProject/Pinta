// 
// ZoomBlurEffect.cs
//  
// Author:
//       Hanh Pham <hanh.pham@gmx.com>
// 
// Copyright (c) 2010 Hanh Pham
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
using Pinta.Gui.Widgets;

namespace Pinta.Core
{
	public class ZoomBlurEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Blurs.ZoomBlur.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Zoom Blur"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}
        private int amount;
        private double offsetFirst;
        private double offsetSecond;

		public ZoomData Data { get; private set; }

        public ZoomBlurEffect()
		{
            Data = new ZoomData();
		}
		
		public override bool LaunchConfiguration ()
		{
			SimpleEffectDialog dialog = new SimpleEffectDialog (Text, PintaCore.Resources.GetIcon (Icon), Data);

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok) {
				dialog.Destroy ();
                return true; //!Data.IsEmpty;
			}

			dialog.Destroy ();

			return false;
		}

		#region Algorithm Code Ported From PDN

        public unsafe override void RenderEffect(ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois)
        {
            this.offsetFirst = Data.Horizontal;
            this.offsetSecond = Data.Vertical;
            this.amount = Data.ZoomAmount;

            long w = dst.Width;
            long h = dst.Height;
            long fox = (long)(dst.Width * this.offsetFirst * 32768.0);
            long foy = (long)(dst.Height * this.offsetSecond * 32768.0);
            long fcx = fox + (w << 15);
            long fcy = foy + (h << 15);
            long fz = this.amount;

            const int n = 64;

            foreach (Gdk.Rectangle rect in rois)
            {
                for (int y = rect.Top; y < rect.Bottom; ++y)
                {
                    ColorBgra* dstPtr = dst.GetPointAddressUnchecked(rect.Left, y);
                    ColorBgra* srcPtr = src.GetPointAddressUnchecked(rect.Left, y);

                    for (int x = rect.Left; x < rect.Right; ++x)
                    {
                        long fx = (x << 16) - fcx;
                        long fy = (y << 16) - fcy;

                        int sr = 0;
                        int sg = 0;
                        int sb = 0;
                        int sa = 0;
                        int sc = 0;

                        sr += srcPtr->R * srcPtr->A;
                        sg += srcPtr->G * srcPtr->A;
                        sb += srcPtr->B * srcPtr->A;
                        sa += srcPtr->A;
                        ++sc;

                        for (int i = 0; i < n; ++i)
                        {
                            fx -= ((fx >> 4) * fz) >> 10;
                            fy -= ((fy >> 4) * fz) >> 10;

                            int u = (int)(fx + fcx + 32768 >> 16);
                            int v = (int)(fy + fcy + 32768 >> 16);
                            if (src.IsVisible(u, v)) 
                            {
                                ColorBgra* srcPtr2 = src.GetPointAddressUnchecked(u, v);

                                sr += srcPtr2->R * srcPtr2->A;
                                sg += srcPtr2->G * srcPtr2->A;
                                sb += srcPtr2->B * srcPtr2->A;
                                sa += srcPtr2->A;
                                ++sc;
                            }
                        }

                        if (sa != 0)
                        {
                            *dstPtr = ColorBgra.FromBgra(
                                Utility.ClampToByte(sb / sa),
                                Utility.ClampToByte(sg / sa),
                                Utility.ClampToByte(sr / sa),
                                Utility.ClampToByte(sa / sc));
                        }
                        else
                        {
                            dstPtr->Bgra = 0;
                        }

                        ++srcPtr;
                        ++dstPtr;
                    }
                }
            }                
        }
		#endregion

        public class ZoomData
		{
			[MinimumValue (0), Caption("Zoom Amount"), MaximumValue (100)]
			public int ZoomAmount = 10;

            // TODO Create "Center" Widget like PDN
            [MinimumValue(-2), IncrementValue(0.01), DigitsValue(2) , MaximumValue(2)]
            public double Horizontal = 0;

            [MinimumValue(-2), IncrementValue(0.01), DigitsValue(2), MaximumValue(2)]
            public double Vertical = 0;
			
			//[Skip]
			//public bool IsEmpty { get { return Radius == 0; } }
		}
	}
}
