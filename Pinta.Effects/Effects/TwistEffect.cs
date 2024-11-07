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
	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		TwistSettings settings = CreateSettings ();

		ReadOnlySpan<ColorBgra> sourceData = src.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = dst.GetPixelData ();

		foreach (var rect in rois) {
			foreach (var pixel in Utility.GeneratePixelOffsets (rect, src.GetSize ())) {
				float j = pixel.coordinates.Y - (settings.HalfHeight + settings.RenderBounds.Top);
				float i = pixel.coordinates.X - (settings.HalfWidth + settings.RenderBounds.Left);
				destinationData[pixel.memoryOffset] =
					(i * i + j * j > (settings.Maxrad + 1) * (settings.Maxrad + 1))
					? sourceData[pixel.memoryOffset]
					: GetFinalPixelColor (src, settings, sourceData, j, i);
			}
		}
	}

	private static ColorBgra GetFinalPixelColor (
		ImageSurface src,
		TwistSettings settings,
		ReadOnlySpan<ColorBgra> sourceData,
		float j,
		float i)
	{
		int b = 0;
		int g = 0;
		int r = 0;
		int a = 0;

		int antialiasSamples = settings.AntialiasPoints.Length;

		for (int p = 0; p < antialiasSamples; ++p) {

			float u = i + (float) settings.AntialiasPoints[p].X;
			float v = j + (float) settings.AntialiasPoints[p].Y;

			double radialDistance = Math.Sqrt (u * u + v * v);
			double originalTheta = Math.Atan2 (v, u);
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
		float preliminaryTwist = Data.Amount;
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

	private static ImmutableArray<PointD> InitializeAntialiasPoints (int aaLevel)
	{
		int aaSamples = aaLevel * aaLevel + 1;
		var aaPoints = ImmutableArray.CreateBuilder<PointD> (aaSamples);
		aaPoints.Count = aaSamples;
		for (int i = 0; i < aaSamples; ++i) {
			float prePtX = i * aaLevel / (float) aaSamples;
			float ptX = prePtX - ((int) prePtX);
			float ptY = i / (float) aaSamples;
			aaPoints[i] = new (ptX, ptY);
		}
		return aaPoints.MoveToImmutable ();
	}

	public sealed class TwistData : EffectData
	{
		[Caption ("Amount"), MinimumValue (-100), MaximumValue (100)]
		public int Amount { get; set; } = 45;

		[Caption ("Antialias"), MinimumValue (0), MaximumValue (5)]
		public int Antialias { get; set; } = 2;
	}
}
