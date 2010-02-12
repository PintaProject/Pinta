/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;

namespace Pinta.Core
{
    public class BrightnessContrastAdjustment
    {
		private int brightness;
		private int contrast;
        private int multiply;
        private int divide;
        private byte[] rgbTable;
       

        public BrightnessContrastAdjustment(int brightness, int contrast)
        {
            this.brightness = brightness;
			this.contrast = contrast;
        }
		
		public unsafe void Render(ImageSurface srcSurf, ImageSurface destSurf)
		{
			Calculate();
			
            for (int y = 0 ; y < srcSurf.Height; y++) {
                ColorBgra *srcRowPtr = srcSurf.GetPointAddressUnchecked(0, y);
				ColorBgra *dstRowPtr = destSurf.GetPointAddressUnchecked(0, y);
                ColorBgra *dstRowEndPtr = dstRowPtr + destSurf.Width;

                if (divide == 0) {
                    while (dstRowPtr < dstRowEndPtr) {
                        ColorBgra col = *srcRowPtr;
                        int i = col.GetIntensityByte();
                        uint c = rgbTable[i];
                        dstRowPtr->Bgra = (col.Bgra & 0xff000000) | c | (c << 8) | (c << 16);

                        ++dstRowPtr;
                        ++srcRowPtr;
                    }
                } else {
                    while (dstRowPtr < dstRowEndPtr) {
                        ColorBgra col = *srcRowPtr;
                        int i = col.GetIntensityByte();
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
		
		private void Calculate()
        {     
            if (contrast < 0) {
                multiply = contrast + 100;
                divide = 100;
            } else if (contrast > 0) {
                multiply = 100;
                divide = 100 - contrast;
            } else {
                multiply = 1;
                divide = 1;
            }

            if (rgbTable == null) {
                rgbTable = new byte[65536];
            }

            if (divide == 0) {
                for (int intensity = 0; intensity < 256; intensity++) {
                    if (intensity + brightness < 128) {
                        rgbTable[intensity] = 0;
                    } else {
                        rgbTable[intensity] = 255;
                    }
                }
            } else if (divide == 100) {
                for (int intensity = 0; intensity < 256; intensity++) {
                    int shift = (intensity - 127) * multiply / divide + 127 - intensity + brightness;

                    for (int col = 0; col < 256; ++col) {
                        int index = (intensity * 256) + col;
                        rgbTable[index] = Utility.ClampToByte(col + shift);
                    }
                }
			} else {
                for (int intensity = 0; intensity < 256; ++intensity) {
                    int shift = (intensity - 127 + brightness) * multiply / divide + 127 - intensity;

                    for (int col = 0; col < 256; ++col) {
                        int index = (intensity * 256) + col;
                        rgbTable[index] = Utility.ClampToByte(col + shift);
                    }
                }
            }   
        }
    }
}
