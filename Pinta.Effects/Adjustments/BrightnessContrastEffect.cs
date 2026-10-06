/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Krzysztof Marecki <marecki.krzysztof@gmail.com>         //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class BrightnessContrastEffect : BaseEffect
{
	const int DEFAULT_BRIGHTNESS = 0;
	const int DEFAULT_CONTRAST = 0;

	// If effect data changed, we recalculate rgb table
	private sealed record CachedOp ( // Why a record? Swaps are atomic
		int brightness,
		int contrast,
		BrightnessContrastPixelOp pixelOp);

	private CachedOp cached_op = new (
		DEFAULT_BRIGHTNESS,
		DEFAULT_CONTRAST,
		new (DEFAULT_BRIGHTNESS, DEFAULT_CONTRAST));

	public sealed override bool IsTileable
		=> true;

	public override string Icon
		=> Resources.Icons.AdjustmentsBrightnessContrast;

	public override string Name
		=> Translations.GetString ("Brightness / Contrast");

	public override bool IsConfigurable
		=> true;

	public override string AdjustmentMenuKey
		=> "B";

	public BrightnessContrastData Data
		=> (BrightnessContrastData) EffectData!;  // NRT - Set in constructor

	public BrightnessContrastEffect (IServiceProvider _)
	{
		EffectData = new BrightnessContrastData ();
	}

	public override Task<bool> LaunchConfiguration (ILivePreviewSession session)
		=> session.LaunchSimpleEffectDialog ();

	private readonly record struct BrightnessContrastSettings (BrightnessContrastPixelOp PreRender, Size CanvasSize);
	private static BrightnessContrastSettings CreateSettings (ImageSurface destination, BrightnessContrastPixelOp pixelOp)
		=> new (
			PreRender: pixelOp,
			CanvasSize: destination.GetSize ());

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		BrightnessContrastData data = Data;

		if (data.Brightness != cached_op.brightness || data.Contrast != cached_op.contrast) {
			cached_op = new (
				data.Brightness,
				data.Contrast,
				new BrightnessContrastPixelOp (data.Brightness, data.Contrast));
		}

		BrightnessContrastSettings settings = CreateSettings (destination, cached_op.pixelOp);

		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();

		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, settings.CanvasSize))
			destinationData[pixel.memoryOffset] = settings.PreRender.Apply (sourceData[pixel.memoryOffset]);
	}

	public sealed class BrightnessContrastData : EffectData
	{
		[Caption ("Brightness")]
		public int Brightness { get; set; } = DEFAULT_BRIGHTNESS;

		[Caption ("Contrast")]
		public int Contrast { get; set; } = DEFAULT_CONTRAST;

		[Skip]
		public override bool IsDefault
			=> Brightness == DEFAULT_BRIGHTNESS && Contrast == DEFAULT_CONTRAST;
	}
}
