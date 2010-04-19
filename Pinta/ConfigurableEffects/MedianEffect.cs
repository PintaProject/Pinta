// 
// MedianEffect.cs
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
	public class MedianEffect : LocalHistogramEffect
	{
		private int radius;
		private int percentile;

		public override string Icon {
			get { return "Menu.Effects.Noise.Median.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Median"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public MedianData Data { get { return EffectData as MedianData; } }

		public MedianEffect ()
		{
			EffectData = new MedianData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			this.radius = Data.Radius;
			this.percentile = Data.Percentile;

			foreach (Gdk.Rectangle rect in rois)
				RenderRect (this.radius, src, dest, rect);
		}

		public unsafe override ColorBgra Apply (ColorBgra src, int area, int* hb, int* hg, int* hr, int* ha)
		{
			ColorBgra c = GetPercentile (this.percentile, area, hb, hg, hr, ha);
			return c;
		}
		#endregion

		public class MedianData : EffectData
		{
			[MinimumValue (1), MaximumValue (200)]
			public int Radius = 10;

			[MinimumValue (0), MaximumValue (100)]
			public int Percentile = 50;

			[Skip]
			public override bool IsDefault { get { return Radius == 0; } }
		}
	}
}
