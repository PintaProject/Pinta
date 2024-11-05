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
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class ReliefEffect : ColorDifferenceEffect
{
	private readonly IChromeService chrome;

	public ReliefEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();

		EffectData = new ReliefData ();
	}

	public ReliefData Data => (ReliefData) EffectData!;

	public override bool IsConfigurable => true;

	public sealed override bool IsTileable => true;

	public override string EffectMenuCategory => Translations.GetString ("Stylize");

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	public override string Icon => Pinta.Resources.Icons.EffectsStylizeRelief;

	public override string Name => Translations.GetString ("Relief");

	// Algorithm Code Ported From PDN
	public override void Render (Cairo.ImageSurface src, Cairo.ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		var weights = ComputeWeights (Data.Angle.ToRadians ());
		RenderColorDifferenceEffect (weights, src, dst, rois);
	}

	private static double[,] ComputeWeights (RadiansAngle angle)
	{
		// angle delta for each weight
		const double ANGLE_DELTA = Math.PI / 4.0;

		double[,] weights = new double[3, 3];

		// for angle = 0 this builds an relief filter pointing straight left

		weights[0, 0] = Math.Cos (angle.Radians + ANGLE_DELTA);
		weights[0, 1] = Math.Cos (angle.Radians + 2.0 * ANGLE_DELTA);
		weights[0, 2] = Math.Cos (angle.Radians + 3.0 * ANGLE_DELTA);

		weights[1, 0] = Math.Cos (angle.Radians);
		weights[1, 1] = 1;
		weights[1, 2] = Math.Cos (angle.Radians + 4.0 * ANGLE_DELTA);

		weights[2, 0] = Math.Cos (angle.Radians - ANGLE_DELTA);
		weights[2, 1] = Math.Cos (angle.Radians - 2.0 * ANGLE_DELTA);
		weights[2, 2] = Math.Cos (angle.Radians - 3.0 * ANGLE_DELTA);

		return weights;
	}
}


public sealed class ReliefData : EffectData
{
	[Caption ("Angle")]
	public DegreesAngle Angle { get; set; } = new (45);
}
