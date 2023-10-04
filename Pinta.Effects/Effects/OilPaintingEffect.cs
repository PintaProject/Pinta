/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class OilPaintingEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsArtisticOilPainting;

	public override string Name => Translations.GetString ("Oil Painting");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Artistic");

	public OilPaintingData Data => (OilPaintingData) EffectData!;  // NRT - Set in constructor

	public OilPaintingEffect ()
	{
		EffectData = new OilPaintingData ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

	#region Algorithm Code Ported From PDN
	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		OilPaintingSettings settings = CreateSettings (src);

		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dest.GetPixelData ();

		foreach (var rect in rois) {

			int rectTop = rect.Top;
			int rectBottom = rect.Bottom;
			int rectLeft = rect.Left;
			int rectRight = rect.Right;

			for (int y = rectTop; y <= rectBottom; ++y) {

				var dst_row = dst_data.Slice (y * settings.width, settings.width);

				int top = y - Data.BrushSize;
				int bottom = y + Data.BrushSize + 1;

				if (top < 0)
					top = 0;

				if (bottom > settings.height)
					bottom = settings.height;

				for (int x = rectLeft; x <= rectRight; ++x)
					dst_row[x] = GetFinalColor (settings, src_data, top, bottom, x);
			}
		}
	}

	private sealed record OilPaintingSettings (
		int width,
		int height,
		int brushSize,
		int arrayLens,
		byte maxIntensity);
	private OilPaintingSettings CreateSettings (ImageSurface src)
	{
		int coarseness = Data.Coarseness;
		return new (
			width: src.Width,
			height: src.Height,
			brushSize: Data.BrushSize,
			arrayLens: 1 + coarseness,
			maxIntensity: (byte) coarseness
		);
	}


	private static ColorBgra GetFinalColor (OilPaintingSettings settings, ReadOnlySpan<ColorBgra> src_data, int top, int bottom, int x)
	{
		Span<int> intensityCount = stackalloc int[settings.arrayLens];
		Span<uint> avgRed = stackalloc uint[settings.arrayLens];
		Span<uint> avgGreen = stackalloc uint[settings.arrayLens];
		Span<uint> avgBlue = stackalloc uint[settings.arrayLens];
		Span<uint> avgAlpha = stackalloc uint[settings.arrayLens];

		int left = x - settings.brushSize;
		int right = x + settings.brushSize + 1;

		if (left < 0)
			left = 0;

		if (right > settings.width)
			right = settings.width;

		int numInt = 0;

		for (int j = top; j < bottom; ++j) {
			var src_row = src_data.Slice (j * settings.width, settings.width);
			for (int i = left; i < right; ++i) {
				ref readonly ColorBgra src_pixel = ref src_row[i];
				byte intensity = Utility.FastScaleByteByByte (src_pixel.GetIntensityByte (), settings.maxIntensity);

				++intensityCount[intensity];
				++numInt;

				avgRed[intensity] += src_pixel.R;
				avgGreen[intensity] += src_pixel.G;
				avgBlue[intensity] += src_pixel.B;
				avgAlpha[intensity] += src_pixel.A;
			}
		}

		byte chosenIntensity = 0;
		int maxInstance = 0;

		for (int i = 0; i <= settings.maxIntensity; ++i) {
			if (intensityCount[i] > maxInstance) {
				chosenIntensity = (byte) i;
				maxInstance = intensityCount[i];
			}
		}

		// TODO: correct handling of alpha values?

		return ColorBgra.FromBgra (
			b: (byte) (avgBlue[chosenIntensity] / maxInstance),
			g: (byte) (avgGreen[chosenIntensity] / maxInstance),
			r: (byte) (avgRed[chosenIntensity] / maxInstance),
			a: (byte) (avgAlpha[chosenIntensity] / maxInstance)
		);
	}
	#endregion

	public sealed class OilPaintingData : EffectData
	{
		[Caption ("Brush Size"), MinimumValue (1), MaximumValue (8)]
		public int BrushSize { get; set; } = 3;

		[Caption ("Coarseness"), MinimumValue (3), MaximumValue (255)]
		public int Coarseness { get; set; } = 50;
	}
}
