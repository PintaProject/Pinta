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
	protected override void Render (
		ImageSurface source,
		ImageSurface destination,
		RectangleI roi)
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
		TwistSettings settings,
		ImageSurface src,
		ReadOnlySpan<ColorBgra> sourceData,
		PixelOffset pixel)
	{
		PointF t1 = new (
			X: pixel.coordinates.X - (settings.HalfWidth + settings.RenderBounds.Left),
			Y: pixel.coordinates.Y - (settings.HalfHeight + settings.RenderBounds.Top));

		if (Utility.MagnitudeSquared (t1) > (settings.Maxrad + 1) * (settings.Maxrad + 1))
			return sourceData[pixel.memoryOffset];

		int b = 0;
		int g = 0;
		int r = 0;
		int a = 0;

		int antialiasSamples = settings.AntialiasPoints.Length;

		for (int p = 0; p < antialiasSamples; ++p) {

			PointF t2 = t1 + settings.AntialiasPoints[p].ToFloat ();

			double radialDistance = Utility.Magnitude (t2);
			double originalTheta = Math.Atan2 (t2.Y, t2.X);
			double radialFactor = 1 - radialDistance / settings.Maxrad;
			double twistAmount = (radialFactor < 0) ? 0 : (radialFactor * radialFactor * radialFactor);
			double twistedTheta = originalTheta + (twistAmount * settings.Twist / 100);

			PointI samplePosition = new (
				X: (int) (settings.HalfWidth + settings.RenderBounds.Left + (float) (radialDistance * Math.Cos (twistedTheta))),
				Y: (int) (settings.HalfHeight + settings.RenderBounds.Top + (float) (radialDistance * Math.Sin (twistedTheta)))
			);

			ColorBgra sample = src.GetColorBgra (sourceData, src.Width, samplePosition);

			b += sample.B;
			g += sample.G;
			r += sample.R;
			a += sample.A;
		}
		return ColorBgra.FromBgra (
			(byte) (b / antialiasSamples),
			(byte) (g / antialiasSamples),
			(byte) (r / antialiasSamples),
			(byte) (a / antialiasSamples));
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
		float preliminaryTwist = -Data.Amount;
		float hw = renderBounds.Width / 2.0f;
		float hh = renderBounds.Height / 2.0f;
		return new (
			RenderBounds: renderBounds,
			HalfWidth: hw,
			HalfHeight: hh,
			Maxrad: Math.Min (hw, hh),
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
		public int Amount { get; set; } = 30;

		[Caption ("Antialias"), MinimumValue (0), MaximumValue (5)]
		public int Antialias { get; set; } = 2;
	}
}
