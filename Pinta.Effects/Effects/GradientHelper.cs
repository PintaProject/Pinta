using System.Collections.Generic;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

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
	private const double DefaultMinimumValue = 0;
	private const double DefaultMaximumValue = 1;

	public static ColorGradient CreateColorGradient (PredefinedGradients scheme)
	{
		return scheme switch {

			PredefinedGradients.BlackAndWhite => ColorGradient.Create (
				ColorBgra.White,
				ColorBgra.Black,
				DefaultMinimumValue,
				DefaultMaximumValue),

			PredefinedGradients.Bonfire => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.White,
				DefaultMinimumValue,
				DefaultMaximumValue,
				new Dictionary<double, ColorBgra> {
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.2502443792766373)] = ColorBgra.Black,
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.5004887585532747)] = ColorBgra.Red,
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.750733137829912)] = ColorBgra.Yellow,
				}),

			PredefinedGradients.CottonCandy => ColorGradient.Create (
				ColorBgra.White,
				ColorBgra.FromBgr (242, 235, 214),
				DefaultMinimumValue,
				DefaultMaximumValue,
				new Dictionary<double, ColorBgra> {
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.25)] = ColorBgra.FromBgr (180, 105, 255),
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.50)] = ColorBgra.FromBgr (219, 112, 219),
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.75)] = ColorBgra.FromBgr (230, 216, 173),
				}),

			PredefinedGradients.Electric => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.White,
				DefaultMinimumValue,
				DefaultMaximumValue,
				new Dictionary<double, ColorBgra> {
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.2502443792766373)] = ColorBgra.Black,
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.5004887585532747)] = ColorBgra.Blue,
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.750733137829912)] = ColorBgra.Cyan,
				}),

			PredefinedGradients.BeautifulItaly => ColorGradient.Create (
				ColorBgra.FromBgr (70, 146, 0),
				ColorBgra.FromBgr (55, 43, 206),
				DefaultMinimumValue,
				DefaultMaximumValue,
				new Dictionary<double, ColorBgra> {
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.25)] = ColorBgra.White,
				}),

			PredefinedGradients.LimeLemon => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.White,
				DefaultMinimumValue,
				DefaultMaximumValue,
				new Dictionary<double, ColorBgra> {
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.25)] = ColorBgra.FromBgr (0, 128, 0),
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.50)] = ColorBgra.FromBgr (0, 255, 0),
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.75)] = ColorBgra.FromBgr (0, 255, 255),
				}),

			PredefinedGradients.PinaColada => ColorGradient.Create (
				ColorBgra.FromBgr (0, 128, 128),
				ColorBgra.FromBgr (196, 245, 253),
				DefaultMinimumValue,
				DefaultMaximumValue,
				new Dictionary<double, ColorBgra> {
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.25)] = ColorBgra.Yellow,
				}),

			PredefinedGradients.CherryBlossom => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.FromBgr (240, 255, 255),
				DefaultMinimumValue,
				DefaultMaximumValue,
				new Dictionary<double, ColorBgra> {
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.25)] = ColorBgra.FromBgr (235, 206, 135),
					[Utility.Lerp (DefaultMinimumValue, DefaultMaximumValue, 0.75)] = ColorBgra.FromBgr (193, 182, 255),
				}),

			_ => CreateColorGradient (PredefinedGradients.Electric),
		};
	}
}
