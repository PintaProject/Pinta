// 
// OilPaintingEffect.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
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
	public class OilPaintingEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Artistic.OilPainting.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Oil Painting"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public OilPaintingData Data { get; private set; }

		public OilPaintingEffect ()
		{
			Data = new OilPaintingData ();
		}

		public override bool LaunchConfiguration ()
		{
			SimpleEffectDialog dialog = new SimpleEffectDialog (Text, PintaCore.Resources.GetIcon (Icon), Data);

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok) {
				dialog.Destroy ();
				return true;
			}

			dialog.Destroy ();

			return false;
		}

		#region Algorithm Code Ported From PDN
		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			int width = src.Width;
			int height = src.Height;

			int arrayLens = 1 + Data.Coarseness;

			int localStoreSize = arrayLens * 5 * sizeof (int);

			byte* localStore = stackalloc byte[localStoreSize];
			byte* p = localStore;

			int* intensityCount = (int*)p;
			p += arrayLens * sizeof (int);

			uint* avgRed = (uint*)p;
			p += arrayLens * sizeof (uint);

			uint* avgGreen = (uint*)p;
			p += arrayLens * sizeof (uint);

			uint* avgBlue = (uint*)p;
			p += arrayLens * sizeof (uint);

			uint* avgAlpha = (uint*)p;
			p += arrayLens * sizeof (uint);

			byte maxIntensity = (byte)Data.Coarseness;

			foreach (Gdk.Rectangle rect in rois) {

				int rectTop = rect.Top;
				int rectBottom = rect.Bottom;
				int rectLeft = rect.Left;
				int rectRight = rect.Right;

				ColorBgra* dst_dataptr = (ColorBgra*)dest.DataPtr;
				int dst_width = dest.Width;
				ColorBgra* src_dataptr = (ColorBgra*)src.DataPtr;
				int src_width = src.Width;
				
				for (int y = rectTop; y < rectBottom; ++y) {
					ColorBgra* dstPtr = dest.GetPointAddressUnchecked (dst_dataptr, dst_width, rect.Left, y);

					int top = y - Data.BrushSize;
					int bottom = y + Data.BrushSize + 1;

					if (top < 0) {
						top = 0;
					}

					if (bottom > height) {
						bottom = height;
					}

					for (int x = rectLeft; x < rectRight; ++x) {
						SetToZero (localStore, (ulong)localStoreSize);

						int left = x - Data.BrushSize;
						int right = x + Data.BrushSize + 1;

						if (left < 0) {
							left = 0;
						}

						if (right > width) {
							right = width;
						}

						int numInt = 0;

						for (int j = top; j < bottom; ++j) {
							ColorBgra* srcPtr = src.GetPointAddressUnchecked (src_dataptr, src_width, left, j);

							for (int i = left; i < right; ++i) {
								byte intensity = Utility.FastScaleByteByByte (srcPtr->GetIntensityByte (), maxIntensity);

								++intensityCount[intensity];
								++numInt;

								avgRed[intensity] += srcPtr->R;
								avgGreen[intensity] += srcPtr->G;
								avgBlue[intensity] += srcPtr->B;
								avgAlpha[intensity] += srcPtr->A;

								++srcPtr;
							}
						}

						byte chosenIntensity = 0;
						int maxInstance = 0;

						for (int i = 0; i <= maxIntensity; ++i) {
							if (intensityCount[i] > maxInstance) {
								chosenIntensity = (byte)i;
								maxInstance = intensityCount[i];
							}
						}

						// TODO: correct handling of alpha values?

						byte R = (byte)(avgRed[chosenIntensity] / maxInstance);
						byte G = (byte)(avgGreen[chosenIntensity] / maxInstance);
						byte B = (byte)(avgBlue[chosenIntensity] / maxInstance);
						byte A = (byte)(avgAlpha[chosenIntensity] / maxInstance);

						*dstPtr = ColorBgra.FromBgra (B, G, R, A);
						++dstPtr;
					}
				}
			}
		}
		
		// This is slow, and gets called a lot
		private unsafe static void SetToZero (byte* dst, ulong length)
		{
			int* ptr = (int*)dst;
			
			for (int i = 0; i < 1020 / 4; i++) {
				*ptr = 0;
				ptr++;
			}
		}
		#endregion

		public class OilPaintingData
		{
			[Caption ("Brush Size"), MinimumValue (1), MaximumValue (8)]
			public int BrushSize = 3;

			[MinimumValue (3), MaximumValue (255)]
			public int Coarseness = 50;
		}
	}
}
