/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using Cairo;

namespace Pinta.Core;

/// <summary>
/// ColorDifferenctEffect is a base class for my difference effects
/// that have floating point (double) convolution filters.
/// its architecture is just like ConvolutionFilterEffect, adding a
/// function (RenderColorDifferenceEffect) called from Render in each
/// derived class.
/// It is also limited to 3x3 kernels.
/// (Chris Crosetto)
/// </summary>
public abstract class ColorDifferenceEffect : BaseEffect
{
	public void RenderColorDifferenceEffect (
		IReadOnlyList<IReadOnlyList<double>> weights,
		ImageSurface src,
		ImageSurface dest,
		ReadOnlySpan<RectangleI> rois)
	{
		RectangleI src_rect = src.GetBounds ();

		// Cache these for a massive performance boost
		var src_data = src.GetReadOnlyPixelData ();
		var dst_data = dest.GetPixelData ();
		int src_width = src.Width;

		foreach (RectangleI rect in rois) {

			foreach (var pixel in Utility.GeneratePixelOffsets (rect, src.GetSize ())) {

				int fyStart = (pixel.coordinates.Y == src_rect.Y) ? 1 : 0;
				int fyEnd = (pixel.coordinates.Y == src_rect.Y + src_rect.Height - 1) ? 2 : 3;

				int fxStart = (pixel.coordinates.X == src_rect.X) ? 1 : 0;
				int fxEnd = (pixel.coordinates.X == src_rect.X + src_rect.Width - 1) ? 2 : 3;

				dst_data[pixel.memoryOffset] = GetFinalPixelColor (weights, src_data, src_width, fxStart, fxEnd, fyStart, fyEnd, x, y);
			}
		}
	}

	private static ColorBgra GetFinalPixelColor (
		IReadOnlyList<IReadOnlyList<double>> weights,
		ReadOnlySpan<ColorBgra> src_data,
		int src_width,
		int fxStart,
		int fxEnd,
		int fyStart,
		int fyEnd,
		int x,
		int y)
	{
		// loop through each weight
		double rSum = 0.0;
		double gSum = 0.0;
		double bSum = 0.0;
		for (int fy = fyStart; fy < fyEnd; ++fy) {
			for (int fx = fxStart; fx < fxEnd; ++fx) {
				double weight = weights[fy][fx];
				ColorBgra c = src_data[(y - 1 + fy) * src_width + (x - 1 + fx)];
				rSum += weight * c.R;
				gSum += weight * c.G;
				bSum += weight * c.B;
			}
		}
		byte iRsum = Utility.ClampToByte (rSum);
		byte iGsum = Utility.ClampToByte (gSum);
		byte iBsum = Utility.ClampToByte (bSum);
		return ColorBgra.FromBgra (iBsum, iGsum, iRsum, 255);
	}
}
