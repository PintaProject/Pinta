/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Marco Rolappe <m_rolappe@gmx.net>                       //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class FrostedGlassEffect : BaseEffect
{
	public override string Icon => Resources.Icons.EffectsDistortFrostedGlass;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Frosted Glass");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Distort");

	public FrostedGlassData Data => (FrostedGlassData) EffectData!;

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public FrostedGlassEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new FrostedGlassData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	// Algorithm Code Ported From PDN

	private sealed record FrostedGlassSettings (
		int amount,
		int src_width,
		int src_height,
		RandomSeed seed);

	private FrostedGlassSettings CreateSettings (ImageSurface src)
	{
		var data = Data;
		return new (
			amount: data.Amount,
			src_width: src.Width,
			src_height: src.Height,
			seed: data.Seed
		);
	}

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		FrostedGlassSettings settings = CreateSettings (source);

		ReadOnlySpan<ColorBgra> src_data = source.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = destination.GetPixelData ();

		Random random = new (settings.seed.GetValueForRegion (roi));

		for (int y = roi.Top; y <= roi.Bottom; ++y) {

			var dst_row = dst_data.Slice (y * settings.src_width, settings.src_width);

			int top = Math.Max (y - settings.amount, 0);
			int bottom = Math.Min (y + settings.amount + 1, settings.src_height);

			for (int x = roi.Left; x <= roi.Right; ++x)
				dst_row[x] = GetFinalPixelColor (settings, random, src_data, top, bottom, x);
		}
	}

	private static ColorBgra GetFinalPixelColor (FrostedGlassSettings settings, Random random, ReadOnlySpan<ColorBgra> src_data, int top, int bottom, int x)
	{
		int intensityChoicesIndex = 0;

		Span<int> intensityCount = stackalloc int[256];
		Span<uint> avgRed = stackalloc uint[256];
		Span<uint> avgGreen = stackalloc uint[256];
		Span<uint> avgBlue = stackalloc uint[256];
		Span<uint> avgAlpha = stackalloc uint[256];
		Span<byte> intensityChoices = stackalloc byte[(1 + (settings.amount * 2)) * (1 + (settings.amount * 2))];

		intensityCount.Clear ();
		avgRed.Clear ();
		avgGreen.Clear ();
		avgBlue.Clear ();
		avgAlpha.Clear ();
		intensityChoices.Clear ();

		int left = x - settings.amount;
		int right = x + settings.amount + 1;

		if (left < 0)
			left = 0;

		if (right > settings.src_width)
			right = settings.src_width;

		for (int j = top; j < bottom; ++j) {

			if (j < 0 || j >= settings.src_height)
				continue;

			var src_row = src_data.Slice (j * settings.src_width, settings.src_width);

			for (int i = left; i < right; ++i) {
				ColorBgra src_pixel = src_row[i];
				byte intensity = src_pixel.GetIntensityByte ();

				intensityChoices[intensityChoicesIndex] = intensity;
				++intensityChoicesIndex;

				++intensityCount[intensity];

				avgRed[intensity] += src_pixel.R;
				avgGreen[intensity] += src_pixel.G;
				avgBlue[intensity] += src_pixel.B;
				avgAlpha[intensity] += src_pixel.A;
			}
		}

		int randNum = random.Next (intensityChoicesIndex);
		byte chosenIntensity = intensityChoices[randNum];

		return ColorBgra.FromBgra (
			b: (byte) (avgBlue[chosenIntensity] / intensityCount[chosenIntensity]),
			g: (byte) (avgGreen[chosenIntensity] / intensityCount[chosenIntensity]),
			r: (byte) (avgRed[chosenIntensity] / intensityCount[chosenIntensity]),
			a: (byte) (avgAlpha[chosenIntensity] / intensityCount[chosenIntensity])
		);
	}

	public sealed class FrostedGlassData : EffectData
	{
		[Caption ("Amount")]
		[MinimumValue (1), MaximumValue (10)]
		public int Amount { get; set; } = 1;

		[Caption ("Random Noise Seed")]
		public RandomSeed Seed { get; set; } = new (0);
	}
}
