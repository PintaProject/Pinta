// 
// RadialBlurEffect.cs
//  
// Author:
//       Olivier Dufour <olivier.duff@gmail.com>
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

		public RadialBlurData Data { get { return EffectData as RadialBlurData; } }

		public RadialBlurEffect ()
		{
			EffectData = new RadialBlurData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		private static void Rotate (ref int fx, ref int fy, int fr)
		{
			int cx = fx;
			int cy = fy;

			//sin(x) ~~ x
			//cos(x)~~ 1 - x^2/2
			fx = cx - ((cy >> 8) * fr >> 8) - ((cx >> 14) * (fr * fr >> 11) >> 8);
			fy = cy + ((cx >> 8) * fr >> 8) - ((cy >> 14) * (fr * fr >> 11) >> 8);
		}

		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois)
		{
			if (Data.Angle == 0) {
				// Copy src to dest
				return;
			}

			int w = dst.Width;
			int h = dst.Height;
			int fcx = (w << 15) + (int)(Data.Offset.X * (w << 15));
			int fcy = (h << 15) + (int)(Data.Offset.Y * (h << 15));

			int n = (Data.Quality * Data.Quality) * (30 + Data.Quality * Data.Quality);

			int fr = (int)(Data.Angle * Math.PI * 65536.0 / 181.0);

			foreach (Gdk.Rectangle rect in rois) {
				for (int y = rect.Top; y < rect.Bottom; ++y) {
					ColorBgra* dstPtr = dst.GetPointAddressUnchecked (rect.Left, y);
					ColorBgra* srcPtr = src.GetPointAddressUnchecked (rect.Left, y);

					for (int x = rect.Left; x < rect.Right; ++x) {
						int fx = (x << 16) - fcx;
						int fy = (y << 16) - fcy;

						int fsr = fr / n;

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

						int ox1 = fx;
						int ox2 = fx;
						int oy1 = fy;
						int oy2 = fy;

						ColorBgra* src_dataptr = (ColorBgra*)src.DataPtr;
						int src_width = src.Width;

						for (int i = 0; i < n; ++i) {
							Rotate (ref ox1, ref oy1, fsr);
							Rotate (ref ox2, ref oy2, -fsr);

							int u1 = ox1 + fcx + 32768 >> 16;
							int v1 = oy1 + fcy + 32768 >> 16;

							if (u1 > 0 && v1 > 0 && u1 < w && v1 < h) {
								ColorBgra* sample = src.GetPointAddressUnchecked (src_dataptr, src_width, u1, v1);

								sr += sample->R * sample->A;
								sg += sample->G * sample->A;
								sb += sample->B * sample->A;
								sa += sample->A;
								++sc;
							}

							int u2 = ox2 + fcx + 32768 >> 16;
							int v2 = oy2 + fcy + 32768 >> 16;

							if (u2 > 0 && v2 > 0 && u2 < w && v2 < h) {
								ColorBgra* sample = src.GetPointAddressUnchecked (src_dataptr, src_width, u2, v2);

								sr += sample->R * sample->A;
								sg += sample->G * sample->A;
								sb += sample->B * sample->A;
								sa += sample->A;
								++sc;
							}
						}

						if (sa > 0) {
							*dstPtr = ColorBgra.FromBgra (
							    Utility.ClampToByte (sb / sa),
							    Utility.ClampToByte (sg / sa),
							    Utility.ClampToByte (sr / sa),
							    Utility.ClampToByte (sa / sc));
						} else {
							dstPtr->Bgra = 0;
						}

						++dstPtr;
						++srcPtr;
					}
				}
			}
		}
		#endregion

		public class RadialBlurData : EffectData
		{
			public Double Angle = 2;
			
			public PointD Offset = new PointD (0, 0);

			[MinimumValue (1), MaximumValue (5)]
			[Hint ("\nUse low quality for previews, small images, and small angles.  Use high quality for final quality, large images, and large angles.")]
			public int Quality = 2;
			
			[Skip]
			public override bool IsDefault { get { return Angle == 0; } }
		}
	}
}
