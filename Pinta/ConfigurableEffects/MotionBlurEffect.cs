// 
// MotionBlurEffect.cs
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
	public class MotionBlurEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Blurs.MotionBlur.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Motion Blur"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public MotionBlurData Data { get { return EffectData as MotionBlurData; } }

		public MotionBlurEffect ()
		{
			EffectData = new MotionBlurData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois)
		{
			PointD start = new PointD (0, 0);
			double theta = ((double)(Data.Angle + 180) * 2 * Math.PI) / 360.0;
			double alpha = (double)Data.Distance;
			PointD end = new PointD ((float)alpha * Math.Cos (theta), (float)(-alpha * Math.Sin (theta)));

			if (Data.Centered) {
				start.X = -end.X / 2.0f;
				start.Y = -end.Y / 2.0f;

				end.X /= 2.0f;
				end.Y /= 2.0f;
			}

			PointD[] points = new PointD[((1 + Data.Distance) * 3) / 2];

			if (points.Length == 1) {
				points[0] = new PointD (0, 0);
			} else {
				for (int i = 0; i < points.Length; ++i) {
					float frac = (float)i / (float)(points.Length - 1);
					points[i] = Utility.Lerp (start, end, frac);
				}
			}

			ColorBgra* samples = stackalloc ColorBgra[points.Length];

			ColorBgra* src_dataptr = (ColorBgra*)src.DataPtr;
			int src_width = src.Width;
			int src_height = src.Height;

			foreach (Gdk.Rectangle rect in rois) {

				for (int y = rect.Top; y < rect.Bottom; ++y) {
					ColorBgra* dstPtr = dst.GetPointAddressUnchecked (rect.Left, y);

					for (int x = rect.Left; x < rect.Right; ++x) {
						int sampleCount = 0;

						PointD a = new PointD ((float)x + points[0].X, (float)y + points[0].Y);
						PointD b = new PointD ((float)x + points[points.Length - 1].X, (float)y + points[points.Length - 1].Y);

						for (int j = 0; j < points.Length; ++j) {
							PointD pt = new PointD (points[j].X + (float)x, points[j].Y + (float)y);

							if (pt.X >= 0 && pt.Y >= 0 && pt.X <= (src_width - 1) && pt.Y <= (src_height - 1)) {
								samples[sampleCount] = src.GetBilinearSample (src_dataptr, src_width, src_height, (float)pt.X, (float)pt.Y);
								++sampleCount;
							}
						}

						*dstPtr = ColorBgra.Blend (samples, sampleCount);
						++dstPtr;
					}
				}
			}
		}
		#endregion

		public class MotionBlurData : EffectData
		{
			[Skip]
			public override bool IsDefault { get { return Distance == 0; } }

			public double Angle = 25;

			[MinimumValue (1), MaximumValue (200)]
			public int Distance = 10;

			public bool Centered = true;
		}
	}
}
