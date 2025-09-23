using System.ComponentModel;
using System.Diagnostics;
using Pinta.Core;

namespace Pinta.Effects;

internal static class GradientExtensions
{
	public static ColorBgra GetColorExtended (
		this ColorGradient<ColorBgra> gradient,
		double position,
		EdgeBehavior edgeBehavior,
		ColorBgra original,
		IPaletteService palette)
	{
		if (position >= gradient.Range.Lower && position <= gradient.Range.Upper)
			return gradient.GetColor (position);

		switch (edgeBehavior) {
			case EdgeBehavior.Clamp:
				if (position > gradient.Range.Upper)
					return gradient.EndColor;
				else if (position < gradient.Range.Lower)
					return gradient.StartColor;
				break;
			case EdgeBehavior.Wrap: {
					double range = gradient.Range.Upper - gradient.Range.Lower;
					double positionOffset = position - gradient.Range.Lower;
					double wrappedOffsetBase = positionOffset % range;
					double wrappedOffset =
						wrappedOffsetBase < 0
						? wrappedOffsetBase + range // Modulo could result in a negative number, so correct it
						: wrappedOffsetBase;
					double adjustedPosition = wrappedOffset + gradient.Range.Lower;
					return gradient.GetColor (adjustedPosition);
				}

			case EdgeBehavior.Reflect: {
					double range = gradient.Range.Upper - gradient.Range.Lower;
					double doubleRange = 2 * range;
					double positionOffset = position - gradient.Range.Lower;
					double reflectedOffsetBase = positionOffset % doubleRange;
					double reflectedOffset =
						reflectedOffsetBase < 0
						? reflectedOffsetBase + doubleRange // Modulo could result in a negative number, so correct it
						: reflectedOffsetBase;
					double adjustedPosition =
						(reflectedOffset < range)
						? gradient.Range.Lower + reflectedOffset
						: gradient.Range.Upper - (reflectedOffset - range);
					return gradient.GetColor (adjustedPosition);
				}
			case EdgeBehavior.Primary:
				return palette.PrimaryColor.ToColorBgra ();
			case EdgeBehavior.Secondary:
				return palette.SecondaryColor.ToColorBgra ();
			case EdgeBehavior.Transparent:
				return ColorBgra.Transparent;
			case EdgeBehavior.Original:
				return original;
			default:
				throw new InvalidEnumArgumentException (
					nameof (edgeBehavior),
					(int) edgeBehavior,
					typeof (EdgeBehavior));
		}
		throw new UnreachableException ();
	}
}
