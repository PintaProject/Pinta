using System.Collections.Generic;
using System.Linq;

namespace Pinta.Core;

public abstract class ColorMapping
{
	private protected ColorMapping () { }
	public abstract ColorBgra GetColor (double position);

	private static ColorBgra DefaultStartColor () => ColorBgra.Black;
	private static ColorBgra DefaultEndColor () => ColorBgra.White;
	private static double DefaultMinimumValue () => 0;
	private static double DefaultMaximumValue () => 255;
	private static IEnumerable<KeyValuePair<double, ColorBgra>> EmptyStops () => Enumerable.Empty<KeyValuePair<double, ColorBgra>> ();

	public static ColorGradient Gradient ()
	{
		return new ColorGradient (
			DefaultStartColor (),
			DefaultEndColor (),
			DefaultMinimumValue (),
			DefaultMaximumValue (),
			EmptyStops ()
		);
	}

	public static ColorGradient Gradient (ColorBgra start, ColorBgra end)
	{
		return new ColorGradient (
			start,
			end,
			DefaultMinimumValue (),
			DefaultMaximumValue (),
			EmptyStops ()
		);
	}

	public static ColorGradient Gradient (ColorBgra start, ColorBgra end, double minimum, double maximum)
	{
		return new ColorGradient (
			start,
			end,
			minimum,
			maximum,
			EmptyStops ()
		);
	}

	public static ColorGradient Gradient (ColorBgra start, ColorBgra end, double minimum, double maximum, IEnumerable<KeyValuePair<double, ColorBgra>> stops)
	{
		return new ColorGradient (
			start,
			end,
			minimum,
			maximum,
			stops
		);
	}
}
