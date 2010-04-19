// 
// BulgeEffect.cs
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
using Cairo;
using Pinta.Gui.Widgets;

namespace Pinta.Core
{
	public class BulgeEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Distort.Bulge.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Bulge"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public BulgeData Data {
			get { return EffectData as BulgeData; }
		}

		public BulgeEffect ()
		{
			EffectData = new BulgeData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		unsafe public override void RenderEffect (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois)
		{
			float bulge = (float)Data.Amount;
			
			float hw = dst.Width / 2f;
			float hh = dst.Height / 2f;
			float maxrad = Math.Min (hw, hh);
			float maxrad2 = maxrad * maxrad;
			float amt = Data.Amount / 100f;
			
			hh = hh + (float)Data.Offset.Y * hh;
			hw = hw + (float)Data.Offset.X * hw;

			int src_width = src.Width;
			int src_height = src.Height;
			ColorBgra* src_data_ptr = (ColorBgra*)src.DataPtr;
			
			foreach (Gdk.Rectangle rect in rois) {
				
				for (int y = rect.Top; y < rect.Bottom; y++) {
					
					ColorBgra* dstPtr = dst.GetPointAddressUnchecked (rect.Left, y);
					ColorBgra* srcPtr = src.GetPointAddressUnchecked (src_data_ptr, src_width, rect.Left, y);
					float v = y - hh;
					
					for (int x = rect.Left; x < rect.Right; x++) {
						float u = x - hw;
						float r = (float)Math.Sqrt (u * u + v * v);
						float rscale1 = (1f - (r / maxrad));
						
						if (rscale1 > 0) {
							float rscale2 = 1 - amt * rscale1 * rscale1;
							
							float xp = u * rscale2;
							float yp = v * rscale2;
							
							*dstPtr = src.GetBilinearSampleClamped (src_data_ptr, src_width, src_height, xp + hw, yp + hh);
						} else {
							*dstPtr = *srcPtr;
						}
						
						++dstPtr;
						++srcPtr;
					}
				}
			}
		}
		#endregion

		public class BulgeData : EffectData
		{
			[MinimumValue(-200), MaximumValue(100)]
			public int Amount = 45;

			public Cairo.PointD Offset = new Cairo.PointD (0.0, 0.0);

			[Skip]
			public override bool IsDefault {
				get { return Amount == 0; }
			}
		}
	}
}
