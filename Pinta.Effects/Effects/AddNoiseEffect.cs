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
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class AddNoiseEffect : BaseEffect
{
	public sealed override bool IsTileable => true;

	public override string Icon => Resources.Icons.EffectsNoiseAddNoise;

	public override string Name => Translations.GetString ("Add Noise");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Noise");

	public NoiseData Data => (NoiseData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	public AddNoiseEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new NoiseData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	// Algorithm Code Ported From PDN
	private const int TABLE_SIZE = 16384;
	private static readonly ImmutableArray<int> lookup = CreateLookup ();

	private static double NormalCurve (double x, double scale)
		=> scale * Math.Exp (-x * x / 2);

	private static ImmutableArray<int> CreateLookup ()
	{
		double l = 5;
		double r = 10;
		double scale = 50;

		while (r - l > 0.0000001) {

			double s = 0;
			scale = (l + r) * 0.5;

			for (int i = 0; i < TABLE_SIZE; ++i) {

				s += NormalCurve (16.0 * ((double) i - TABLE_SIZE / 2) / TABLE_SIZE, scale);

				if (s > 1000000)
					break;
			}

			if (s > TABLE_SIZE)
				r = scale;
			else if (s < TABLE_SIZE)
				l = scale;
			else
				break;
		}

		var result = ImmutableArray.CreateBuilder<int> (TABLE_SIZE);
		result.Count = TABLE_SIZE;

		double sum = 0;
		int roundedSum = 0, lastRoundedSum;

		for (int i = 0; i < TABLE_SIZE; ++i) {

			sum += NormalCurve (16.0 * ((double) i - TABLE_SIZE / 2) / TABLE_SIZE, scale);
			lastRoundedSum = roundedSum;
			roundedSum = (int) sum;

			for (int j = lastRoundedSum; j < roundedSum; ++j)
				result[j] = (i - TABLE_SIZE / 2) * 65536 / TABLE_SIZE;
		}

		return result.MoveToImmutable ();
	}

	private sealed record AddNoiseSettings (
		Size size,
		RandomSeed seed,
		double coverage,
		int dev,
		int sat);

	private AddNoiseSettings CreateSettings (ImageSurface src)
	{
		var data = Data;
		int intensity = data.Intensity;
		int color_saturation = data.ColorSaturation;
		return new (
			size: src.GetSize (),
			seed: data.Seed,
			coverage: 0.01 * data.Coverage,
			dev: intensity * intensity / 4,
			sat: color_saturation * 4096 / 100
		);
	}

	protected override void Render (
		ImageSurface source,
		ImageSurface destination,
		RectangleI rect)
	{
		AddNoiseSettings settings = CreateSettings (source);

		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();

		// Reseed the random number generator for each rectangle being rendered.
		// This should produce consistent results regardless of the number of threads
		// being used to render the effect, but will change if the effect is tiled differently.
		Random rand = new (settings.seed.GetValueForRegion (rect));

		foreach (var pixel in Tiling.GeneratePixelOffsets (rect, settings.size))
			destinationData[pixel.memoryOffset] = GetFinalPixelColor (
				settings,
				rand,
				sourceData[pixel.memoryOffset]);
	}

	private static ColorBgra GetFinalPixelColor (
		AddNoiseSettings settings,
		Random rand,
		ColorBgra original)
	{
		if (rand.NextDouble () > settings.coverage)
			return original;

		int _r = lookup[rand.Next (TABLE_SIZE)];
		int _g = lookup[rand.Next (TABLE_SIZE)];
		int _b = lookup[rand.Next (TABLE_SIZE)];

		int i = (4899 * _r + 9618 * _g + 1867 * _b) >> 14;

		int r = i + (((_r - i) * settings.sat) >> 12);
		int g = i + (((_g - i) * settings.sat) >> 12);
		int b = i + (((_b - i) * settings.sat) >> 12);

		return ColorBgra.FromBgra (
			b: Utility.ClampToByte (original.B + ((b * settings.dev + 32768) >> 16)),
			g: Utility.ClampToByte (original.G + ((g * settings.dev + 32768) >> 16)),
			r: Utility.ClampToByte (original.R + ((r * settings.dev + 32768) >> 16)),
			a: original.A
		);
	}

	public sealed class NoiseData : EffectData
	{
		[Caption ("Intensity"), MinimumValue (0), MaximumValue (100)]
		public int Intensity { get; set; } = 64;

		[Caption ("Color Saturation"), MinimumValue (0), MaximumValue (400)]
		public int ColorSaturation { get; set; } = 100;

		[Caption ("Coverage"), MinimumValue (0), DigitsValue (2), MaximumValue (100)]
		public double Coverage { get; set; } = 100.0;

		[Caption ("Random Noise")]
		public RandomSeed Seed { get; set; } = new (0);
	}
}
