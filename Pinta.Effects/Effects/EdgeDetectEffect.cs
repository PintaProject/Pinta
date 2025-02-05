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
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class EdgeDetectEffect : BaseEffect
{
	public override string Icon
		=> Pinta.Resources.Icons.EffectsStylizeEdgeDetect;

	public sealed override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Edge Detect");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Stylize");

	public EdgeDetectData Data => (EdgeDetectData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public EdgeDetectEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new EdgeDetectData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		var weights = ComputeWeights (Data.Angle.ToRadians ());
		ColorDifference.RenderColorDifferenceEffect (weights, src, dest, rois);
	}

	private static double[,] ComputeWeights (RadiansAngle angle)
	{
		// angle delta for each weight
		const double ANGLE_DELTA = Math.PI / 4.0;

		double[,] weights = new double[3, 3];

		// for angle = 0 this builds an edge detect filter pointing straight left

		weights[0, 0] = Math.Cos (angle.Radians + ANGLE_DELTA);
		weights[0, 1] = Math.Cos (angle.Radians + 2.0 * ANGLE_DELTA);
		weights[0, 2] = Math.Cos (angle.Radians + 3.0 * ANGLE_DELTA);

		weights[1, 0] = Math.Cos (angle.Radians);
		weights[1, 1] = 0;
		weights[1, 2] = Math.Cos (angle.Radians + 4.0 * ANGLE_DELTA);

		weights[2, 0] = Math.Cos (angle.Radians - ANGLE_DELTA);
		weights[2, 1] = Math.Cos (angle.Radians - 2.0 * ANGLE_DELTA);
		weights[2, 2] = Math.Cos (angle.Radians - 3.0 * ANGLE_DELTA);

		return weights;
	}
}

public sealed class EdgeDetectData : EffectData
{
	[Caption ("Angle")]
	public DegreesAngle Angle { get; set; } = new (45);
}
