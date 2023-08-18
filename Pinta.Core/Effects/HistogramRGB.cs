/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;

namespace Pinta.Core;

/// <summary>
/// Histogram is used to calculate a histogram for a surface (in a selection,
/// if desired). This can then be used to retrieve percentile, average, peak,
/// and distribution information.
/// </summary>
public sealed class HistogramRgb
    : Histogram
{
	public HistogramRgb ()
	    : base (3, 256)
	{
		visualColors = new ColorBgra[]{
				      ColorBgra.Blue,
				      ColorBgra.Green,
				      ColorBgra.Red
				  };
	}

	public override ColorBgra GetMeanColor ()
	{
		float[] mean = GetMean ();
		return ColorBgra.FromBgr ((byte) (mean[0] + 0.5f), (byte) (mean[1] + 0.5f), (byte) (mean[2] + 0.5f));
	}

	public override ColorBgra GetPercentileColor (float fraction)
	{
		int[] perc = GetPercentile (fraction);

		return ColorBgra.FromBgr ((byte) (perc[0]), (byte) (perc[1]), (byte) (perc[2]));
	}

	protected override void AddSurfaceRectangleToHistogram (ImageSurface surface, RectangleI rect)
	{
		var histogramB = histogram[0];
		var histogramG = histogram[1];
		var histogramR = histogram[2];

		var data = surface.GetReadOnlyPixelData ();
		int rect_right = rect.Right;
		int width = surface.Width;

		for (int y = rect.Y; y <= rect.Bottom; ++y) {
			var row = data.Slice (y * width);
			for (int x = rect.X; x <= rect_right; ++x) {
				ref readonly ColorBgra c = ref row[x];
				++histogramB[c.B];
				++histogramG[c.G];
				++histogramR[c.R];
			}
		}
	}

	public void SetFromLeveledHistogram (HistogramRgb inputHistogram, UnaryPixelOps.Level upo)
	{
		if (inputHistogram == null || upo == null) {
			return;
		}

		Clear ();

		Span<float> before = stackalloc float[3];
		Span<float> slopes = stackalloc float[3];

		for (int c = 0; c < 3; c++) {
			long[] channelHistogramOutput = histogram[c];
			long[] channelHistogramInput = inputHistogram.histogram[c];

			for (int v = 0; v <= 255; v++) {
				ColorBgra after = ColorBgra.FromBgr ((byte) v, (byte) v, (byte) v);

				upo.UnApply (after, before, slopes);

				if (after[c] > upo.ColorOutHigh[c]
				    || after[c] < upo.ColorOutLow[c]
				    || (int) Math.Floor (before[c]) < 0
				    || (int) Math.Ceiling (before[c]) > 255
				    || float.IsNaN (before[c])) {
					channelHistogramOutput[v] = 0;
				} else if (before[c] <= upo.ColorInLow[c]) {
					channelHistogramOutput[v] = 0;

					for (int i = 0; i <= upo.ColorInLow[c]; i++) {
						channelHistogramOutput[v] += channelHistogramInput[i];
					}
				} else if (before[c] >= upo.ColorInHigh[c]) {
					channelHistogramOutput[v] = 0;

					for (int i = upo.ColorInHigh[c]; i < 256; i++) {
						channelHistogramOutput[v] += channelHistogramInput[i];
					}
				} else {
					channelHistogramOutput[v] = (int) (slopes[c] * Utility.Lerp (
					    channelHistogramInput[(int) Math.Floor (before[c])],
					    channelHistogramInput[(int) Math.Ceiling (before[c])],
					    before[c] - Math.Floor (before[c])));
				}
			}
		}

		OnHistogramUpdated ();
	}

	public UnaryPixelOps.Level MakeLevelsAuto ()
	{
		ColorBgra lo = GetPercentileColor (0.005f);
		ColorBgra md = GetMeanColor ();
		ColorBgra hi = GetPercentileColor (0.995f);

		return UnaryPixelOps.Level.AutoFromLoMdHi (lo, md, hi);
	}
}
