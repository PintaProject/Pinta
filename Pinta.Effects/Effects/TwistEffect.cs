/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Marco Rolappe <m_rolappe@gmx.net>                       //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class TwistEffect : BaseEffect
{
	public override string Icon
		=> Resources.Icons.EffectsDistortTwist;

	public sealed override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Twist");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Distort");

	public TwistData Data
		=> (TwistData) EffectData!; // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly ILivePreview live_preview;
	private readonly IWorkspaceService workspace;
	public TwistEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		live_preview = services.GetService<ILivePreview> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new TwistData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	// Algorithm Code Ported From PDN
	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		TwistSettings settings = CreateSettings (destination);
		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();
		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, settings.Size))
			destinationData[pixel.memoryOffset] = GetFinalPixelColor (
				settings,
				source,
				sourceData,
				pixel);
	}

	private static ColorBgra GetFinalPixelColor (
		in TwistSettings settings,
		ImageSurface source,
		ReadOnlySpan<ColorBgra> sourceData,
		PixelOffset pixel)
	{
		PointD fromCenter = new (
			X: pixel.coordinates.X - settings.Center.X,
			Y: pixel.coordinates.Y - settings.Center.Y);

		if (fromCenter.MagnitudeSquared () > settings.DistanceThresholdSquared)
			return sourceData[pixel.memoryOffset];

		int antialiasSamples = settings.AntialiasPoints.Length;
		ColorBgra.Blender aggregate = new ();

		for (int i = 0; i < antialiasSamples; ++i) {
			PointD offset = settings.AntialiasPoints[i];
			PointD location = fromCenter + offset;
			aggregate += GetSampleColor (
				settings,
				source,
				sourceData,
				sourceData[pixel.memoryOffset],
				location);
		}

		return aggregate.Blend ();
	}

	private static ColorBgra GetSampleColor (
		in TwistSettings settings,
		ImageSurface source,
		ReadOnlySpan<ColorBgra> sourceData,
		ColorBgra original,
		PointD location)
	{
		double radialDistanceSquared = location.MagnitudeSquared ();

		// If sample falls outside twist circle, it just samples the original
		if (radialDistanceSquared > settings.MaxRadiusSquared)
			return original;

		double radialDistance = Math.Sqrt (radialDistanceSquared); // Guaranteed to be > 0 (see previous check)
		double radialFactor = 1.0d - radialDistance / settings.MaxRadius;
		double twistAmount = radialFactor * radialFactor * radialFactor;
		RadiansAngle localTwist = new (twistAmount * settings.Twist);

		Matrix3x2D rotation = Matrix3x2D.CreateRotation (localTwist);
		PointD rotatedLocation = location.Transformed (rotation);

		PointI samplePosition = (settings.Center + rotatedLocation).ToInt ();

		return source.GetColorBgra (
			sourceData,
			settings.Size.Width,
			samplePosition);
	}

	private readonly record struct TwistSettings (
		PointD Center,
		Size Size,
		double DistanceThresholdSquared,
		double MaxRadius,
		double MaxRadiusSquared,
		double Twist,
		ImmutableArray<PointD> AntialiasPoints);

	private TwistSettings CreateSettings (ImageSurface destination)
	{
		TwistData data = Data;
		RectangleI renderBounds = live_preview.RenderBounds;
		double preliminaryTwist = -data.Amount;
		double halfWidth = renderBounds.Width / 2.0d;
		double halfHeight = renderBounds.Height / 2.0d;
		double radiusBasis = Math.Min (halfWidth, halfHeight);
		double maxRadius = radiusBasis * (data.RadiusPercentage / 100.0d);
		return new (
			Center: new (
				X: halfWidth + renderBounds.Left,
				Y: halfHeight + renderBounds.Top),
			Size: destination.GetSize (),
			DistanceThresholdSquared: (maxRadius + 1) * (maxRadius + 1),
			MaxRadius: maxRadius,
			MaxRadiusSquared: maxRadius * maxRadius,
			Twist: preliminaryTwist * preliminaryTwist * Math.Sign (preliminaryTwist) / 100,
			AntialiasPoints: InitializeAntialiasPoints (data.Antialias));
	}

	private static ImmutableArray<PointD> InitializeAntialiasPoints (int antiAliasLevel)
	{
		int antiAliasSample = antiAliasLevel * antiAliasLevel + 1;
		var antiAliasPoints = ImmutableArray.CreateBuilder<PointD> (antiAliasSample);
		antiAliasPoints.Count = antiAliasSample;
		for (int i = 0; i < antiAliasSample; ++i) {
			double prePtX = i * antiAliasLevel / (double) antiAliasSample;
			antiAliasPoints[i] = new (
				X: prePtX - ((int) prePtX),
				Y: i / (double) antiAliasSample);
		}
		return antiAliasPoints.MoveToImmutable ();
	}

	public sealed class TwistData : EffectData
	{
		[Caption ("Amount")]
		[MinimumValue (-100), MaximumValue (100)]
		public int Amount { get; set; } = 30;

		[Caption ("Radius Percentage")]
		[MinimumValue (0), MaximumValue (100)]
		public int RadiusPercentage { get; set; } = 100;

		[Caption ("Antialias")]
		[MinimumValue (0), MaximumValue (5)]
		public int Antialias { get; set; } = 2;
	}
}
