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

	private static readonly UnaryPixelOp invert_color = new UnaryPixelOps.Invert ();
	private static readonly UnaryPixelOp pass_through = new UnaryPixelOps.Identity ();

	private readonly IChromeService chrome;
	private readonly IPaletteService palette;
	private readonly IWorkspaceService workspace;
	public MandelbrotFractalEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		palette = services.GetService<IPaletteService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new MandelbrotFractalData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	// Algorithm Code Ported From PDN

	private static readonly PointD offset_basis = new (X: -0.7, Y: -0.29);

	private static readonly Mandelbrot fractal = new (
		maxIterations: 1024,
		maxSquared: 100_000);

	private readonly record struct MandelbrotSettings (
		ColorGradient<ColorBgra> Gradient,
		Matrix3x2D Rotation,
		Size CanvasSize,
		UnaryPixelOp PostProcessing,
		double InverseHeight,
		double InverseZoom,
		double InverseQuality,
		double InverseCount,
		int Count,
		int Factor);

	private MandelbrotSettings CreateSettings (ImageSurface destination)
	{
		const double ZOOM_FACTOR = 20.0;

		// Reference to effect data, to prevent repeated casting
		// TODO: Remove if and when reading the property doesn't require casting
		var data = Data;

		Size canvasSize = new (destination.Width, destination.Height);
		double zoom = 1 + ZOOM_FACTOR * data.Zoom;
		int count = data.Quality * data.Quality + 1;

		var baseGradient =
			GradientHelper
			.CreateBaseGradientForEffect (
				palette,
				data.ColorSchemeSource,
				data.ColorScheme,
				data.ColorSchemeSeed)
			.Resized (NumberRange.Create<double> (0, 1023));

		return new (

			Gradient: data.ReverseColorScheme ? baseGradient.Reversed () : baseGradient,
			Rotation: Matrix3x2D.CreateRotation (data.Angle.ToRadians ()),
			CanvasSize: canvasSize,
			PostProcessing: data.InvertColors ? invert_color : pass_through,

			InverseHeight: 1.0 / canvasSize.Height,
			InverseZoom: 1.0 / zoom,
			InverseQuality: 1.0 / data.Quality,
			InverseCount: 1.0 / count,

			Count: count,
			Factor: data.Factor);
	}

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		MandelbrotSettings settings = CreateSettings (destination);
		Span<ColorBgra> destinationData = destination.GetPixelData ();
		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, settings.CanvasSize))
			destinationData[pixel.memoryOffset] = GetPixelColor (settings, pixel.coordinates);
	}

	private static ColorBgra GetPixelColor (in MandelbrotSettings settings, PointI target)
	{
		ColorBgra.Blender aggregate = new ();

		double baseU = ((2.0 * target.X) - settings.CanvasSize.Width) * settings.InverseHeight;
		double baseV = ((2.0 * target.Y) - settings.CanvasSize.Height) * settings.InverseHeight;

		double deltaU = settings.InverseCount * settings.InverseHeight;

		for (int i = 0; i < settings.Count; i++) {

			PointD rel = new (
				X: baseU + i * deltaU,
				Y: baseV + (i * settings.InverseQuality % 1) * settings.InverseHeight);

			PointD rotatedRel = rel.Transformed (settings.Rotation);

			double m = fractal.Compute (
				r: (rotatedRel.X * settings.InverseZoom) + offset_basis.X,
				i: (rotatedRel.Y * settings.InverseZoom) + offset_basis.Y,
				factor: settings.Factor);

			double c = Math.Clamp (
				64 + settings.Factor * m,
				settings.Gradient.Range.Lower,
				settings.Gradient.Range.Upper);

			aggregate += settings.Gradient.GetColor (c);
		}

		ColorBgra blended = aggregate.Blend ();

		return settings.PostProcessing.Apply (blended);
	}

	public sealed class MandelbrotFractalData : EffectData
	{
		[Caption ("Factor")]
		[MinimumValue (1), MaximumValue (10)]
		public int Factor { get; set; } = 1;

		[Caption ("Quality")]
		[MinimumValue (1), MaximumValue (5)]
		public int Quality { get; set; } = 2;

		[Caption ("Zoom")]
		[MinimumValue (0), MaximumValue (100), IncrementValue (0.5)]
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
