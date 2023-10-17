/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class MandelbrotFractalEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsRenderMandelbrotFractal;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Mandelbrot Fractal");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Render");

	public MandelbrotFractalData Data => (MandelbrotFractalData) EffectData!;  // NRT - Set in constructor

	public MandelbrotFractalEffect ()
	{
		EffectData = new MandelbrotFractalData ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

	#region Algorithm Code Ported From PDN
	private const double Max = 100000;
	private static readonly double inv_log_max = 1.0 / Math.Log (Max);
	private static readonly double zoom_factor = 20.0;
	private const double XOffsetBasis = -0.7;
	private readonly double x_offset = XOffsetBasis;

	private const double YOffsetBasis = -0.29;
	private readonly double y_offset = YOffsetBasis;

	private readonly InvertColorsEffect invert_effect = new ();

	private static double Mandelbrot (double r, double i, int factor)
	{
		int c = 0;
		double x = 0;
		double y = 0;
		while ((c * factor) < 1024 && ((x * x) + (y * y)) < Max) {
			double t = x;
			x = (x * x) - (y * y) + r;
			y = (2 * t * y) + i;
			++c;
		}
		return c - Math.Log ((y * y) + (x * x)) * inv_log_max;
	}

	private sealed record MandelbrotSettings (
		int w,
		int h,
		double invH,
		double invZoom,
		double invQuality,
		int count,
		double invCount,
		double angleTheta,
		int factor,
		double xOffset,
		double yOffset,
		ImmutableColorGradient gradient);
	private MandelbrotSettings CreateSettings (ImageSurface dst)
	{
		int h = dst.Height;
		double zoom = 1 + zoom_factor * Data.Zoom;
		int count = Data.Quality * Data.Quality + 1;

		return new (

			w: dst.Width,
			h: h,

			invH: 1.0 / h,
			invZoom: 1.0 / zoom,

			invQuality: 1.0 / (double) Data.Quality,

			count: count,
			invCount: 1.0 / (double) count,
			angleTheta: (Data.Angle.Degrees * 2 * Math.PI) / 360,

			factor: Data.Factor,

			xOffset: x_offset,
			yOffset: y_offset,

			gradient: CreateColorGradient (Data.Colors)
		);
	}

	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		MandelbrotSettings settings = CreateSettings (dst);

		Span<ColorBgra> dst_data = dst.GetPixelData ();
		foreach (Core.RectangleI rect in rois) {
			for (int y = rect.Top; y <= rect.Bottom; y++) {
				var dst_row = dst_data.Slice (y * settings.w, settings.w);
				for (int x = rect.Left; x <= rect.Right; x++) {
					PointI target = new (x, y);
					dst_row[x] = GetPixelColor (settings, target);
				}
			}
		}

		if (Data.InvertColors)
			invert_effect.Render (dst, dst, rois);
	}

	private static ColorBgra GetPixelColor (MandelbrotSettings settings, PointI target)
	{
		int r = 0;
		int g = 0;
		int b = 0;
		int a = 0;
		for (double i = 0; i < settings.count; i++) {
			double u = (2.0 * target.X - settings.w + (i * settings.invCount)) * settings.invH;
			double v = (2.0 * target.Y - settings.h + ((i * settings.invQuality) % 1)) * settings.invH;

			double radius = Math.Sqrt ((u * u) + (v * v));
			double radiusP = radius;
			double theta = Math.Atan2 (v, u);
			double thetaP = theta + settings.angleTheta;

			double uP = radiusP * Math.Cos (thetaP);
			double vP = radiusP * Math.Sin (thetaP);

			double m = Mandelbrot ((uP * settings.invZoom) + settings.xOffset, (vP * settings.invZoom) + settings.yOffset, settings.factor);

			double c = 64 + settings.factor * m;

			double clamped_c = Math.Clamp (c, settings.gradient.MinPosition, settings.gradient.MaxPosition);

			ColorBgra colorAddend = settings.gradient.GetColor (clamped_c);

			r += colorAddend.R;
			g += colorAddend.G;
			b += colorAddend.B;
			a += colorAddend.A;
		}
		return ColorBgra.FromBgra (Utility.ClampToByte (b / settings.count), Utility.ClampToByte (g / settings.count), Utility.ClampToByte (r / settings.count), Utility.ClampToByte (a / settings.count));
	}
	#endregion

	public enum PredefinedColorSchemes
	{
		[Caption ("Cotton Candy")]
		CottonCandy,

		[Caption ("Electric")]
		Electric,

		[Caption ("La Bella Italia")]
		LaBellaItalia,

		[Caption ("Lime Lemon")]
		LimeLemon,

		[Caption ("Piña Colada")]
		PinaColada,

		[Caption ("Sakura Sigh")]
		SakuraSigh,
	}

	private static ImmutableColorGradient CreateColorGradient (PredefinedColorSchemes scheme)
	{
		const double Outer = 0;
		const double Core = 1023;
		return scheme switch {
			PredefinedColorSchemes.CottonCandy => new ImmutableColorGradient (
				ColorBgra.White,
				ColorBgra.FromBgr (242, 235, 214),
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[256] = ColorBgra.FromBgr (180, 105, 255),
					[512] = ColorBgra.FromBgr (219, 112, 219),
					[768] = ColorBgra.FromBgr (230, 216, 173),
				}
			),
			PredefinedColorSchemes.Electric => new ImmutableColorGradient (
				ColorBgra.Transparent,
				ColorBgra.White,
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[256] = ColorBgra.Black,
					[512] = ColorBgra.Blue,
					[768] = ColorBgra.Cyan,
				}
			),
			PredefinedColorSchemes.LaBellaItalia => new ImmutableColorGradient (
				ColorBgra.FromBgr (70, 146, 0),
				ColorBgra.FromBgr (55, 43, 206),
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[256] = ColorBgra.White,
				}
			),
			PredefinedColorSchemes.LimeLemon => new ImmutableColorGradient (
				ColorBgra.Transparent,
				ColorBgra.White,
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[256] = ColorBgra.FromBgr (0, 128, 0),
					[512] = ColorBgra.FromBgr (0, 255, 0),
					[768] = ColorBgra.FromBgr (0, 255, 255),
				}
			),
			PredefinedColorSchemes.PinaColada => new ImmutableColorGradient (
				ColorBgra.FromBgr (0, 128, 128),
				ColorBgra.FromBgr (196, 245, 253),
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[256] = ColorBgra.Yellow,
				}
			),
			PredefinedColorSchemes.SakuraSigh => new ImmutableColorGradient (
				ColorBgra.Transparent,
				ColorBgra.FromBgr (240, 255, 255),
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[256] = ColorBgra.FromBgr (235, 206, 135),
					[768] = ColorBgra.FromBgr (193, 182, 255),
				}

			),
			_ => CreateColorGradient (PredefinedColorSchemes.Electric)
		};
	}

	public sealed class MandelbrotFractalData : EffectData
	{
		[Caption ("Factor"), MinimumValue (1), MaximumValue (10)]
		public int Factor { get; set; } = 1;

		[Caption ("Quality"), MinimumValue (1), MaximumValue (5)]
		public int Quality { get; set; } = 2;

		//TODO double
		[Caption ("Zoom"), MinimumValue (0), MaximumValue (100)]
		public int Zoom { get; set; } = 10;

		[Caption ("Angle")]
		public DegreesAngle Angle { get; set; } = new (0);

		[Caption ("Colors")]
		public PredefinedColorSchemes Colors { get; set; } = PredefinedColorSchemes.Electric;

		[Caption ("Invert Colors")]
		public bool InvertColors { get; set; } = false;
	}
}
