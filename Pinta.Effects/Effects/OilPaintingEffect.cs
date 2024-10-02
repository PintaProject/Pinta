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
	public override string Icon => Pinta.Resources.Icons.EffectsArtisticOilPainting;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Oil Painting");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Artistic");

	public OilPaintingData Data => (OilPaintingData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;

	public OilPaintingEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new OilPaintingData ();
	}

	public override Task<Gtk.ResponseType> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	#region Algorithm Code Ported From PDN
	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		OilPaintingSettings settings = CreateSettings (src);

		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dest.GetPixelData ();

		foreach (var rect in rois) {
			foreach (var pixel in Utility.GeneratePixelOffsets (rect, settings.canvasSize)) {
				int top = Math.Max (pixel.coordinates.Y - settings.brushSize, 0);
				int bottom = Math.Min (pixel.coordinates.Y + settings.brushSize + 1, settings.canvasSize.Height);
				dst_data[pixel.memoryOffset] = GetFinalColor (settings, src_data, top, bottom, pixel.coordinates.X);
			}
		}
	}

	private sealed record OilPaintingSettings (
		Size canvasSize,
		int brushSize,
		int arrayLens,
		byte maxIntensity);
	private OilPaintingSettings CreateSettings (ImageSurface src)
	{
		int coarseness = Data.Coarseness;
		return new (
			canvasSize: src.GetSize (),
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

		int left = Math.Max (x - settings.brushSize, 0);
		int right = Math.Min (x + settings.brushSize + 1, settings.canvasSize.Width);

		int numInt = 0;

		for (int j = top; j < bottom; ++j) {
			var src_row = src_data.Slice (j * settings.canvasSize.Width, settings.canvasSize.Width);
			for (int i = left; i < right; ++i) {
				ColorBgra src_pixel = src_row[i];
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

			if (intensityCount[i] <= maxInstance)
				continue;

			chosenIntensity = (byte) i;
			maxInstance = intensityCount[i];
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
