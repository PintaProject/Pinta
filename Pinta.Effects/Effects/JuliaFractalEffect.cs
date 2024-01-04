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

	public JuliaFractalEffect ()
	{
		EffectData = new JuliaFractalData ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

	#region Algorithm Code Ported From PDN
	private static readonly double log2_10000 = Math.Log (10000);

	private static double Julia (double x, double y, double r, double i)
	{
		double c = 0;
		while (c < 256 && ((x * x) + (y * y) < 10000)) {
			double t = x;
			x = (x * x) - (y * y) + r;
			y = (2 * t * y) + i;
			++c;
		}
		return c - (2 - 2 * log2_10000 / Math.Log ((x * x) + (y * y)));
	}

	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		JuliaSettings settings = CreateSettings (dst);
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
	}

	private sealed record JuliaSettings (
		int w,
		int h,
		double invH,
		double invZoom,
		double invQuality,
		double aspect,
		int count,
		double invCount,
		double angleTheta,
		int factor);
	private JuliaSettings CreateSettings (ImageSurface dst)
	{
		var w = dst.Width;
		var h = dst.Height;
		var count = Data.Quality * Data.Quality + 1;
		return new (
			w: w,
			h: h,
			invH: 1.0 / h,
			invZoom: 1.0 / Data.Zoom,
			invQuality: 1.0 / Data.Quality,
			aspect: h / (double) w,
			count: count,
			invCount: 1.0 / count,
			angleTheta: (Data.Angle.Degrees * Math.PI * 2) / 360.0,
			factor: Data.Factor
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
			double u = (2.0 * target.X - settings.w + (i * settings.invCount)) * settings.invH;
			double v = (2.0 * target.Y - settings.h + ((i * settings.invQuality) % 1)) * settings.invH;

			double radius = Math.Sqrt ((u * u) + (v * v));
			double radiusP = radius;
			double theta = Math.Atan2 (v, u);
			double thetaP = theta + settings.angleTheta;

			double uP = radiusP * Math.Cos (thetaP);
			double vP = radiusP * Math.Sin (thetaP);

			double jX = (uP - vP * settings.aspect) * settings.invZoom;
			double jY = (vP + uP * settings.aspect) * settings.invZoom;

			double j = Julia (jX, jY, Jr, Ji);

			double c = settings.factor * j;

			b += Utility.ClampToByte (c - 768);
			g += Utility.ClampToByte (c - 512);
			r += Utility.ClampToByte (c - 256);
			a += Utility.ClampToByte (c - 0);
		}

		return ColorBgra.FromBgra (Utility.ClampToByte (b / settings.count), Utility.ClampToByte (g / settings.count), Utility.ClampToByte (r / settings.count), Utility.ClampToByte (a / settings.count));
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

		[Caption ("Angle")]
		public DegreesAngle Angle { get; set; } = new (0);
	}
}
