//
// FrostedGlassEffect.cs
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
	public class FrostedGlassEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Distort.FrostedGlass.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Frosted Glass"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public FrostedGlassData Data {
			get { return EffectData as FrostedGlassData; }
		}

		private Random random = new Random ();

		public FrostedGlassEffect () {
			EffectData = new FrostedGlassData ();
		}

		public override bool LaunchConfiguration () {
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		unsafe public override void RenderEffect (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois) {
			int width = src.Width;
			int height = src.Height;
			int r = Data.Amount;
			Random localRandom = this.random;

			int* intensityCount = stackalloc int[256];
			uint* avgRed = stackalloc uint[256];
			uint* avgGreen = stackalloc uint[256];
			uint* avgBlue = stackalloc uint[256];
			uint* avgAlpha = stackalloc uint[256];
			byte* intensityChoices = stackalloc byte[(1 + (r * 2)) * (1 + (r * 2))];

			int src_width = src.Width;
			ColorBgra* src_data_ptr = (ColorBgra*)src.DataPtr;
			
			foreach (var rect in rois) {
				int rectTop = rect.Top;
				int rectBottom = rect.Bottom;
				int rectLeft = rect.Left;
				int rectRight = rect.Right;
				
				for (int y = rectTop; y < rectBottom; ++y) {
					ColorBgra* dstPtr = dst.GetPointAddress (rect.Left, y);

					int top = y - r;
					int bottom = y + r + 1;
					
					if (top < 0) {
						top = 0;
					}
					
					if (bottom > height) {
						bottom = height;
					}
					
					for (int x = rectLeft; x < rectRight; ++x) {
						int intensityChoicesIndex = 0;
						
						for (int i = 0; i < 256; ++i) {
							intensityCount[i] = 0;
							avgRed[i] = 0;
							avgGreen[i] = 0;
							avgBlue[i] = 0;
							avgAlpha[i] = 0;
						}
						
						int left = x - r;
						int right = x + r + 1;
						
						if (left < 0) {
							left = 0;
						}
						
						if (right > width) {
							right = width;
						}
						
						for (int j = top; j < bottom; ++j) {
							if (j < 0 || j >= height) {
								continue;
							}
							
							ColorBgra* srcPtr = src.GetPointAddressUnchecked (src_data_ptr, src_width, left, j);
							
							for (int i = left; i < right; ++i) {
								byte intensity = srcPtr->GetIntensityByte ();
								
								intensityChoices[intensityChoicesIndex] = intensity;
								++intensityChoicesIndex;
								
								++intensityCount[intensity];
								
								avgRed[intensity] += srcPtr->R;
								avgGreen[intensity] += srcPtr->G;
								avgBlue[intensity] += srcPtr->B;
								avgAlpha[intensity] += srcPtr->A;
								
								++srcPtr;
							}
						}
						
						int randNum;
						
						lock (localRandom) {
							randNum = localRandom.Next (intensityChoicesIndex);
						}
						
						byte chosenIntensity = intensityChoices[randNum];
						
						byte R = (byte)(avgRed[chosenIntensity] / intensityCount[chosenIntensity]);
						byte G = (byte)(avgGreen[chosenIntensity] / intensityCount[chosenIntensity]);
						byte B = (byte)(avgBlue[chosenIntensity] / intensityCount[chosenIntensity]);
						byte A = (byte)(avgAlpha[chosenIntensity] / intensityCount[chosenIntensity]);
						
						*dstPtr = ColorBgra.FromBgra (B, G, R, A);
						++dstPtr;
						
						// prepare the array for the next loop iteration
						for (int i = 0; i < intensityChoicesIndex; ++i) {
							intensityChoices[i] = 0;
						}
					}
				}
			}
		}
		#endregion

		public class FrostedGlassData : EffectData
		{
			[MinimumValue(1), MaximumValue(10)]
			public int Amount = 1;
		}
	}
}
