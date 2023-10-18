using System.Collections.Generic;
using System.Linq;
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
	public static ImmutableColorGradient CreateColorGradient (PredefinedGradients scheme)
	{
		const double Outer = 0;
		const double Core = 1023;
		return scheme switch {
			PredefinedGradients.BlackAndWhite => new ImmutableColorGradient (
				ColorBgra.White,
				ColorBgra.Black,
				Outer,
				Core,
				Enumerable.Empty<KeyValuePair<double, ColorBgra>> ()
			),
			PredefinedGradients.CottonCandy => new ImmutableColorGradient (
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
			PredefinedGradients.Electric => new ImmutableColorGradient (
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
			PredefinedGradients.LaBellaItalia => new ImmutableColorGradient (
				ColorBgra.FromBgr (70, 146, 0),
				ColorBgra.FromBgr (55, 43, 206),
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[256] = ColorBgra.White,
				}
			),
			PredefinedGradients.LimeLemon => new ImmutableColorGradient (
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
			PredefinedGradients.PinaColada => new ImmutableColorGradient (
				ColorBgra.FromBgr (0, 128, 128),
				ColorBgra.FromBgr (196, 245, 253),
				Outer,
				Core,
				new Dictionary<double, ColorBgra> {
					[256] = ColorBgra.Yellow,
				}
			),
			PredefinedGradients.SakuraSigh => new ImmutableColorGradient (
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
