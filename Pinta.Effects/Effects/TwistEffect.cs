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
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class TwistEffect : BaseEffect
{
	public override string Icon => Resources.Icons.EffectsDistortTwist;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Twist");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Distort");

	public TwistData Data => (TwistData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly ILivePreview live_preview;
	public TwistEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		live_preview = services.GetService<ILivePreview> ();
		EffectData = new TwistData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	// Algorithm Code Ported From PDN
	protected override void Render (
		ImageSurface source,
		ImageSurface destination,
		RectangleI roi)
	{
		TwistSettings settings = CreateSettings ();
		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();
		foreach (var pixel in Utility.GeneratePixelOffsets (roi, source.GetSize ()))
			destinationData[pixel.memoryOffset] = GetFinalPixelColor (
				settings,
				source,
				sourceData,
				pixel);
	}

	private static ColorBgra GetFinalPixelColor (
		TwistSettings settings,
		ImageSurface src,
		ReadOnlySpan<ColorBgra> sourceData,
		PixelOffset pixel)
	{
		float j = pixel.coordinates.Y - (settings.HalfHeight + settings.RenderBounds.Top);
		float i = pixel.coordinates.X - (settings.HalfWidth + settings.RenderBounds.Left);

		if (Mathematics.Magnitude (i, j) > (settings.Maxrad + 1) * (settings.Maxrad + 1))
			return sourceData[pixel.memoryOffset];

		int antialiasSamples = settings.AntialiasPoints.Length;

		Span<ColorBgra> samples = stackalloc ColorBgra[antialiasSamples];

		for (int p = 0; p < antialiasSamples; ++p) {

			float u = i + (float) settings.AntialiasPoints[p].X;
			float v = j + (float) settings.AntialiasPoints[p].Y;

			double radialDistance = Mathematics.Magnitude (u, v);
			double originalTheta = Math.Atan2 (v, u);
			double radialFactor = 1 - radialDistance / settings.Maxrad;
			double twistAmount = (radialFactor < 0) ? 0 : (radialFactor * radialFactor * radialFactor);
			double twistedTheta = originalTheta + (twistAmount * settings.Twist / 100);

			PointI samplePosition = new (
				X: (int) (settings.HalfWidth + settings.RenderBounds.Left + (float) (radialDistance * Math.Cos (twistedTheta))),
				Y: (int) (settings.HalfHeight + settings.RenderBounds.Top + (float) (radialDistance * Math.Sin (twistedTheta)))
			);

			samples[p] = src.GetColorBgra (sourceData, src.Width, samplePosition);
		}

		return ColorBgra.Blend (samples);
	}

	private sealed record TwistSettings (
		RectangleI RenderBounds,
		float HalfWidth,
		float HalfHeight,
		float Maxrad,
		float Twist,
		ImmutableArray<PointD> AntialiasPoints);

	private TwistSettings CreateSettings ()
	{
		RectangleI renderBounds = live_preview.RenderBounds;
		float preliminaryTwist = Data.Amount;
		float halfWidth = renderBounds.Width / 2.0f;
		float halfHeight = renderBounds.Height / 2.0f;
		return new (
			RenderBounds: renderBounds,
			HalfWidth: halfWidth,
			HalfHeight: halfHeight,
			Maxrad: Math.Min (halfWidth, halfHeight),
			Twist: preliminaryTwist * preliminaryTwist * Math.Sign (preliminaryTwist),
			AntialiasPoints: InitializeAntialiasPoints (Data.Antialias)
		);
	}

	private static ImmutableArray<PointD> InitializeAntialiasPoints (int antiAliasLevel)
	{
		int antiAliasSample = antiAliasLevel * antiAliasLevel + 1;
		var antiAliasPoints = ImmutableArray.CreateBuilder<PointD> (antiAliasSample);
		antiAliasPoints.Count = antiAliasSample;
		for (int i = 0; i < antiAliasSample; ++i) {
			float pre_pt_x = i * antiAliasLevel / (float) antiAliasSample;
			float pt_x = pre_pt_x - ((int) pre_pt_x);
			float pt_y = i / (float) antiAliasSample;
			antiAliasPoints[i] = new (pt_x, pt_y);
		}
		return antiAliasPoints.MoveToImmutable ();
	}

	public sealed class TwistData : EffectData
	{
		[Caption ("Amount"), MinimumValue (-100), MaximumValue (100)]
		public int Amount { get; set; } = 45;

		[Caption ("Antialias"), MinimumValue (0), MaximumValue (5)]
		public int Antialias { get; set; } = 2;
	}
}
