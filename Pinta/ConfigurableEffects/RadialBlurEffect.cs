// 
// RadialBlurEffect.cs
//  
// Author:
//       dufoli <${AuthorEmail}>
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
using Pinta.Gui.Widgets;
using Cairo;

namespace Pinta.Core
{
	public class RadialBlurEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Blurs.RadialBlur.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Radial Blur"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public RadialBlurData Data { get; private set; }
		
		public RadialBlurEffect ()
		{
			Data = new RadialBlurData ();
		}
		
		public override bool LaunchConfiguration ()
		{
			SimpleEffectDialog dialog = new SimpleEffectDialog (Text, PintaCore.Resources.GetIcon (Icon), Data);

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok) {
				dialog.Destroy ();
				return !Data.IsEmpty;
			}

			dialog.Destroy ();

			return false;
		}

		#region Algorithm Code Ported From PDN
		private static void Rotate(ref int fx, ref int fy, int fr)
        {
            int cx = fx;
            int cy = fy;

            //sin(x) ~~ x
            //cos(x)~~ 1 - x^2/2
            fx = cx - ((cy >> 8) * fr >> 8) - ((cx >> 14) * (fr * fr >> 11) >> 8);
            fy = cy + ((cx >> 8) * fr >> 8) - ((cy >> 14) * (fr * fr >> 11) >> 8);
        }
		
		public unsafe override void RenderEffect (ImageSurface imageSource, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			if (Data.Radius == 0) {
				// Copy src to dest
				return;
			}

            int w = dest.GetBounds().Width;
            int h = dest.GetBounds().Height;
            int fcx = w << 15;
            int fcy = h << 15;
            int fr = (int)((double)Data.Radius * Math.PI * 65536.0 / 181.0);
            int strideSrc = imageSource.Stride;
            int strideDst = imageSource.Stride;
            ColorBgra* srcPtr = imageSource.GetRowAddressUnchecked(0);
            ColorBgra* dstPtr = imageSource.GetRowAddressUnchecked(0);
            
            foreach (Gdk.Rectangle rect in rois) {

                for (int y = rect.Top; y < rect.Bottom; ++y)
                {
                    ColorBgra *dstRow = (ColorBgra *)(strideDst * y + (byte *)dstPtr);

                    for (int x = rect.Left; x < rect.Right; ++x)
                    {
                        int fx = (x << 16) - fcx;
                        int fy = (y << 16) - fcy;
                        const int n = 64;

                        int fsr = fr / n;

                        int sr = 0;
                        int sg = 0;
                        int sb = 0;
                        int sa = 0;
                        int sc = 0;

                        ColorBgra *src = x + (ColorBgra *)((byte *)srcPtr + strideSrc * y);

                        sr += src->R * src->A;
                        sg += src->G * src->A;
                        sb += src->B * src->A;
                        sa += src->A;
                        ++sc;

                        int ox1 = fx;
                        int ox2 = fx;
                        int oy1 = fy;
                        int oy2 = fy;

                        for (int i = 0; i < n; ++i)
                        {
                            int u;
                            int v;

                            Rotate(ref ox1, ref oy1, fsr);
                            Rotate(ref ox2, ref oy2, -fsr);
                            
                            u = ox1 + fcx + 32768 >> 16;
                            v = oy1 + fcy + 32768 >> 16;

                            if (u > 0 && v > 0 && u < w && v < h)
                            {
                                src = u + (ColorBgra *)((byte *)srcPtr + strideSrc * v);

                                sr += src->R * src->A;
                                sg += src->G * src->A;
                                sb += src->B * src->A;
                                sa += src->A;
                                ++sc;
                            }

                            u = ox2 + fcx + 32768 >> 16;
                            v = oy2 + fcy + 32768 >> 16;

                            if (u > 0 && v > 0 && u < w&& v < h)
                            {
                                src = u + (ColorBgra *)((byte *)srcPtr + strideSrc * v);

                                sr += src->R * src->A;
                                sg += src->G * src->A;
                                sb += src->B * src->A;
                                sa += src->A;
                                ++sc;
                            }
                        }
                 
                        if (sa > 0)
                        {
                            dstRow[x] = ColorBgra.FromBgra(
                                Utility.ClampToByte(sb / sa),
                                Utility.ClampToByte(sg / sa),
                                Utility.ClampToByte(sr / sa),
                                Utility.ClampToByte(sa / sc)
                                );
                        }
                        else
                        {
                            dstRow[x].Bgra = 0;
                        }
                    }
                }
            }
		}
		#endregion

		public class RadialBlurData
		{
			[MinimumValue (0), MaximumValue (360)]
			public int Radius = 2;
			
			[Skip]
			public bool IsEmpty { get { return Radius == 0; } }
		}
	}
}
