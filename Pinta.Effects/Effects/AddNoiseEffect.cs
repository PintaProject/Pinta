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
using System.Numerics;
using System.Runtime.CompilerServices;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class AddNoiseEffect : BaseEffect
{
	public sealed override bool IsTileable => true;

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

		while (r - l > 0.0000001) {
			double s = 0;
			scale = (l + r) * 0.5;

			for (int i = 0; i < TableSize; ++i) {
				s += NormalCurve (16.0 * ((double) i - TableSize / 2) / TableSize, scale);

				if (s > 1000000) {
					break;
				}
			}

			if (s > TableSize) {
				r = scale;
			} else if (s < TableSize) {
				l = scale;
			} else {
				break;
			}
		}

		var result = ImmutableArray.CreateBuilder<int> (TableSize);
		result.Count = TableSize;

		double sum = 0;
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

	private static int CreateSeedForRegion (int global_seed, int rect_left, int rect_top)
	{
		// Note that HashCode.Combine() can't be used because it is random per-process and would
		// produce inconsistent results for unit tests.
		// This is the same implementation from HashCode.cs, but without the randomization.
		const uint Prime2 = 2246822519U;
		const uint Prime3 = 3266489917U;
		const uint Prime4 = 668265263U;

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		static uint MixFinal (uint hash)
		{
			hash ^= hash >> 15;
			hash *= Prime2;
			hash ^= hash >> 13;
			hash *= Prime3;
			hash ^= hash >> 16;
			return hash;
		}

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		static uint QueueRound (uint hash, uint queuedValue)
		{
			return BitOperations.RotateLeft (hash + queuedValue * Prime3, 17) * Prime4;
		}

		uint hash = 374761393U;
		hash += 12;

		hash = QueueRound (hash, (uint) global_seed);
		hash = QueueRound (hash, (uint) rect_left);
		hash = QueueRound (hash, (uint) rect_top);

		hash = MixFinal (hash);
		return (int) hash;
	}

	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		int intensity = Data.Intensity;
		int color_saturation = Data.ColorSaturation;
		double coverage = 0.01 * Data.Coverage;
		int global_seed = Data.Seed.Value;

		int dev = intensity * intensity / 4;
		int sat = color_saturation * 4096 / 100;

		ReadOnlySpan<int> localLookup = lookup.AsSpan ();

		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dst.GetPixelData ();
		int width = src.Width;

		foreach (var rect in rois) {
			// Reseed the random number generator for each rectangle being rendered.
			// This should produce consistent results regardless of the number of threads
			// being used to render the effect, but will change if the effect is tiled differently.
			var rand = new Random (CreateSeedForRegion (global_seed, rect.Left, rect.Top));

			int right = rect.Right;
			for (int y = rect.Top; y <= rect.Bottom; ++y) {
				var dst_row = dst_data.Slice (y * width, width);
				var src_row = src_data.Slice (y * width, width);

				for (int x = rect.Left; x <= right; ++x) {
					if (rand.NextDouble () > coverage) {
						dst_row[x] = src_row[x];
					} else {
						int r;
						int g;
						int b;
						int i;

						r = localLookup[rand.Next (TableSize)];
						g = localLookup[rand.Next (TableSize)];
						b = localLookup[rand.Next (TableSize)];

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
		public int Intensity { get; set; } = 64;

		[Caption ("Color Saturation"), MinimumValue (0), MaximumValue (400)]
		public int ColorSaturation { get; set; } = 100;

		[Caption ("Coverage"), MinimumValue (0), DigitsValue (2), MaximumValue (100)]
		public double Coverage { get; set; } = 100.0;

		[Caption ("Seed")]
		public RandomSeed Seed { get; set; } = new (0);
	}
}
