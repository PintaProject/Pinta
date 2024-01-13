using System;
using System.Collections.Generic;
using System.Linq;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public enum ColorSchemeSource
{
	[Caption ("Predefined Gradient")]
	PredefinedGradient,

	[Caption ("Selected Colors")]
	SelectedColors,

	[Caption ("Random")]
	Random,
}

public enum PredefinedGradients
{
	// Translators: Gradient with the colors of the flag of Italy: red, white, and green
	[Caption ("Beautiful Italy")]
	BeautifulItaly,

	// Translators: Simple gradient that goes from black to white
	[Caption ("Black and White")]
	BlackAndWhite,

	// Translators: Gradient that starts out white, like the core of a raging fire, and then goes through yellow, red, and black (like visible black smoke), and finally transparent, blending with the background
	[Caption ("Bonfire")]
	Bonfire,

	// Translators: Gradient that starts out off-white, like cherry blossoms against sunlight, then goes through pink, then light blue (like the sky) and finally transparent, blending with the background
	[Caption ("Cherry Blossom")]
	CherryBlossom,

	// Translators: Gradient with the colors of blue and pink cotton candy
	[Caption ("Cotton Candy")]
	CottonCandy,

	// Translators: Gradient that starts out white, like the the inner part of a spark, and goes through progressively dark shades of blue until it reaches black, and finally transparent, blending with the background
	[Caption ("Electric")]
	Electric,

	// Translators: Gradient with a citrusy vibe that starts out white, goes through light yellow, several shades of green, and then transparent, blending with the background
	[Caption ("Lime Lemon")]
	LimeLemon,

	// Translators: Gradient with different shades of brownish yellow
	[Caption ("Piña Colada")]
	PinaColada,
}

internal static class GradientHelper
{
	private const double DefaultStartPosition = 0;
	private const double DefaultEndPosition = 1;

	public static ColorGradient CreateBaseGradientForEffect (
		ColorSchemeSource colorSchemeSource,
		PredefinedGradients colorScheme,
		RandomSeed colorSchemeSeed)
	{
		switch (colorSchemeSource) {
			case ColorSchemeSource.PredefinedGradient:
				return CreateColorGradient (colorScheme);
			case ColorSchemeSource.SelectedColors:
				return ColorGradient.Create (
					PintaCore.Palette.PrimaryColor.ToColorBgra (),
					PintaCore.Palette.SecondaryColor.ToColorBgra (),
					DefaultStartPosition,
					DefaultEndPosition);
			case ColorSchemeSource.Random:
				Random random = new (colorSchemeSeed.Value);
				ColorBgra startColor = RandomColor (random);
				ColorBgra endColor = RandomColor (random);
				int stopsCount = random.Next (0, 5);
				if (stopsCount == 0) {
					return ColorGradient.Create (
						startColor,
						endColor,
						DefaultStartPosition,
						DefaultEndPosition);
				} else {
					return ColorGradient.Create (
						startColor,
						endColor,
						DefaultStartPosition,
						DefaultEndPosition,
						Enumerable
						.Range (0, stopsCount)
						.Select (
							n => KeyValuePair.Create (
								Utility.Lerp (DefaultStartPosition, DefaultEndPosition, (n + 1) / (stopsCount + 1)),
								RandomColor (random)
							)
						)
					);
				}
			default:
				throw new ArgumentOutOfRangeException (nameof (colorSchemeSource));
		}
	}

	private static ColorBgra RandomColor (Random random)
	{
		Span<byte> colorBytes = stackalloc byte[4];
		random.NextBytes (colorBytes);
		uint unsignedInteger = BitConverter.ToUInt32 (colorBytes);
		return ColorBgra.FromUInt32 (unsignedInteger);
	}

	public static ColorGradient CreateColorGradient (PredefinedGradients scheme)
	{
		return scheme switch {

			PredefinedGradients.BeautifulItaly => ColorGradient.Create (
				ColorBgra.FromBgr (70, 146, 0),
				ColorBgra.FromBgr (55, 43, 206),
				DefaultStartPosition,
				DefaultEndPosition,
				new Dictionary<double, ColorBgra> {
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.25)] = ColorBgra.White,
				}),

			PredefinedGradients.BlackAndWhite => ColorGradient.Create (
				ColorBgra.White,
				ColorBgra.Black,
				DefaultStartPosition,
				DefaultEndPosition),

			PredefinedGradients.Bonfire => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.White,
				DefaultStartPosition,
				DefaultEndPosition,
				new Dictionary<double, ColorBgra> {
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.25)] = ColorBgra.Black,
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.50)] = ColorBgra.Red,
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.75)] = ColorBgra.Yellow,
				}),

			PredefinedGradients.CherryBlossom => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.FromBgr (240, 255, 255),
				DefaultStartPosition,
				DefaultEndPosition,
				new Dictionary<double, ColorBgra> {
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.25)] = ColorBgra.FromBgr (235, 206, 135),
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.75)] = ColorBgra.FromBgr (193, 182, 255),
				}),

			PredefinedGradients.CottonCandy => ColorGradient.Create (
				ColorBgra.White,
				ColorBgra.FromBgr (242, 235, 214),
				DefaultStartPosition,
				DefaultEndPosition,
				new Dictionary<double, ColorBgra> {
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.25)] = ColorBgra.FromBgr (180, 105, 255),
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.50)] = ColorBgra.FromBgr (219, 112, 219),
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.75)] = ColorBgra.FromBgr (230, 216, 173),
				}),

			PredefinedGradients.Electric => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.White,
				DefaultStartPosition,
				DefaultEndPosition,
				new Dictionary<double, ColorBgra> {
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.25)] = ColorBgra.Black,
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.50)] = ColorBgra.Blue,
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.75)] = ColorBgra.Cyan,
				}),

			PredefinedGradients.LimeLemon => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.White,
				DefaultStartPosition,
				DefaultEndPosition,
				new Dictionary<double, ColorBgra> {
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.25)] = ColorBgra.FromBgr (0, 128, 0),
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.50)] = ColorBgra.FromBgr (0, 255, 0),
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.75)] = ColorBgra.FromBgr (0, 255, 255),
				}),

			PredefinedGradients.PinaColada => ColorGradient.Create (
				ColorBgra.FromBgr (0, 128, 128),
				ColorBgra.FromBgr (196, 245, 253),
				DefaultStartPosition,
				DefaultEndPosition,
				new Dictionary<double, ColorBgra> {
					[Utility.Lerp (DefaultStartPosition, DefaultEndPosition, 0.25)] = ColorBgra.Yellow,
				}),

			_ => CreateColorGradient (PredefinedGradients.Electric),
		};
	}
}
