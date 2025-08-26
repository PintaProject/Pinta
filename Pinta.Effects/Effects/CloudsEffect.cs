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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class CloudsEffect : BaseEffect
{
	public sealed override bool IsTileable
		=> true;

	public override string Icon
		=> Resources.Icons.EffectsRenderClouds;

	public override string Name
		=> Translations.GetString ("Clouds");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Render");

	public CloudsData Data
		=> (CloudsData) EffectData!;  // NRT - Set in constructor

	private readonly IPaletteService palette;
	private readonly IWorkspaceService workspace;
	private readonly IChromeService chrome;
	public CloudsEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		palette = services.GetService<IPaletteService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new CloudsData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	private readonly record struct CloudsSettings (
		int Scale,
		byte Seed,
		double Power,
		ColorGradient<ColorBgra> Gradient,
		RectangleI Roi,
		Size Size,
		UserBlendOp Blending);

	private CloudsSettings CreateSettings (ImageSurface destination, RectangleI roi)
	{
		CloudsData data = Data;

		var baseGradient =
			GradientHelper
			.CreateBaseGradientForEffect (
				palette,
				data.ColorSchemeSource,
				data.ColorScheme,
				data.ColorSchemeSeed)
			.Resized (0, 1);

		return new (
			Scale: data.Scale,
			Seed: unchecked((byte) data.Seed.Value),
			Power: data.Power / 100.0,
			Gradient: data.ReverseColorScheme ? baseGradient.Reversed () : baseGradient,
			Roi: roi,
			Size: destination.GetSize (),
			Blending: GetBlendOp (data.BlendMode));
	}

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		CloudsSettings settings = CreateSettings (destination, roi);
		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();
		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, settings.Size))
			destinationData[pixel.memoryOffset] = GetFinalPixelColor (
				settings,
				sourceData[pixel.memoryOffset],
				pixel.coordinates);
	}

	private static ColorBgra GetFinalPixelColor (in CloudsSettings settings, ColorBgra original, PointI coordinates)
	{
		int dx = 2 * coordinates.X - settings.Roi.Width;
		int dy = 2 * coordinates.Y - settings.Roi.Height;

		double val = 0;
		double mult = 1;
		int div = settings.Scale;

		for (int i = 0; i < 12 && mult > 0.03 && div > 0; ++i) {

			PointD dr = new (
				X: 65536 + dx / (double) div,
				Y: 65536 + dy / (double) div);

			PointI dd = new (
				X: (int) dr.X,
				Y: (int) dr.Y);

			PointD df = new (
				X: dr.X - dd.X,
				Y: dr.Y - dd.Y);

			double noise = PerlinNoise.Compute (
				unchecked((byte) dd.X),
				unchecked((byte) dd.Y),
				df,
				(byte) (settings.Seed ^ i)
			);

			val += noise * mult;
			div /= 2;
			mult *= settings.Power;
		}

		ColorBgra cloudColor = settings.Gradient.GetColor ((val + 1) / 2);

		return settings.Blending.Apply (original, cloudColor);
	}

	private static UserBlendOp GetBlendOp (string blendModeName)
	{
		// TODO: Implement missing blend ops,
		// so that one can use UserBlendOps.GetAllBlendModeNames ()
		// and something like UserBlendOps.GetBlendModeByName ()

		return blendModeName switch {

			"Multiply" => new UserBlendOps.MultiplyBlendOp (),
			"Color Burn" => new UserBlendOps.ColorBurnBlendOp (),
			"Color Dodge" => new UserBlendOps.ColorDodgeBlendOp (),
			"Overlay" => new UserBlendOps.OverlayBlendOp (),
			"Difference" => new UserBlendOps.DifferenceBlendOp (),
			"Lighten" => new UserBlendOps.LightenBlendOp (),
			"Darken" => new UserBlendOps.DarkenBlendOp (),
			"Screen" => new UserBlendOps.ScreenBlendOp (),
			"Xor" => new UserBlendOps.XorBlendOp (), // TODO: Seems to behave differently from previous implementation

			// TODO: Hard Light
			// TODO: Soft Light
			// TODO: Color
			// TODO: Luminosity
			// TODO: Hue
			// TODO: Saturation

			"Additive" => new UserBlendOps.AdditiveBlendOp (),
			"Negation" => new UserBlendOps.NegationBlendOp (),
			"Reflect" => new UserBlendOps.ReflectBlendOp (),
			"Glow" => new UserBlendOps.GlowBlendOp (),

			_ => new UserBlendOps.NormalBlendOp (),
		};
	}


	public sealed class CloudsData : EffectData
	{
		[Skip]
		public override bool IsDefault => Power == 0;

		[Caption ("Scale")]
		[MinimumValue (2), MaximumValue (1000)]
		public int Scale { get; set; } = 250;

		[Caption ("Power")]
		[MinimumValue (0), MaximumValue (100)]
		public int Power { get; set; } = 50;

		[Skip]
		public static ReadOnlyDictionary<string, object> BlendOps { get; }

		[Skip]
		private static readonly string default_blend_op;

		static CloudsData ()
		{
			Dictionary<string, BlendMode> allOps = new () {
				{ Translations.GetString("Normal"), Core.BlendMode.Normal },
				{ Translations.GetString("Multiply"), Core.BlendMode.Multiply },
				{ Translations.GetString("Additive"), (BlendMode) 100},
				{ Translations.GetString("Color Burn"), Core.BlendMode.ColorBurn },
				{ Translations.GetString("Color Dodge"), Core.BlendMode.ColorDodge },
				{ Translations.GetString("Reflect"), (BlendMode) 101 },
				{ Translations.GetString("Glow"), (BlendMode) 102 },
				{ Translations.GetString("Overlay"), Core.BlendMode.Overlay },
				{ Translations.GetString("Difference"), Core.BlendMode.Difference },
				{ Translations.GetString("Negation"), (BlendMode) 103 },
				{ Translations.GetString("Lighten"), Core.BlendMode.Lighten },
				{ Translations.GetString("Darken"), Core.BlendMode.Darken },
				{ Translations.GetString("Screen"), Core.BlendMode.Screen },
				{ Translations.GetString("Xor"), Core.BlendMode.Xor },
			};

			BlendOps =
				allOps
				.ToDictionary (kvp => kvp.Key, kvp => (object) kvp.Value)
				.AsReadOnly ();

			default_blend_op = UserBlendOps.GetBlendModeName (Core.BlendMode.Normal);
		}

		[StaticList (nameof (BlendOps))]
		public string BlendMode { get; set; } = default_blend_op;

		[Caption ("Random Noise Seed")]
		public RandomSeed Seed { get; set; } = new (0);

		[Caption ("Color Scheme Source")]
		public ColorSchemeSource ColorSchemeSource { get; set; } = ColorSchemeSource.SelectedColors;

		[Caption ("Color Scheme")]
		public PresetGradients ColorScheme { get; set; } = PresetGradients.BeautifulItaly;

		[Caption ("Random Color Scheme Seed")]
		public RandomSeed ColorSchemeSeed { get; set; } = new (0);

		[Caption ("Reverse Color Scheme")]
		public bool ReverseColorScheme { get; set; } = false;
	}
}
