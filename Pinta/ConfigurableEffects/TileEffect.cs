//
// TileEffect.cs
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
	public class TileEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Distort.Tile.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Tile Reflection"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public TileData Data {
			get { return EffectData as TileData; }
		}

		public TileEffect () {
			EffectData = new TileData ();
		}

		public override bool LaunchConfiguration () {
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		unsafe public override void RenderEffect (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois) {
			int width = dst.Width;
			int height = dst.Height;
			float hw = width / 2f;
			float hh = height / 2f;
			float sin = (float)Math.Sin (Data.Rotation * Math.PI / 180.0);
			float cos = (float)Math.Cos (Data.Rotation * Math.PI / 180.0);
			float scale = (float)Math.PI / Data.TileSize;
			float intensity = Data.Intensity;
			
			intensity = intensity * intensity / 10 * Math.Sign (intensity);
			
			int aaLevel = 4;
			int aaSamples = aaLevel * aaLevel + 1;
			PointD* aaPoints = stackalloc PointD[aaSamples];
			
			for (int i = 0; i < aaSamples; ++i) {
				double x = (i * aaLevel) / (double)aaSamples;
				double y = i / (double)aaSamples;
				
				x -= (int)x;
				
				// RGSS + rotation to maximize AA quality
				aaPoints[i] = new PointD ((double)(cos * x + sin * y), (double)(cos * y - sin * x));
			}
			
			int src_width = src.Width;
			ColorBgra* src_data_ptr = (ColorBgra*)src.DataPtr;

			foreach (var rect in rois) {
				for (int y = rect.Top; y < rect.Bottom; y++) {
					float j = y - hh;
					ColorBgra* dstPtr = dst.GetPointAddressUnchecked (rect.Left, y);
					
					for (int x = rect.Left; x < rect.Right; x++) {
						int b = 0;
						int g = 0;
						int r = 0;
						int a = 0;
						float i = x - hw;
						
						for (int p = 0; p < aaSamples; ++p) {
							PointD pt = aaPoints[p];
							
							float u = i + (float)pt.X;
							float v = j - (float)pt.Y;
							
							float s = cos * u + sin * v;
							float t = -sin * u + cos * v;
							
							s += intensity * (float)Math.Tan (s * scale);
							t += intensity * (float)Math.Tan (t * scale);
							u = cos * s - sin * t;
							v = sin * s + cos * t;
							
							int xSample = (int)(hw + u);
							int ySample = (int)(hh + v);
							
							xSample = (xSample + width) % width;
							// This makes it a little faster
							if (xSample < 0) {
								xSample = (xSample + width) % width;
							}
							
							ySample = (ySample + height) % height;
							// This makes it a little faster
							if (ySample < 0) {
								ySample = (ySample + height) % height;
							}
							
							ColorBgra sample = *src.GetPointAddressUnchecked (src_data_ptr, src_width, xSample, ySample);
							
							b += sample.B;
							g += sample.G;
							r += sample.R;
							a += sample.A;
						}
						
						*(dstPtr++) = ColorBgra.FromBgra ((byte)(b / aaSamples), (byte)(g / aaSamples),
							(byte)(r / aaSamples), (byte)(a / aaSamples));
					}
				}
			}
		}
		#endregion


		public class TileData : EffectData
		{
			[MinimumValue(-45), MaximumValue(45)]
			public double Rotation = 30;
			[MinimumValue(2), MaximumValue(200)]
			public int TileSize = 40;
			[MinimumValue(-20), MaximumValue(20)]
			public int Intensity = 8;
		}
	}
}
