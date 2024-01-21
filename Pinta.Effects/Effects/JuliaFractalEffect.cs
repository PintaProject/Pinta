/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class JuliaFractalEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsRenderJuliaFractal;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Julia Fractal");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Render");

	public JuliaFractalData Data => (JuliaFractalData) EffectData!;  // NRT - Set in constructor

	private readonly IPaletteService palette;

	private readonly IChromeService chrome;

	public JuliaFractalEffect (IServiceManager services)
	{
		chrome = services.GetService<IChromeService> ();
		palette = services.GetService<IPaletteService> ();
		EffectData = new JuliaFractalData ();
	}

	public override void LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	#region Algorithm Code Ported From PDN

	private static readonly double log2_10000 = Math.Log (10000);

	private static double Julia (PointD jLoc, double r, double i)
	{
		double c = 0;
		while (c < 256 && Utility.MagnitudeSquared (jLoc) < 10000) {
			jLoc = GetNextLocation (jLoc, r, i);
			++c;
		}
		return c - (2 - 2 * log2_10000 / Math.Log (Utility.MagnitudeSquared (jLoc)));
	}

	private static PointD GetNextLocation (PointD jLoc, double r, double i)
	{
		double t = jLoc.X;
		double x = (jLoc.X * jLoc.X) - (jLoc.Y * jLoc.Y) + r;
		double y = (2 * t * jLoc.Y) + i;
		return new (x, y);
	}

	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		JuliaSettings settings = CreateSettings (dst);
		Span<ColorBgra> dst_data = dst.GetPixelData ();
		foreach (RectangleI rect in rois)
			foreach (var pixel in Utility.GeneratePixelOffsets (rect, settings.canvasSize))
				dst_data[pixel.memoryOffset] = GetPixelColor (settings, pixel.coordinates);
	}

	private sealed record JuliaSettings (
		Size canvasSize,
		double invH,
		double invZoom,
		double invQuality,
		double aspect,
		int count,
		double invCount,
		double angleTheta,
		int factor,
		ColorGradient colorGradient);
	private JuliaSettings CreateSettings (ImageSurface dst)
	{
		Size canvasSize = dst.GetSize ();
		var count = Data.Quality * Data.Quality + 1;

		var baseGradient =
			GradientHelper
			.CreateBaseGradientForEffect (
				palette,
				Data.ColorSchemeSource,
				Data.ColorScheme,
				Data.ColorSchemeSeed)
			.Resized (0, 1023);

		return new (
			canvasSize: canvasSize,
			invH: 1.0 / canvasSize.Height,
			invZoom: 1.0 / Data.Zoom,
			invQuality: 1.0 / Data.Quality,
			aspect: canvasSize.Height / (double) canvasSize.Width,
			count: count,
			invCount: 1.0 / count,
			angleTheta: (Data.Angle.Degrees * Math.PI * 2) / 360.0,
			factor: Data.Factor,
			colorGradient: Data.ReverseColorScheme ? baseGradient.Reversed () : baseGradient
		);
	}

	const double Jr = 0.3125;
	const double Ji = 0.03;
	private static ColorBgra GetPixelColor (JuliaSettings settings, PointI target)
	{
		int r = 0;
		int g = 0;
		int b = 0;
		int a = 0;

		for (double i = 0; i < settings.count; i++) {
			double u = (2.0 * target.X - settings.canvasSize.Width + (i * settings.invCount)) * settings.invH;
			double v = (2.0 * target.Y - settings.canvasSize.Height + ((i * settings.invQuality) % 1)) * settings.invH;

			double radius = Math.Sqrt ((u * u) + (v * v));
			double radiusP = radius;
			double theta = Math.Atan2 (v, u);
			double thetaP = theta + settings.angleTheta;

			double uP = radiusP * Math.Cos (thetaP);
			double vP = radiusP * Math.Sin (thetaP);

			PointD jLoc = new (
				X: (uP - vP * settings.aspect) * settings.invZoom,
				Y: (vP + uP * settings.aspect) * settings.invZoom
			);

			double j = Julia (jLoc, Jr, Ji);

			double c = settings.factor * j;

			double clamped_c = Math.Clamp (c, settings.colorGradient.StartPosition, settings.colorGradient.EndPosition);

			ColorBgra colorAddend = settings.colorGradient.GetColor (clamped_c);

			b += colorAddend.B;
			g += colorAddend.G;
			r += colorAddend.R;
			a += colorAddend.A;
		}

		return ColorBgra.FromBgra (
			b: Utility.ClampToByte (b / settings.count),
			g: Utility.ClampToByte (g / settings.count),
			r: Utility.ClampToByte (r / settings.count),
			a: Utility.ClampToByte (a / settings.count)
		);
	}
	#endregion

	public sealed class JuliaFractalData : EffectData
	{
		[Caption ("Factor"), MinimumValue (1), MaximumValue (10)]
		public int Factor { get; set; } = 4;

		[Caption ("Quality"), MinimumValue (1), MaximumValue (5)]
		public int Quality { get; set; } = 2;

		[Caption ("Zoom"), MinimumValue (0), MaximumValue (50)]
		public int Zoom { get; set; } = 1;

		[Caption ("Color Scheme Source")]
		public ColorSchemeSource ColorSchemeSource { get; set; } = ColorSchemeSource.PresetGradient;

		[Caption ("Color Scheme")]
		public PresetGradients ColorScheme { get; set; } = PresetGradients.Bonfire;

		[Caption ("Random Color Scheme Seed")]
		public RandomSeed ColorSchemeSeed { get; set; } = new (0);

		[Caption ("Reverse Color Scheme")]
		public bool ReverseColorScheme { get; set; } = false;

		[Caption ("Angle")]
		public DegreesAngle Angle { get; set; } = new (0);
	}
}
