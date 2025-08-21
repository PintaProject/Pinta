/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class MotionBlurEffect : BaseEffect
{
	public override string Icon
		=> Resources.Icons.EffectsBlursMotionBlur;

	public sealed override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Motion Blur");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Blurs");

	public MotionBlurData Data
		=> (MotionBlurData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public MotionBlurEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new MotionBlurData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	// Algorithm Code Ported From PDN

	private sealed record MotionBlurSettings (
		Size canvasSize,
		ImmutableArray<PointD> points);

	private MotionBlurSettings CreateSettings (ImageSurface src)
	{
		RadiansAngle theta = Data.Angle.ToRadians () + new RadiansAngle (Math.PI);

		double alpha = Data.Distance;

		PointD start = PointD.Zero;
		PointD end = new (
			X: (float) alpha * Math.Cos (theta.Radians),
			Y: (float) (-alpha * Math.Sin (theta.Radians)));

		if (Data.Centered) {
			start = new (-end.X / 2.0f, -end.Y / 2.0f);
			end = new (end.X / 2.0f, end.Y / 2.0f);
		}

		int numberOfPoints = (1 + Data.Distance) * 3 / 2;
		var points = ImmutableArray.CreateBuilder<PointD> (numberOfPoints);
		points.Count = numberOfPoints;
		if (numberOfPoints == 1) {
			points[0] = new PointD (0, 0);
		} else {
			for (int i = 0; i < numberOfPoints; ++i) {
				float frac = i / (float) (numberOfPoints - 1);
				points[i] = Utility.Lerp (start, end, frac);
			}
		}

		return new (
			canvasSize: src.GetSize (),
			points: points.MoveToImmutable ());
	}

	protected override void Render (
		ImageSurface source,
		ImageSurface destination,
		RectangleI roi)
	{
		MotionBlurSettings settings = CreateSettings (source);

		Span<ColorBgra> samples = stackalloc ColorBgra[settings.points.Length];

		ReadOnlySpan<ColorBgra> src_data = source.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = destination.GetPixelData ();

		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, settings.canvasSize))
			dst_data[pixel.memoryOffset] = GetFinalPixelColor (
				settings,
				source,
				src_data,
				samples,
				pixel);
	}

	private static ColorBgra GetFinalPixelColor (
		MotionBlurSettings settings,
		ImageSurface source,
		ReadOnlySpan<ColorBgra> src_data,
		Span<ColorBgra> samples,
		in PixelOffset pixel)
	{
		int sampleCount = 0;

		for (int j = 0; j < settings.points.Length; ++j) {

			PointD pt = new (settings.points[j].X + pixel.coordinates.X, settings.points[j].Y + pixel.coordinates.Y);

			if (pt.X < 0 || pt.Y < 0 || pt.X > (settings.canvasSize.Width - 1) || pt.Y > (settings.canvasSize.Height - 1))
				continue;

			samples[sampleCount] = source.GetBilinearSample (
				src_data,
				settings.canvasSize.Width,
				settings.canvasSize.Height,
				(float) pt.X,
				(float) pt.Y);

			++sampleCount;
		}

		if (sampleCount == 0)
			return ColorBgra.Transparent; // TODO: Check if this scenario is possible, otherwise remove condition
		else
			return ColorBgra.Blend (samples[..sampleCount]);
	}

	public sealed class MotionBlurData : EffectData
	{
		[Skip]
		public override bool IsDefault => Distance == 0;

		[Caption ("Angle")]
		public DegreesAngle Angle { get; set; } = new (25);

		[Caption ("Distance")]
		[MinimumValue (1), MaximumValue (200)]
		public int Distance { get; set; } = 10;

		[Caption ("Centered")]
		public bool Centered { get; set; } = true;
	}
}
