using System;
using System.Collections.Generic;
using System.Linq;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public enum ColorSchemeSource
{
	[Caption ("Preset Gradient")]
	PresetGradient,

	[Caption ("Selected Colors")]
	SelectedColors,

	[Caption ("Random")]
	Random,
}

public enum PresetGradients
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

	// Translators: Gradient with bright, high-energy, and otherworldly tones of purple and yellow, along with a dark red that gives off the appearance of burning
	[Caption ("Martian Lava")]
	MartianLava,

	// Translators: Gradient with different shades of brownish yellow
	[Caption ("Piña Colada")]
	PinaColada,
}

internal static class GradientHelper
{
	private const double DefaultStartPosition = 0;
	private const double DefaultEndPosition = 1;

	public static ColorGradient CreateBaseGradientForEffect (
		IPaletteService palette,
		ColorSchemeSource colorSchemeSource,
		PresetGradients colorScheme,
		RandomSeed colorSchemeSeed)
	{
		switch (colorSchemeSource) {
			case ColorSchemeSource.PresetGradient:
				return CreateColorGradient (colorScheme);
			case ColorSchemeSource.SelectedColors:
				return ColorGradient.Create (
					palette.PrimaryColor.ToColorBgra (),
					palette.SecondaryColor.ToColorBgra (),
					DefaultStartPosition,
					DefaultEndPosition);
			case ColorSchemeSource.Random:
				Random random = new (colorSchemeSeed.Value);
				ColorBgra startColor = random.RandomColorBgra ();
				ColorBgra endColor = random.RandomColorBgra ();
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
								n => {
									double fraction = Mathematics.InvLerp<double> (0, stopsCount + 1, n + 1);
									return
										KeyValuePair.Create (
											Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, fraction),
											random.RandomColorBgra ());
								}
							)
					);
				}
			default:
				throw new ArgumentOutOfRangeException (nameof (colorSchemeSource));
		}
	}

	public static ColorGradient CreateColorGradient (PresetGradients scheme)
	{
		return scheme switch {

			PresetGradients.BeautifulItaly => ColorGradient.Create (
				ColorBgra.FromBgr (70, 146, 0),
				ColorBgra.FromBgr (55, 43, 206),
				DefaultStartPosition,
				DefaultEndPosition,
				new Dictionary<double, ColorBgra> {
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.25)] = ColorBgra.White,
				}),

			PresetGradients.BlackAndWhite => ColorGradient.Create (
				ColorBgra.White,
				ColorBgra.Black,
				DefaultStartPosition,
				DefaultEndPosition),

			PresetGradients.Bonfire => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.White,
				DefaultStartPosition,
				DefaultEndPosition,
				new Dictionary<double, ColorBgra> {
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.25)] = ColorBgra.Black,
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.50)] = ColorBgra.Red,
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.75)] = ColorBgra.Yellow,
				}),

			PresetGradients.CherryBlossom => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.FromBgr (240, 255, 255),
				DefaultStartPosition,
				DefaultEndPosition,
				new Dictionary<double, ColorBgra> {
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.25)] = ColorBgra.FromBgr (235, 206, 135),
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.75)] = ColorBgra.FromBgr (193, 182, 255),
				}),

			PresetGradients.CottonCandy => ColorGradient.Create (
				ColorBgra.White,
				ColorBgra.FromBgr (242, 235, 214),
				DefaultStartPosition,
				DefaultEndPosition,
				new Dictionary<double, ColorBgra> {
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.25)] = ColorBgra.FromBgr (180, 105, 255),
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.50)] = ColorBgra.FromBgr (219, 112, 219),
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.75)] = ColorBgra.FromBgr (230, 216, 173),
				}),

			PresetGradients.Electric => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.White,
				DefaultStartPosition,
				DefaultEndPosition,
				new Dictionary<double, ColorBgra> {
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.25)] = ColorBgra.Black,
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.50)] = ColorBgra.Blue,
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.75)] = ColorBgra.Cyan,
				}),

			PresetGradients.LimeLemon => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.White,
				DefaultStartPosition,
				DefaultEndPosition,
				new Dictionary<double, ColorBgra> {
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.25)] = ColorBgra.FromBgr (0, 128, 0),
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.50)] = ColorBgra.FromBgr (0, 255, 0),
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.75)] = ColorBgra.FromBgr (0, 255, 255),
				}),

			PresetGradients.MartianLava => ColorGradient.Create (
				ColorBgra.FromBgr (26, 12, 70),
				ColorBgra.FromBgr (93, 117, 228),
				DefaultStartPosition,
				DefaultEndPosition,
				new Dictionary<double, ColorBgra> {
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.2)] = ColorBgra.FromBgr (103, 101, 213),
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.4)] = ColorBgra.FromBgr (25, 219, 200),
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.6)] = ColorBgra.FromBgr (124, 52, 59),
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.8)] = ColorBgra.FromBgr (248, 133, 0),
				}),

			PresetGradients.PinaColada => ColorGradient.Create (
				ColorBgra.FromBgr (0, 128, 128),
				ColorBgra.FromBgr (196, 245, 253),
				DefaultStartPosition,
				DefaultEndPosition,
				new Dictionary<double, ColorBgra> {
					[Mathematics.Lerp (DefaultStartPosition, DefaultEndPosition, 0.25)] = ColorBgra.Yellow,
				}),

			_ => CreateColorGradient (PresetGradients.Electric),
		};
	}
}
