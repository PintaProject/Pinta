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

public sealed class JuliaFractalEffect : BaseEffect
{
	public override string Icon
		=> Resources.Icons.EffectsRenderJuliaFractal;

	public sealed override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Julia Fractal");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Render");

	public JuliaFractalData Data
		=> (JuliaFractalData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly IPaletteService palette;
	private readonly IWorkspaceService workspace;
	public JuliaFractalEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		palette = services.GetService<IPaletteService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new JuliaFractalData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	// Algorithm Code Ported From PDN

	private static readonly Julia fractal = new (maxSquared: 10_000);

	protected override void Render (ImageSurface src, ImageSurface dst, RectangleI roi)
	{
		JuliaSettings settings = CreateSettings (dst);
		Span<ColorBgra> dst_data = dst.GetPixelData ();
		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, settings.canvasSize))
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
		Matrix3x2D rotation,
		int factor,
		ColorGradient<ColorBgra> colorGradient);
	private JuliaSettings CreateSettings (ImageSurface dst)
	{
		// Reference to effect data, to prevent repeated casting
		// TODO: Remove if and when reading the property doesn't require casting
		var data = Data;

		Size canvasSize = dst.GetSize ();
		var count = data.Quality * data.Quality + 1;
		RadiansAngle angleTheta = data.Angle.ToRadians ();

		var baseGradient =
			GradientHelper
			.CreateBaseGradientForEffect (
				palette,
				data.ColorSchemeSource,
				data.ColorScheme,
				data.ColorSchemeSeed)
			.Resized (0, 1023);

		return new (
			canvasSize: canvasSize,
			invH: 1.0 / canvasSize.Height,
			invZoom: 1.0 / data.Zoom,
			invQuality: 1.0 / data.Quality,
			aspect: canvasSize.Height / (double) canvasSize.Width,
			count: count,
			invCount: 1.0 / count,
			rotation: Matrix3x2D.CreateRotation (data.Angle.ToRadians ()),
			factor: data.Factor,
			colorGradient: data.ReverseColorScheme ? baseGradient.Reversed () : baseGradient
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

		double baseTransfX = 2.0 * target.X - settings.canvasSize.Width;
		double baseTransfY = 2.0 * target.Y - settings.canvasSize.Height;

		for (double i = 0; i < settings.count; i++) {

			PointD transformed = new (
				X: (baseTransfX + (i * settings.invCount)) * settings.invH,
				Y: (baseTransfY + ((i * settings.invQuality) % 1)) * settings.invH);

			PointD p = transformed.Transformed (settings.rotation);

			PointD jLoc = new (
				X: (p.X - p.Y * settings.aspect) * settings.invZoom,
				Y: (p.Y + p.X * settings.aspect) * settings.invZoom);

			double j = fractal.Compute (jLoc, Jr, Ji);

			double c = Math.Clamp (
				settings.factor * j,
				settings.colorGradient.StartPosition,
				settings.colorGradient.EndPosition);

			ColorBgra colorAddend = settings.colorGradient.GetColor (c);

			b += colorAddend.B;
			g += colorAddend.G;
			r += colorAddend.R;
			a += colorAddend.A;
		}

		return ColorBgra.FromBgra (
			b: Utility.ClampToByte (b / settings.count),
			g: Utility.ClampToByte (g / settings.count),
			r: Utility.ClampToByte (r / settings.count),
			a: Utility.ClampToByte (a / settings.count));
	}

	public sealed class JuliaFractalData : EffectData
	{
		[Caption ("Factor"), MinimumValue (1), MaximumValue (10)]
		public int Factor { get; set; } = 4;

		[Caption ("Quality"), MinimumValue (1), MaximumValue (5)]
		public int Quality { get; set; } = 2;

		[Caption ("Zoom"), MinimumValue (0), MaximumValue (50)]
		public double Zoom { get; set; } = 1;

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
