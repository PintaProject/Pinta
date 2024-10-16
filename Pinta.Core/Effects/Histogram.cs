/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Immutable;
using Cairo;

namespace Pinta.Core;

/// <summary>
/// Histogram is used to calculate a histogram for a surface (in a selection,
/// if desired). This can then be used to retrieve percentile, average, peak,
/// and distribution information.
/// </summary>
public abstract class Histogram
{
	protected long[][] histogram;
	public long[][] HistogramValues {
		get => histogram;

		set {
			if (value.Length != histogram.Length || value[0].Length != histogram[0].Length)
				throw new ArgumentException ("value muse be an array of arrays of matching size", nameof (value));

			histogram = value;
			OnHistogramUpdated ();
		}
	}

	public int Channels => histogram.Length;

	public int Entries => histogram[0].Length;

	protected internal Histogram (int channels, int entries, ImmutableArray<ColorBgra> visualColors)
	{
		if (visualColors.IsDefault)
			throw new ArgumentException ("Not initialized", nameof (visualColors));

		visual_colors = visualColors;

		histogram = new long[channels][];

		for (int channel = 0; channel < channels; ++channel)
			histogram[channel] = new long[entries];
	}

	public event EventHandler? HistogramChanged;
	protected void OnHistogramUpdated ()
	{
		HistogramChanged?.Invoke (this, EventArgs.Empty);
	}

	protected readonly ImmutableArray<ColorBgra> visual_colors;
	public ColorBgra GetVisualColor (int channel)
		=> visual_colors[channel];

	public long GetOccurrences (int channel, int val)
		=> histogram[channel][val];

	public long GetMax ()
	{
		long max = -1;

		foreach (long[] channelHistogram in histogram)
			foreach (long i in channelHistogram)
				if (i > max)
					max = i;

		return max;
	}

	public long GetMax (int channel)
	{
		long max = -1;

		foreach (long i in histogram[channel])
			if (i > max)
				max = i;

		return max;
	}

	public ImmutableArray<float> GetMean ()
	{
		var ret = ImmutableArray.CreateBuilder<float> (Channels);
		ret.Count = Channels;

		for (int channel = 0; channel < Channels; ++channel) {
			ReadOnlySpan<long> channelHistogram = histogram[channel];
			ret[channel] = GetForChannel (channelHistogram);
		}
		return ret.MoveToImmutable ();

		// --- Methods

		static float GetForChannel (ReadOnlySpan<long> channelHistogram)
		{
			long avg = 0;
			long sum = 0;

			for (int j = 0; j < channelHistogram.Length; j++) {
				avg += j * channelHistogram[j];
				sum += channelHistogram[j];
			}

			return
				sum == 0
				? 0
				: avg / (float) sum;
		}
	}

	public ImmutableArray<int> GetPercentile (float fraction)
	{
		var ret = ImmutableArray.CreateBuilder<int> (Channels);
		ret.Count = Channels;
		for (int channel = 0; channel < Channels; ++channel) {
			ReadOnlySpan<long> channelHistogram = histogram[channel];
			ret[channel] = GetForChannel (channelHistogram);
		}
		return ret.MoveToImmutable ();

		// --- Methods

		int GetForChannel (ReadOnlySpan<long> channelHistogram)
		{
			long integral = 0;
			long sum = 0;

			for (int j = 0; j < channelHistogram.Length; j++)
				sum += channelHistogram[j];

			for (int j = 0; j < channelHistogram.Length; j++) {
				integral += channelHistogram[j];
				if (integral > sum * fraction)
					return j;
			}

			return default;
		}
	}

	public abstract ColorBgra GetMeanColor ();

	public abstract ColorBgra GetPercentileColor (float fraction);

	/// <summary>
	/// Sets the histogram to be all zeros.
	/// </summary>
	protected void Clear ()
	{
		histogram.Initialize ();
	}

	protected abstract void AddSurfaceRectangleToHistogram (ImageSurface surface, RectangleI rect);

	//public void UpdateHistogram(Surface surface)
	//{
	//    Clear();
	//    AddSurfaceRectangleToHistogram(surface, surface.Bounds);
	//    OnHistogramUpdated();
	//}

	public void UpdateHistogram (ImageSurface surface, RectangleI rect)
	{
		Clear ();
		AddSurfaceRectangleToHistogram (surface, rect);
		OnHistogramUpdated ();
	}

	//public void UpdateHistogram(Surface surface, PdnRegion roi)
	//{
	//    Clear();

	//    foreach (Rectangle rect in roi.GetRegionScansReadOnlyInt()) 
	//    {
	//        AddSurfaceRectangleToHistogram(surface, rect);
	//    }

	//    OnHistogramUpdated();
	//}
}
