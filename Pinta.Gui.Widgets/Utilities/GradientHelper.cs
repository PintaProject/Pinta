using System.Collections.Generic;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public enum PredefinedGradients
{
	[Caption ("Black and White")]
	BlackAndWhite,

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

public static class GradientHelper
{
	public static ColorGradient CreateColorGradient (PredefinedGradients scheme)
	{
		const double Outer = 0;
		const double Core = 1023;
		return scheme switch {
			PredefinedGradients.BlackAndWhite => ColorMapping.Gradient (
				ColorBgra.White,
				ColorBgra.Black,
				Outer,
				Core
			),
			PredefinedGradients.CottonCandy => ColorMapping.Gradient (
				ColorBgra.White,
				ColorBgra.FromBgr (242, 235, 214),
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[256] = ColorBgra.FromBgr (180, 105, 255),
					[512] = ColorBgra.FromBgr (219, 112, 219),
					[768] = ColorBgra.FromBgr (230, 216, 173),
				}
			),
			PredefinedGradients.Electric => ColorMapping.Gradient (
				ColorBgra.Transparent,
				ColorBgra.White,
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[256] = ColorBgra.Black,
					[512] = ColorBgra.Blue,
					[768] = ColorBgra.Cyan,
				}
			),
			PredefinedGradients.LaBellaItalia => ColorMapping.Gradient (
				ColorBgra.FromBgr (70, 146, 0),
				ColorBgra.FromBgr (55, 43, 206),
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[256] = ColorBgra.White,
				}
			),
			PredefinedGradients.LimeLemon => ColorMapping.Gradient (
				ColorBgra.Transparent,
				ColorBgra.White,
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[256] = ColorBgra.FromBgr (0, 128, 0),
					[512] = ColorBgra.FromBgr (0, 255, 0),
					[768] = ColorBgra.FromBgr (0, 255, 255),
				}
			),
			PredefinedGradients.PinaColada => ColorMapping.Gradient (
				ColorBgra.FromBgr (0, 128, 128),
				ColorBgra.FromBgr (196, 245, 253),
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[256] = ColorBgra.Yellow,
				}
			),
			PredefinedGradients.SakuraSigh => ColorMapping.Gradient (
				ColorBgra.Transparent,
				ColorBgra.FromBgr (240, 255, 255),
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[256] = ColorBgra.FromBgr (235, 206, 135),
					[768] = ColorBgra.FromBgr (193, 182, 255),
				}

			),
			_ => CreateColorGradient (PredefinedGradients.Electric)
		};
	}
}
