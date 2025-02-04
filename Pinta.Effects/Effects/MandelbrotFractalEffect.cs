/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class MandelbrotFractalEffect : BaseEffect
{
	public override string Icon
		=> Resources.Icons.EffectsRenderMandelbrotFractal;

	public sealed override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Mandelbrot Fractal");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Render");

	public MandelbrotFractalData Data
		=> (MandelbrotFractalData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly IPaletteService palette;
	private readonly IWorkspaceService workspace;
	public MandelbrotFractalEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		palette = services.GetService<IPaletteService> ();
		workspace = services.GetService<IWorkspaceService> ();
		invert_effect = new (services);
		EffectData = new MandelbrotFractalData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	// Algorithm Code Ported From PDN

	private static readonly PointD offset_basis = new (X: -0.7, Y: -0.29);

	private readonly InvertColorsEffect invert_effect;

	private static readonly Mandelbrot fractal = new (
		maxIterations: 1024,
		maxSquared: 100_000);

	private sealed record MandelbrotSettings (
		Size canvasSize,
		double invH,
		double invZoom,
		double invQuality,
		int count,
		double invCount,
		RadiansAngle angleTheta,
		int factor,
		bool invertColors,
		ColorGradient colorGradient);
	private MandelbrotSettings CreateSettings (ImageSurface dst)
	{
		const double ZOOM_FACTOR = 20.0;
		Size canvasSize = new (dst.Width, dst.Height);
		double zoom = 1 + ZOOM_FACTOR * Data.Zoom;
		int count = Data.Quality * Data.Quality + 1;

		ColorGradient baseGradient =
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
			invZoom: 1.0 / zoom,

			invQuality: 1.0 / Data.Quality,

			count: count,
			invCount: 1.0 / count,
			angleTheta: Data.Angle.ToRadians (),

			factor: Data.Factor,

			invertColors: Data.InvertColors,

			colorGradient: Data.ReverseColorScheme ? baseGradient.Reversed () : baseGradient
		);
	}

	protected override void Render (ImageSurface src, ImageSurface dst, RectangleI roi)
	{
		MandelbrotSettings settings = CreateSettings (dst);
		Span<ColorBgra> dst_data = dst.GetPixelData ();
		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, settings.canvasSize))
			dst_data[pixel.memoryOffset] = GetPixelColor (settings, pixel.coordinates);

		if (settings.invertColors)
			invert_effect.Render (dst, dst, [roi]);
	}

	private static ColorBgra GetPixelColor (MandelbrotSettings settings, PointI target)
	{
		int r = 0;
		int g = 0;
		int b = 0;
		int a = 0;

		double baseU = ((2.0 * target.X) - settings.canvasSize.Width) * settings.invH;
		double baseV = ((2.0 * target.Y) - settings.canvasSize.Height) * settings.invH;

		double deltaU = settings.invCount * settings.invH;

		for (double i = 0; i < settings.count; i++) {

			double u = baseU + i * deltaU;
			double v = baseV + (i * settings.invQuality % 1) * settings.invH;

			double radius = Mathematics.Magnitude (u, v);
			double theta = Math.Atan2 (v, u);
			double thetaP = theta + settings.angleTheta.Radians;

			double rotatedU = radius * Math.Cos (thetaP);
			double rotatedV = radius * Math.Sin (thetaP);

			double m = fractal.Compute (
				r: (rotatedU * settings.invZoom) + offset_basis.X,
				i: (rotatedV * settings.invZoom) + offset_basis.Y,
				factor: settings.factor);

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

	public sealed class MandelbrotFractalData : EffectData
	{
		[Caption ("Factor"), MinimumValue (1), MaximumValue (10)]
		public int Factor { get; set; } = 1;

		[Caption ("Quality"), MinimumValue (1), MaximumValue (5)]
		public int Quality { get; set; } = 2;

		[Caption ("Zoom"), MinimumValue (0), MaximumValue (100)]
		public double Zoom { get; set; } = 10;

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
