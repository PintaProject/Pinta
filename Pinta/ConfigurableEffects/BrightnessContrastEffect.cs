// 
// BrightnessContrastEffect.cs
//  
// Author:
//       Krzysztof Marecki <marecki.krzysztof@gmail.com>
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
	public class BrightnessContrastEffect : BaseEffect
	{
		private int multiply;
		private int divide;
		private byte[] rgbTable;

		public override string Icon {
			get { return "Menu.Adjustments.BrightnessAndContrast.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Brightness / Contrast"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public BrightnessContrastData Data { get; private set; }
		
		public BrightnessContrastEffect ()
		{
			Data = new BrightnessContrastData ();
		}
		
		public override bool LaunchConfiguration ()
		{
			Data = new BrightnessContrastData ();
			SimpleEffectDialog dialog = new SimpleEffectDialog (Text, PintaCore.Resources.GetIcon (Icon), Data);
			
			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok) {
				dialog.Destroy ();
				
				// Don't trigger anything if no options were changed
				return !Data.IsDefault;
			}

			dialog.Destroy ();

			return false;
		}

		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			Calculate ();

			for (int y = 0; y < src.Height; y++) {
				ColorBgra* srcRowPtr = src.GetPointAddressUnchecked (0, y);
				ColorBgra* dstRowPtr = dest.GetPointAddressUnchecked (0, y);
				ColorBgra* dstRowEndPtr = dstRowPtr + dest.Width;

				if (divide == 0) {
					while (dstRowPtr < dstRowEndPtr) {
						ColorBgra col = *srcRowPtr;
						int i = col.GetIntensityByte ();
						uint c = rgbTable[i];
						dstRowPtr->Bgra = (col.Bgra & 0xff000000) | c | (c << 8) | (c << 16);

						++dstRowPtr;
						++srcRowPtr;
					}
				} else {
					while (dstRowPtr < dstRowEndPtr) {
						ColorBgra col = *srcRowPtr;
						int i = col.GetIntensityByte ();
						int shiftIndex = i * 256;

						col.R = rgbTable[shiftIndex + col.R];
						col.G = rgbTable[shiftIndex + col.G];
						col.B = rgbTable[shiftIndex + col.B];

						*dstRowPtr = col;
						++dstRowPtr;
						++srcRowPtr;
					}
				}
			}
		}

		private void Calculate ()
		{
			if (Data.Contrast < 0) {
				multiply = Data.Contrast + 100;
				divide = 100;
			} else if (Data.Contrast > 0) {
				multiply = 100;
				divide = 100 - Data.Contrast;
			} else {
				multiply = 1;
				divide = 1;
			}

			if (rgbTable == null)
				rgbTable = new byte[65536];

			if (divide == 0) {
				for (int intensity = 0; intensity < 256; intensity++) {
					if (intensity + Data.Brightness < 128)
						rgbTable[intensity] = 0;
					else
						rgbTable[intensity] = 255;
				}
			} else if (divide == 100) {
				for (int intensity = 0; intensity < 256; intensity++) {
					int shift = (intensity - 127) * multiply / divide + 127 - intensity + Data.Brightness;

					for (int col = 0; col < 256; ++col) {
						int index = (intensity * 256) + col;
						rgbTable[index] = Utility.ClampToByte (col + shift);
					}
				}
			} else {
				for (int intensity = 0; intensity < 256; ++intensity) {
					int shift = (intensity - 127 + Data.Brightness) * multiply / divide + 127 - intensity;

					for (int col = 0; col < 256; ++col) {
						int index = (intensity * 256) + col;
						rgbTable[index] = Utility.ClampToByte (col + shift);
					}
				}
			}
		}
		
		public class BrightnessContrastData
		{
			public int Brightness = 0;
			public int Contrast = 0;
			
			[Skip]
			public bool IsDefault {
				get { return Brightness == 0 && Contrast == 0; }
			}
		}
	}
}
