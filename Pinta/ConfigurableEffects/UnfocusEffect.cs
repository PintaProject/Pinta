// 
// UnfocusEffect.cs
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
	public class UnfocusEffect : LocalHistogramEffect
	{
		private int radius;

		public override string Icon {
			get { return "Menu.Effects.Blurs.Unfocus.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Unfocus"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public UnfocusData Data { get { return EffectData as UnfocusData; } }

		public UnfocusEffect ()
		{
			EffectData = new UnfocusData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			this.radius = Data.Radius;

			foreach (Gdk.Rectangle rect in rois)
				RenderRectWithAlpha (this.radius, src, dest, rect);
		}

		public unsafe override ColorBgra ApplyWithAlpha (ColorBgra src, int area, int sum, int* hb, int* hg, int* hr)
		{
			//each slot of the histgram can contain up to area * 255. This will overflow an int when area > 32k
			if (area < 32768) {
				int b = 0;
				int g = 0;
				int r = 0;

				for (int i = 1; i < 256; ++i) {
					b += i * hb[i];
					g += i * hg[i];
					r += i * hr[i];
				}

				int alpha = sum / area;
				int div = area * 255;

				return ColorBgra.FromBgraClamped (b / div, g / div, r / div, alpha);
			} else {	//use a long if an int will overflow.
				long b = 0;
				long g = 0;
				long r = 0;

				for (long i = 1; i < 256; ++i) {
					b += i * hb[i];
					g += i * hg[i];
					r += i * hr[i];
				}

				int alpha = sum / area;
				int div = area * 255;

				return ColorBgra.FromBgraClamped (b / div, g / div, r / div, alpha);
			}
		}
		#endregion

		public class UnfocusData : EffectData
		{
			[MinimumValue (1), MaximumValue (200)]
			public int Radius = 4;

			[Skip]
			public override bool IsDefault { get { return Radius == 0; } }
		}
	}
}
