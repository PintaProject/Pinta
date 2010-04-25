// 
// GridRenderer.cs
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
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	class GridRenderer
	{
		private CanvasRenderer cr;

		public GridRenderer (CanvasRenderer cr)
		{
			this.cr = cr;
		}

		public unsafe void Render (Cairo.ImageSurface dst, Gdk.Point offset)
		{
			if (cr.ScaleFactor > new ScaleFactor (1, 2))
				return;

			int[] d2SLookupX = cr.Dst2SrcLookupX;
			int[] d2SLookupY = cr.Dst2SrcLookupY;
			int[] s2DLookupX = cr.Src2DstLookupX;
			int[] s2DLookupY = cr.Src2DstLookupY;

			ColorBgra[] blackAndWhite = new ColorBgra[2] { ColorBgra.White, ColorBgra.Black };

			// draw horizontal lines
			int dstHeight = dst.Height;
			int dstWidth = dst.Width;
			int dstStride = dst.Stride;
			int sTop = d2SLookupY[offset.Y];
			int sBottom = d2SLookupY[offset.Y + dstHeight];

			for (int srcY = sTop; srcY <= sBottom; ++srcY) {
				int dstY = s2DLookupY[srcY];
				int dstRow = dstY - offset.Y;

				if (dstRow >= 0 && dstRow < dstHeight) {
					ColorBgra* dstRowPtr = dst.GetRowAddressUnchecked (dstRow);
					ColorBgra* dstRowEndPtr = dstRowPtr + dstWidth;

					dstRowPtr += offset.X & 1;

					while (dstRowPtr < dstRowEndPtr) {
						*dstRowPtr = ColorBgra.Black;
						dstRowPtr += 2;
					}
				}
			}

			// draw vertical lines
			int sLeft = d2SLookupX[offset.X];
			int sRight = d2SLookupX[offset.X + dstWidth];

			for (int srcX = sLeft; srcX <= sRight; ++srcX) {
				int dstX = s2DLookupX[srcX];
				int dstCol = dstX - offset.X;

				if (dstCol >= 0 && dstCol < dstWidth) {
					byte* dstColPtr = (byte*)dst.GetPointAddress (dstCol, 0);
					byte* dstColEndPtr = dstColPtr + dstStride * dstHeight;

					dstColPtr += (offset.Y & 1) * dstStride;

					while (dstColPtr < dstColEndPtr) {
						*((ColorBgra*)dstColPtr) = ColorBgra.Black;
						dstColPtr += 2 * dstStride;
					}
				}
			}
		}
	}
}
