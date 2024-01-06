using System.Collections.Generic;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public enum PredefinedGradients
{
	[Caption ("Black and White")]
	BlackAndWhite,

	[Caption ("Bonfire")]
	Bonfire,

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

internal static class GradientHelper
{
	public static ColorGradient CreateColorGradient (PredefinedGradients scheme)
	{
		const double Outer = 0;
		const double Core = 1;

		return scheme switch {
			PredefinedGradients.BlackAndWhite => ColorGradient.Create (
				ColorBgra.White,
				ColorBgra.Black,
				Outer,
				Core),

			PredefinedGradients.Bonfire => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.White,
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[0.25] = ColorBgra.Black,
					[0.50] = ColorBgra.Red,
					[0.75] = ColorBgra.Yellow,
				}),

			PredefinedGradients.CottonCandy => ColorGradient.Create (
				ColorBgra.White,
				ColorBgra.FromBgr (242, 235, 214),
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[0.25] = ColorBgra.FromBgr (180, 105, 255),
					[0.50] = ColorBgra.FromBgr (219, 112, 219),
					[0.75] = ColorBgra.FromBgr (230, 216, 173),
				}),

			PredefinedGradients.Electric => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.White,
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[0.25] = ColorBgra.Black,
					[0.50] = ColorBgra.Blue,
					[0.75] = ColorBgra.Cyan,
				}),

			PredefinedGradients.LaBellaItalia => ColorGradient.Create (
				ColorBgra.FromBgr (70, 146, 0),
				ColorBgra.FromBgr (55, 43, 206),
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[0.25] = ColorBgra.White,
				}),

			PredefinedGradients.LimeLemon => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.White,
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[0.25] = ColorBgra.FromBgr (0, 128, 0),
					[0.50] = ColorBgra.FromBgr (0, 255, 0),
					[0.75] = ColorBgra.FromBgr (0, 255, 255),
				}),

			PredefinedGradients.PinaColada => ColorGradient.Create (
				ColorBgra.FromBgr (0, 128, 128),
				ColorBgra.FromBgr (196, 245, 253),
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[0.25] = ColorBgra.Yellow,
				}),

			PredefinedGradients.SakuraSigh => ColorGradient.Create (
				ColorBgra.Transparent,
				ColorBgra.FromBgr (240, 255, 255),
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[0.25] = ColorBgra.FromBgr (235, 206, 135),
					[0.75] = ColorBgra.FromBgr (193, 182, 255),
				}),

			_ => CreateColorGradient (PredefinedGradients.Electric),
		};
	}
}
