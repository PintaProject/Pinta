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
		=> (TwistData) EffectData!;  // NRT - Set in constructor

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
		TwistSettings settings = CreateSettings ();
		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();
		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, source.GetSize ()))
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
		PointF fromCenter = new (
			X: pixel.coordinates.X - settings.Center.X,
			Y: pixel.coordinates.Y - settings.Center.Y);

		if (fromCenter.MagnitudeSquaredF () > settings.DistanceThreshold)
			return sourceData[pixel.memoryOffset];

		int antialiasSamples = settings.AntialiasPoints.Length;
		ColorBgra.Blender aggregate = new ();

		for (int i = 0; i < antialiasSamples; ++i) {

			PointF offset = settings.AntialiasPoints[i];
			PointF location = fromCenter + offset;
			double radialDistance = location.Magnitude ();

			// If sample falls outside twist circle, it just samples the original
			if (radialDistance > settings.Maxrad) {
				aggregate += source.GetColorBgra (sourceData, source.Width, pixel.coordinates);
				continue;
			}

			double originalTheta = Math.Atan2 (location.Y, location.X);
			double radialFactor = 1 - radialDistance / settings.Maxrad; // Guaranteed to be > 0 (see previous check)
			double twistAmount = radialFactor * radialFactor * radialFactor;
			double twistedTheta = originalTheta + (twistAmount * settings.Twist);
			PointI samplePosition = new (
				X: (int) (settings.Center.X + (float) (radialDistance * Math.Cos (twistedTheta))),
				Y: (int) (settings.Center.Y + (float) (radialDistance * Math.Sin (twistedTheta))));

			aggregate += source.GetColorBgra (sourceData, source.Width, samplePosition);
		}

		return aggregate.Blend ();
	}

	private readonly record struct TwistSettings (
		PointF Center,
		float DistanceThreshold,
		float Maxrad,
		float Twist,
		ImmutableArray<PointF> AntialiasPoints);

	private TwistSettings CreateSettings ()
	{
		TwistData data = Data;
		RectangleI renderBounds = live_preview.RenderBounds;
		float preliminaryTwist = -data.Amount;
		float halfWidth = renderBounds.Width / 2.0f;
		float halfHeight = renderBounds.Height / 2.0f;
		float maxrad = Math.Min (halfWidth, halfHeight);
		return new (
			Center: new (
				X: halfWidth + renderBounds.Left,
				Y: halfHeight + renderBounds.Top),
			DistanceThreshold: (maxrad + 1) * (maxrad + 1),
			Maxrad: maxrad,
			Twist: preliminaryTwist * preliminaryTwist * Math.Sign (preliminaryTwist) / 100,
			AntialiasPoints: InitializeAntialiasPointsF (data.Antialias)
		);
	}

	private static ImmutableArray<PointF> InitializeAntialiasPointsF (int antiAliasLevel)
	{
		int antiAliasSample = antiAliasLevel * antiAliasLevel + 1;
		var antiAliasPoints = ImmutableArray.CreateBuilder<PointF> (antiAliasSample);
		antiAliasPoints.Count = antiAliasSample;
		for (int i = 0; i < antiAliasSample; ++i) {
			float prePtX = i * antiAliasLevel / (float) antiAliasSample;
			antiAliasPoints[i] = new (
				X: prePtX - ((int) prePtX),
				Y: i / (float) antiAliasSample);
		}
		return antiAliasPoints.MoveToImmutable ();
	}

	public sealed class TwistData : EffectData
	{
		[Caption ("Amount")]
		[MinimumValue (-100), MaximumValue (100)]
		public int Amount { get; set; } = 30;

		[Caption ("Antialias")]
		[MinimumValue (0), MaximumValue (5)]
		public int Antialias { get; set; } = 2;
	}
}
