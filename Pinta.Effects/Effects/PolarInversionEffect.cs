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

public sealed class PolarInversionEffect : BaseEffect, IWarpEffect<PolarInversionData>
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
		=> Chrome.LaunchSimpleEffectDialog (this);

	public IPaletteService Palette { get; }
	public IChromeService Chrome { get; }
	public PolarInversionEffect (IServiceProvider services)
	{
		Palette = services.GetService<IPaletteService> ();
		Chrome = services.GetService<IChromeService> ();
		EffectData = new PolarInversionData ();
	}

	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		this.RenderWarpEffect (src, dst, rois);
	}

	public Warp.TransformData InverseTransform (
		Warp.TransformData transData,
		WarpSettings settings)
	{
		double x = transData.X;
		double y = transData.Y;

		// NOTE: when x and y are zero, this will divide by zero and return NaN
		double invertDistance = Utility.Lerp (
			1.0,
			settings.defaultRadius2 / Utility.MagnitudeSquared (x, y),
			Data.Amount);

		return new (
			X: x * invertDistance,
			Y: y * invertDistance);
	}
}

public sealed class PolarInversionData : EffectData, IWarpData
{
	[MinimumValue (-4), MaximumValue (4)]
	public double Amount { get; set; } = 0;

	[Caption ("Quality"), MinimumValue (1), MaximumValue (5)]
	public int Quality { get; set; } = 2;

	[Caption ("Center Offset")]
	public PointD CenterOffset { get; set; }

	public WarpEdgeBehavior EdgeBehavior { get; set; } = WarpEdgeBehavior.Reflect;
}
