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

public sealed class PolarInversionEffect : BaseEffect
{
	public override string Icon
		=> Resources.Icons.EffectsDistortPolarInversion;

	public sealed override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Polar Inversion");

	public PolarInversionData Data
		=> (PolarInversionData) EffectData!;

	public override string EffectMenuCategory
		=> Translations.GetString ("Distort");

	public override bool IsConfigurable
		=> true;

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	private readonly IChromeService chrome;
	private readonly ILivePreview live_preview;
	private readonly IPaletteService palette;
	private readonly IWorkspaceService workspace;
	public PolarInversionEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		live_preview = services.GetService<ILivePreview> ();
		palette = services.GetService<IPaletteService> ();
		workspace = services.GetService<IWorkspaceService> ();

		EffectData = new PolarInversionData ();
	}

	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		Warp.Settings settings = Warp.CreateSettings (
			Data,
			live_preview.RenderBounds,
			palette);

		Span<ColorBgra> dst_data = dst.GetPixelData ();
		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		foreach (RectangleI rect in rois) {
			foreach (var pixel in Tiling.GeneratePixelOffsets (rect, src.GetSize ())) {
				dst_data[pixel.memoryOffset] = Warp.GetPixelColor (
					settings,
					InverseTransform,
					src,
					src_data[pixel.memoryOffset],
					pixel);
			}
		}
	}

	public Warp.TransformData InverseTransform (
		Warp.Settings settings,
		Warp.TransformData transData)
	{
		double x = transData.X;
		double y = transData.Y;

		// NOTE: when x and y are zero, this will divide by zero and return NaN
		double invertDistance = Mathematics.Lerp (
			1.0,
			settings.defaultRadius2 / Mathematics.MagnitudeSquared (x, y),
			Data.Amount);

		return new (
			X: x * invertDistance,
			Y: y * invertDistance);
	}
}

public sealed class PolarInversionData : EffectData, Warp.IEffectData
{
	[MinimumValue (-4), MaximumValue (4)]
	public double Amount { get; set; } = 0;

	[Caption ("Quality"), MinimumValue (1), MaximumValue (5)]
	public int Quality { get; set; } = 2;

	[Caption ("Center Offset")]
	public CenterOffset<double> CenterOffset { get; set; }

	public EdgeBehavior EdgeBehavior { get; set; } = EdgeBehavior.Reflect;
}
