// 
// TwistEffect.cs
//  
// Author:
//       Marco Rolappe <m_rolappe@gmx.net>
// 
// Copyright (c) 2010 Marco Rolappe
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
	public class TwistEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Distort.Twist.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Twist"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public TwistData Data { get { return EffectData as TwistData; } }

		public TwistEffect ()
		{
			EffectData = new TwistData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois)
		{
			float twist = Data.Amount;

			float hw = dst.Width / 2.0f;
			float hh = dst.Height / 2.0f;
			float maxrad = Math.Min (hw, hh);

			twist = twist * twist * Math.Sign (twist);

			int aaLevel = Data.Antialias;
			int aaSamples = aaLevel * aaLevel + 1;
			PointD* aaPoints = stackalloc PointD[aaSamples];

			for (int i = 0; i < aaSamples; ++i) {
				PointD pt = new PointD (
				    ((i * aaLevel) / (float)aaSamples),
				    i / (float)aaSamples);

				pt.X -= (int)pt.X;
				aaPoints[i] = pt;
			}

			int src_width = src.Width;
			ColorBgra* src_data_ptr = (ColorBgra*)src.DataPtr;

			foreach (var rect in rois) {
				for (int y = rect.Top; y < rect.Bottom; y++) {
					float j = y - hh;
					ColorBgra* dstPtr = dst.GetPointAddressUnchecked (rect.Left, y);
					ColorBgra* srcPtr = src.GetPointAddressUnchecked (src_data_ptr, src_width, rect.Left, y);

					for (int x = rect.Left; x < rect.Right; x++) {
						float i = x - hw;

						if (i * i + j * j > (maxrad + 1) * (maxrad + 1)) {
							*dstPtr = *srcPtr;
						} else {
							int b = 0;
							int g = 0;
							int r = 0;
							int a = 0;

							for (int p = 0; p < aaSamples; ++p) {
								float u = i + (float)aaPoints[p].X;
								float v = j + (float)aaPoints[p].Y;
								double rad = Math.Sqrt (u * u + v * v);
								double theta = Math.Atan2 (v, u);

								double t = 1 - rad / maxrad;

								t = (t < 0) ? 0 : (t * t * t);

								theta += (t * twist) / 100;

								ColorBgra sample = src.GetPointUnchecked (src_data_ptr, src_width, 
								    (int)(hw + (float)(rad * Math.Cos (theta))),
								    (int)(hh + (float)(rad * Math.Sin (theta))));

								b += sample.B;
								g += sample.G;
								r += sample.R;
								a += sample.A;
							}

							*dstPtr = ColorBgra.FromBgra (
							    (byte)(b / aaSamples),
							    (byte)(g / aaSamples),
							    (byte)(r / aaSamples),
							    (byte)(a / aaSamples));
						}

						++dstPtr;
						++srcPtr;
					}
				}
			}
		}
		#endregion

		public class TwistData : EffectData
		{
			[MinimumValue (-100), MaximumValue (100)]
			public int Amount = 45;
			[MinimumValue (0), MaximumValue (5)]
			public int Antialias = 2;
		}
	}
}
