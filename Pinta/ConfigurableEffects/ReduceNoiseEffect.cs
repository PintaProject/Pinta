// 
// ReduceNoiseEffect.cs
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
	public class ReduceNoiseEffect : LocalHistogramEffect
	{
		private int radius;
		private double strength;

		public override string Icon {
			get { return "Menu.Effects.Noise.ReduceNoise.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Reduce Noise"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public ReduceNoiseData Data { get { return EffectData as ReduceNoiseData; } }

		public ReduceNoiseEffect ()
		{
			EffectData = new ReduceNoiseData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public override unsafe ColorBgra Apply (ColorBgra color, int area, int* hb, int* hg, int* hr, int* ha)
		{
			ColorBgra normalized = GetPercentileOfColor (color, area, hb, hg, hr, ha);
			double lerp = strength * (1 - 0.75 * color.GetIntensity ());

			return ColorBgra.Lerp (color, normalized, lerp);
		}

		private static unsafe ColorBgra GetPercentileOfColor (ColorBgra color, int area, int* hb, int* hg, int* hr, int* ha)
		{
			int rc = 0;
			int gc = 0;
			int bc = 0;

			for (int i = 0; i < color.R; ++i)
				rc += hr[i];

			for (int i = 0; i < color.G; ++i)
				gc += hg[i];

			for (int i = 0; i < color.B; ++i)
				bc += hb[i];

			rc = (rc * 255) / area;
			gc = (gc * 255) / area;
			bc = (bc * 255) / area;

			return ColorBgra.FromBgr ((byte)bc, (byte)gc, (byte)rc);
		}

		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			this.radius = Data.Radius;
			this.strength = -0.2 * Data.Strength;

			foreach (Gdk.Rectangle rect in rois)
				RenderRect (this.radius, src, dest, rect);
		}
		#endregion

		public class ReduceNoiseData : EffectData
		{
			[MinimumValue (1), MaximumValue (200)]
			public int Radius = 6;

			[MinimumValue (0), IncrementValue (0.01), DigitsValue (2), MaximumValue (1)]
			public double Strength = 0.4;
		}
	}
}
