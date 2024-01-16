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

public sealed class MandelbrotFractalEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsRenderMandelbrotFractal;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Mandelbrot Fractal");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Render");

	public MandelbrotFractalData Data => (MandelbrotFractalData) EffectData!;  // NRT - Set in constructor

	private readonly IPaletteService palette;

	private readonly IChromeManager chrome;

	public MandelbrotFractalEffect (IServiceManager services)
	{
		chrome = services.GetService<IChromeManager> ();
		invert_effect = new (services);
		palette = services.GetService<IPaletteService> ();
		EffectData = new MandelbrotFractalData ();
	}

	public override void LaunchConfiguration ()
	{
		chrome.LaunchSimpleEffectDialog (this);
	}

	#region Algorithm Code Ported From PDN
	private const double Max = 100000;
	private static readonly double inv_log_max = 1.0 / Math.Log (Max);
	private static readonly double zoom_factor = 20.0;
	private const double XOffsetBasis = -0.7;
	private readonly double x_offset = XOffsetBasis;

	private const double YOffsetBasis = -0.29;
	private readonly double y_offset = YOffsetBasis;

	private readonly InvertColorsEffect invert_effect;

	private static double Mandelbrot (double r, double i, int factor)
	{
		int c = 0;
		double x = 0;
		double y = 0;
		while ((c * factor) < 1024 && Utility.MagnitudeSquared (x, y) < Max) {
			double t = x;
			x = (x * x) - (y * y) + r;
			y = (2 * t * y) + i;
			++c;
		}
		return c - Math.Log (Utility.MagnitudeSquared (x, y)) * inv_log_max;
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
		bool invertColors,
		ColorGradient colorGradient);
	private MandelbrotSettings CreateSettings (ImageSurface dst)
	{
		int h = dst.Height;
		double zoom = 1 + zoom_factor * Data.Zoom;
		int count = Data.Quality * Data.Quality + 1;

		var baseGradient =
			GradientHelper
			.CreateBaseGradientForEffect (
				palette,
				Data.ColorSchemeSource,
				Data.ColorScheme,
				Data.ColorSchemeSeed)
			.Resized (0, 1023);

		return new (

			w: dst.Width,
			h: h,

			invH: 1.0 / h,
			invZoom: 1.0 / zoom,

			invQuality: 1.0 / Data.Quality,

			count: count,
			invCount: 1.0 / count,
			angleTheta: (Data.Angle.Degrees * 2 * Math.PI) / 360,

			factor: Data.Factor,

			xOffset: x_offset,
			yOffset: y_offset,

			invertColors: Data.InvertColors,

			colorGradient: Data.ReverseColorScheme ? baseGradient.Reversed () : baseGradient
		);
	}

	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		MandelbrotSettings settings = CreateSettings (dst);

		Span<ColorBgra> dst_data = dst.GetPixelData ();
		foreach (RectangleI rect in rois) {
			for (int y = rect.Top; y <= rect.Bottom; y++) {
				var dst_row = dst_data.Slice (y * settings.w, settings.w);
				for (int x = rect.Left; x <= rect.Right; x++) {
					dst_row[x] = GetPixelColor (settings, new (x, y));
				}
			}
		}

		if (settings.invertColors)
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

			double clamped_c = Math.Clamp (c, settings.colorGradient.StartPosition, settings.colorGradient.EndPosition);

			ColorBgra colorAddend = settings.colorGradient.GetColor (clamped_c);

			r += colorAddend.R;
			g += colorAddend.G;
			b += colorAddend.B;
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

		[Caption ("Color Scheme Source")]
		public ColorSchemeSource ColorSchemeSource { get; set; } = ColorSchemeSource.PresetGradient;

		[Caption ("Color Scheme")]
		public PresetGradients ColorScheme { get; set; } = PresetGradients.Electric;

		[Caption ("Random Color Scheme Seed")]
		public RandomSeed ColorSchemeSeed { get; set; } = new (0);

		[Caption ("Reverse Color Scheme")]
		public bool ReverseColorScheme { get; set; } = false;

		[Caption ("Invert Colors")]
		public bool InvertColors { get; set; } = false;
	}
}
