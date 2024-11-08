/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class BulgeEffect : BaseEffect
{
	public sealed override bool IsTileable => true;

	public override string Icon => Resources.Icons.EffectsDistortBulge;

	public override string Name => Translations.GetString ("Bulge");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Distort");

	public BulgeData Data => (BulgeData) EffectData!;

	private readonly IChromeService chrome;
	public BulgeEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new BulgeData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	private sealed record BulgeSettings (
		float halfWidth,
		float halfHeight,
		float maxRadius,
		float amount,
		int sourceWidth,
		int sourceHeight);

	private BulgeSettings CreateSettings (ImageSurface source)
	{
		float bulge = Data.Amount;

		float hw = source.Width / 2f;
		float hh = source.Height / 2f;

		return new (
			halfWidth: hw + ((float) Data.Offset.X * hw),
			halfHeight: hh + ((float) Data.Offset.Y * hh),
			maxRadius: Math.Min (hw, hh) * Data.RadiusPercentage / 100f,
			amount: bulge / 100f,
			sourceWidth: source.Width,
			sourceHeight: source.Height);
	}

	// Algorithm Code Ported From PDN
	public override void Render (
		ImageSurface source,
		ImageSurface destination,
		ReadOnlySpan<RectangleI> rois)
	{
		BulgeSettings settings = CreateSettings (source);

		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();

		foreach (RectangleI rect in rois) {

			foreach (var pixel in Utility.GeneratePixelOffsets (rect, new Size (settings.sourceWidth, settings.sourceHeight))) {

				destinationData[pixel.memoryOffset] = GetFinalPixelColor (
					settings,
					source,
					sourceData,
					pixel);
			}
		}
	}

	private static ColorBgra GetFinalPixelColor (
		BulgeSettings settings,
		ImageSurface source,
		ReadOnlySpan<ColorBgra> sourceData,
		PixelOffset pixel)
	{
		float v = pixel.coordinates.Y - settings.halfHeight;
		float u = pixel.coordinates.X - settings.halfWidth;

		float r = (float) Utility.Magnitude (u, v);
		float rscale1 = (1f - (r / settings.maxRadius));

		if (rscale1 <= 0)
			return sourceData[pixel.memoryOffset];

		float rscale2 = 1 - settings.amount * rscale1 * rscale1;

		float xp = u * rscale2;
		float yp = v * rscale2;

		return source.GetBilinearSampleClamped (
			sourceData,
			settings.sourceWidth,
			settings.sourceHeight,
			xp + settings.halfWidth,
			yp + settings.halfHeight);
	}

	public sealed class BulgeData : EffectData
	{
		[Caption ("Amount"), MinimumValue (-200), MaximumValue (100)]
		public int Amount { get; set; } = 45;

		[Caption ("Offset")]
		public PointD Offset { get; set; } = new (0.0, 0.0);

		// Translators: This refers to how big the radius is as a percentage of the image's dimensions
		[Caption ("Radius Percentage")]
		[MinimumValue (10), MaximumValue (100)]
		public int RadiusPercentage { get; set; } = 100;

		[Skip]
		public override bool IsDefault => Amount == 0;
	}
}
