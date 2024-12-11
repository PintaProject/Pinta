/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class OilPaintingEffect : BaseEffect
{
	public override string Icon
		=> Resources.Icons.EffectsArtisticOilPainting;

	public sealed override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Oil Painting");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Artistic");

	public OilPaintingData Data
		=> (OilPaintingData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	public OilPaintingEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new OilPaintingData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	// Algorithm Code Ported From PDN
	protected override void Render (
		ImageSurface source,
		ImageSurface destination,
		RectangleI roi)
	{
		OilPaintingSettings settings = CreateSettings (source);
		ReadOnlySpan<ColorBgra> src_data = source.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = destination.GetPixelData ();
		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, settings.canvasSize))
			dst_data[pixel.memoryOffset] = GetFinalColor (
				settings,
				src_data,
				pixel);
	}

	private sealed record OilPaintingSettings (
		Size canvasSize,
		int brushSize,
		int arrayLens,
		byte maxIntensity);
	private OilPaintingSettings CreateSettings (ImageSurface source)
	{
		int coarseness = Data.Coarseness;
		return new (
			canvasSize: source.GetSize (),
			brushSize: Data.BrushSize,
			arrayLens: 1 + coarseness,
			maxIntensity: (byte) coarseness);
	}


	private static ColorBgra GetFinalColor (
		OilPaintingSettings settings,
		ReadOnlySpan<ColorBgra> sourceData,
		PixelOffset pixel)
	{
		Span<int> intensityCount = stackalloc int[settings.arrayLens];
		Span<uint> avgRed = stackalloc uint[settings.arrayLens];
		Span<uint> avgGreen = stackalloc uint[settings.arrayLens];
		Span<uint> avgBlue = stackalloc uint[settings.arrayLens];
		Span<uint> avgAlpha = stackalloc uint[settings.arrayLens];

		int left = Math.Max (pixel.coordinates.X - settings.brushSize, 0);
		int right = Math.Min (pixel.coordinates.X + settings.brushSize + 1, settings.canvasSize.Width);
		int top = Math.Max (pixel.coordinates.Y - settings.brushSize, 0);
		int bottom = Math.Min (pixel.coordinates.Y + settings.brushSize + 1, settings.canvasSize.Height);

		int numInt = 0;

		for (int j = top; j < bottom; ++j) {
			var src_row = sourceData.Slice (j * settings.canvasSize.Width, settings.canvasSize.Width);
			for (int i = left; i < right; ++i) {

				ColorBgra sourcePixel = src_row[i];

				byte intensity = Utility.FastScaleByteByByte (sourcePixel.GetIntensityByte (), settings.maxIntensity);

				++intensityCount[intensity];
				++numInt;

				avgRed[intensity] += sourcePixel.R;
				avgGreen[intensity] += sourcePixel.G;
				avgBlue[intensity] += sourcePixel.B;
				avgAlpha[intensity] += sourcePixel.A;
			}
		}

		IntensityData intensityData = GetIntensityData (settings, intensityCount);

		// TODO: correct handling of alpha values?
		return ColorBgra.FromBgra (
			b: (byte) (avgBlue[intensityData.chosenIntensity] / intensityData.maxInstance),
			g: (byte) (avgGreen[intensityData.chosenIntensity] / intensityData.maxInstance),
			r: (byte) (avgRed[intensityData.chosenIntensity] / intensityData.maxInstance),
			a: (byte) (avgAlpha[intensityData.chosenIntensity] / intensityData.maxInstance));
	}

	private readonly record struct IntensityData (byte chosenIntensity, int maxInstance);
	private static IntensityData GetIntensityData (
		OilPaintingSettings settings,
		ReadOnlySpan<int> intensityCount)
	{
		byte chosenIntensity = 0;
		int maxInstance = 0;
		for (int i = 0; i <= settings.maxIntensity; ++i) {

			if (intensityCount[i] <= maxInstance)
				continue;

			chosenIntensity = (byte) i;
			maxInstance = intensityCount[i];
		}
		return new (chosenIntensity, maxInstance);
	}

	public sealed class OilPaintingData : EffectData
	{
		[Caption ("Brush Size"), MinimumValue (1), MaximumValue (8)]
		public int BrushSize { get; set; } = 3;

		[Caption ("Coarseness"), MinimumValue (3), MaximumValue (255)]
		public int Coarseness { get; set; } = 50;
	}
}
