// 
// OutlineEffect.cs
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
	public class OutlineEffect : LocalHistogramEffect
	{
		private int thickness;
		private int intensity;

		public override string Icon {
			get { return "Menu.Effects.Stylize.Outline.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Outline"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public OutlineData Data { get { return EffectData as OutlineData; } }

		public OutlineEffect ()
		{
			EffectData = new OutlineData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public unsafe override ColorBgra Apply (ColorBgra src, int area, int* hb, int* hg, int* hr, int* ha)
		{
			int minCount1 = area * (100 - this.intensity) / 200;
			int minCount2 = area * (100 + this.intensity) / 200;

			int bCount = 0;
			int b1 = 0;
			
			while (b1 < 255 && hb[b1] == 0)
				++b1;

			while (b1 < 255 && bCount < minCount1) {
				bCount += hb[b1];
				++b1;
			}

			int b2 = b1;
			while (b2 < 255 && bCount < minCount2) {
				bCount += hb[b2];
				++b2;
			}

			int gCount = 0;
			int g1 = 0;
			while (g1 < 255 && hg[g1] == 0)
				++g1;

			while (g1 < 255 && gCount < minCount1) {
				gCount += hg[g1];
				++g1;
			}

			int g2 = g1;
			while (g2 < 255 && gCount < minCount2) {
				gCount += hg[g2];
				++g2;
			}

			int rCount = 0;
			int r1 = 0;
			while (r1 < 255 && hr[r1] == 0)
				++r1;

			while (r1 < 255 && rCount < minCount1) {
				rCount += hr[r1];
				++r1;
			}

			int r2 = r1;
			while (r2 < 255 && rCount < minCount2) {
				rCount += hr[r2];
				++r2;
			}

			int aCount = 0;
			int a1 = 0;
			while (a1 < 255 && hb[a1] == 0)
				++a1;

			while (a1 < 255 && aCount < minCount1) {
				aCount += ha[a1];
				++a1;
			}

			int a2 = a1;
			while (a2 < 255 && aCount < minCount2) {
				aCount += ha[a2];
				++a2;
			}

			return ColorBgra.FromBgra (
			    (byte)(255 - (b2 - b1)),
			    (byte)(255 - (g2 - g1)),
			    (byte)(255 - (r2 - r1)),
			    (byte)(a2));
		}

		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			this.thickness = Data.Thickness;
			this.intensity = Data.Intensity;

			foreach (Gdk.Rectangle rect in rois)
				RenderRect (this.thickness, src, dest, rect);
		}

		#endregion

		public class OutlineData : EffectData
		{
			[MinimumValue (1), MaximumValue (200)]
			public int Thickness = 3;

			[MinimumValue (0), MaximumValue (100)]
			public int Intensity = 50;

			[Skip]
			public override bool IsDefault { get { return Thickness == 0; } }
		}
	}
}
