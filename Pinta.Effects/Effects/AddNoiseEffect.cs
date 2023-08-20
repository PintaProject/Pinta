/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Hanh Pham <hanh.pham@gmx.com>                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Immutable;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class AddNoiseEffect : BaseEffect
{
	private int intensity;
	private int color_saturation;
	private double coverage;

	public override string Icon => Pinta.Resources.Icons.EffectsNoiseAddNoise;

	public override string Name => Translations.GetString ("Add Noise");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Noise");

	public NoiseData Data => (NoiseData) EffectData!;  // NRT - Set in constructor

	static AddNoiseEffect ()
	{
		lookup = CreateLookup ();
	}

	public AddNoiseEffect ()
	{
		EffectData = new NoiseData ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

	#region Algorithm Code Ported From PDN
	[ThreadStatic]
	private static Random thread_rand = new ();
	private const int TableSize = 16384;
	private static readonly ImmutableArray<int> lookup;

	private static double NormalCurve (double x, double scale)
	{
		return scale * Math.Exp (-x * x / 2);
	}

	private static ImmutableArray<int> CreateLookup ()
	{
		double l = 5;
		double r = 10;
		double scale = 50;
		double sum = 0;

		while (r - l > 0.0000001) {
			sum = 0;
			scale = (l + r) * 0.5;

			for (int i = 0; i < TableSize; ++i) {
				sum += NormalCurve (16.0 * ((double) i - TableSize / 2) / TableSize, scale);

				if (sum > 1000000) {
					break;
				}
			}

			if (sum > TableSize) {
				r = scale;
			} else if (sum < TableSize) {
				l = scale;
			} else {
				break;
			}
		}

		var result = ImmutableArray.CreateBuilder<int> (TableSize);
		result.Count = TableSize;
		sum = 0;
		int roundedSum = 0, lastRoundedSum;

		for (int i = 0; i < TableSize; ++i) {
			sum += NormalCurve (16.0 * ((double) i - TableSize / 2) / TableSize, scale);
			lastRoundedSum = roundedSum;
			roundedSum = (int) sum;

			for (int j = lastRoundedSum; j < roundedSum; ++j) {
				result[j] = (i - TableSize / 2) * 65536 / TableSize;
			}
		}

		return result.ToImmutable ();
	}

	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		this.intensity = Data.Intensity;
		this.color_saturation = Data.ColorSaturation;
		this.coverage = 0.01 * Data.Coverage;

		int dev = this.intensity * this.intensity / 4;
		int sat = this.color_saturation * 4096 / 100;

		if (thread_rand == null) {
			thread_rand = new Random (unchecked(System.Threading.Thread.CurrentThread.GetHashCode () ^
			    unchecked((int) DateTime.Now.Ticks)));
		}

		Random localRand = thread_rand;
		ReadOnlySpan<int> localLookup = lookup.AsSpan ();

		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dst.GetPixelData ();
		int width = src.Width;

		foreach (var rect in rois) {
			int right = rect.Right;

			for (int y = rect.Top; y <= rect.Bottom; ++y) {
				var dst_row = dst_data.Slice (y * width, width);
				var src_row = src_data.Slice (y * width, width);

				for (int x = rect.Left; x <= right; ++x) {
					if (localRand.NextDouble () > this.coverage) {
						dst_row[x] = src_row[x];
					} else {
						int r;
						int g;
						int b;
						int i;

						r = localLookup[localRand.Next (TableSize)];
						g = localLookup[localRand.Next (TableSize)];
						b = localLookup[localRand.Next (TableSize)];

						i = (4899 * r + 9618 * g + 1867 * b) >> 14;


						r = i + (((r - i) * sat) >> 12);
						g = i + (((g - i) * sat) >> 12);
						b = i + (((b - i) * sat) >> 12);

						ref readonly ColorBgra src_pixel = ref src_row[x];
						ref ColorBgra dst_pixel = ref dst_row[x];

						dst_pixel.R = Utility.ClampToByte (src_pixel.R + ((r * dev + 32768) >> 16));
						dst_pixel.G = Utility.ClampToByte (src_pixel.G + ((g * dev + 32768) >> 16));
						dst_pixel.B = Utility.ClampToByte (src_pixel.B + ((b * dev + 32768) >> 16));
						dst_pixel.A = src_pixel.A;
					}
				}
			}
		}
	}
	#endregion

	public sealed class NoiseData : EffectData
	{
		[Caption ("Intensity"), MinimumValue (0), MaximumValue (100)]
		public int Intensity = 64;

		[Caption ("Color Saturation"), MinimumValue (0), MaximumValue (400)]
		public int ColorSaturation = 100;

		[Caption ("Coverage"), MinimumValue (0), DigitsValue (2), MaximumValue (100)]
		public double Coverage = 100.0;
	}
}
