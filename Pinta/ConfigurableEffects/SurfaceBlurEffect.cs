// 
// SurfaceBlurEffect.cs
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
	public class SurfaceBlurEffect : LocalHistogramEffect
	{
		private int radius;
		private int threshold;
		private int[] intensityFunction;

		public override string Icon {
			get { return "Menu.Effects.Blurs.SurfaceBlur.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Surface Blur"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public SurfaceBlurData Data { get { return EffectData as SurfaceBlurData; } }

		public SurfaceBlurEffect ()
		{
			EffectData = new SurfaceBlurData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		// rather than a fancy function such as a gaussian, 
		// currently using a trigular function as this seems to be 'good-enough'
		private static int[] PrecalculateIntensityFunction (int threshold)
		{
			int[] factors = new int[256];

			double slope = 96d / threshold;

			for (int i = 0; i < 256; i++) {
				int factor = (int)Math.Round (255 - (i * slope), MidpointRounding.AwayFromZero);

				if (factor < 0)
					factor = 0;

				factors[i] = factor;
			}

			return factors;
		}

		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			this.radius = Data.Radius;
			this.threshold = Data.Threshold;
			this.intensityFunction = PrecalculateIntensityFunction (this.threshold);

			foreach (Gdk.Rectangle rect in rois)
				RenderRect (this.radius, src, dest, rect);

		}

		public override unsafe ColorBgra Apply (ColorBgra src, int area, int* hb, int* hg, int* hr, int* ha)
		{
			int resultB = BlurChannel (src.B, hb);
			int resultG = BlurChannel (src.G, hg);
			int resultR = BlurChannel (src.R, hr);

			// there is no way we can deal with pre-multiplied alphas; the correlation 
			// between channels no longer exists by this point in the algorithm... 
			// so, just use the alpha from the source pixel.
			ColorBgra result = ColorBgra.FromBgra ((byte)resultB, (byte)resultG, (byte)resultR, src.A);

			return result;
		}

		private unsafe int BlurChannel (int current, int* histogram)
		{
			// note to self: pointers are passed by-value...
			//               incrementing passed pointer - no effect outside current scope
			int sum = 0;
			int divisor = 0;
			int result = current;

			for (int bin = 0; bin < 256; bin++) {
				if (*histogram > 0) {
					int diff;

					if (bin > current)
						diff = bin - current;
					else
						diff = current - bin;

					int intensity = this.intensityFunction[diff];

					if (intensity > 0) {
						int t = (*histogram) * intensity;
						sum += (t * bin);
						divisor += t;
					}
				}

				++histogram;
			}

			if (divisor > 0) {
				// 1/2 LSB for integer rounding
				int roundingTerm = divisor >> 1;
				result = (sum + roundingTerm) / divisor;
			}

			return result;
		}
		#endregion

		public class SurfaceBlurData : EffectData
		{
			[MinimumValue (1), MaximumValue (100)]
			public int Radius = 6;

			[MinimumValue (1), MaximumValue (100)]
			public int Threshold = 15;

			[Skip]
			public override bool IsDefault { get { return Radius == 0; } }
		}
	}
}