/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

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

			dst.Flush ();

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
			
			dst.MarkDirty ();
		}
	}
}
