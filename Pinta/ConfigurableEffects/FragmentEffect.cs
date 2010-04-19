// 
// FragmentEffect.cs
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
	public class FragmentEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Blurs.Fragment.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Fragment"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public FragmentData Data { get { return EffectData as FragmentData; } }

		public FragmentEffect ()
		{
			EffectData = new FragmentData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		private Gdk.Point[] RecalcPointOffsets (int fragments, double rotationAngle, int distance)
		{
			double pointStep = 2 * Math.PI / (double)fragments;
			double rotationRadians = ((rotationAngle - 90.0) * Math.PI) / 180.0;
			double offsetAngle = pointStep;

			Gdk.Point[] pointOffsets = new Gdk.Point[fragments];

			for (int i = 0; i < fragments; i++) {
				double currentRadians = rotationRadians + (pointStep * i);

				pointOffsets[i] = new Gdk.Point (
				    (int)Math.Round (distance * -Math.Sin (currentRadians), MidpointRounding.AwayFromZero),
				    (int)Math.Round (distance * -Math.Cos (currentRadians), MidpointRounding.AwayFromZero));
			}
			
			return pointOffsets;
		}

		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois)
		{
			Gdk.Point[] pointOffsets = RecalcPointOffsets (Data.Fragments, Data.Rotation, Data.Distance);

			int poLength = pointOffsets.Length;
			Gdk.Point* pointOffsetsPtr = stackalloc Gdk.Point[poLength];
			
			for (int i = 0; i < poLength; ++i)
				pointOffsetsPtr[i] = pointOffsets[i];

			ColorBgra* samples = stackalloc ColorBgra[poLength];

			// Cache these for a massive performance boost
			int src_width = src.Width;
			int src_height = src.Height;
			ColorBgra* src_data_ptr = (ColorBgra*)src.DataPtr;

			foreach (Gdk.Rectangle rect in rois) {
				for (int y = rect.Top; y < rect.Bottom; y++) {
					ColorBgra* dstPtr = dst.GetPointAddressUnchecked (rect.Left, y);

					for (int x = rect.Left; x < rect.Right; x++) {
						int sampleCount = 0;

						for (int i = 0; i < poLength; ++i) {
							int u = x - pointOffsetsPtr[i].X;
							int v = y - pointOffsetsPtr[i].Y;

							if (u >= 0 && u < src_width && v >= 0 && v < src_height) {
								samples[sampleCount] = src.GetPointUnchecked (src_data_ptr, src_width, u, v);
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

		public class FragmentData : EffectData
		{
			[MinimumValue (2), MaximumValue (50)]
			public int Fragments = 4;

			[MinimumValue (0), MaximumValue (100)]
			public int Distance = 8;

			public double Rotation = 0;
		}
	}
}
