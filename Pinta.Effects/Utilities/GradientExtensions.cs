using System;
using System.ComponentModel;
using Pinta.Core;

namespace Pinta.Effects;

internal static class GradientExtensions
{
	public static ColorBgra GetColorExtended (
		this ColorGradient<ColorBgra> gradient,
		double position,
		EdgeBehavior edgeBehavior,
		IPaletteService palette)
	{
		switch (edgeBehavior) {
			case EdgeBehavior.Clamp:
			case EdgeBehavior.Original:
				if (position > gradient.EndPosition)
					return gradient.EndColor;
				else if (position < gradient.StartPosition)
					return gradient.StartColor;
				break;
			case EdgeBehavior.Wrap: {
					if (position >= gradient.StartPosition && position <= gradient.EndPosition) break;
					double range = gradient.EndPosition - gradient.StartPosition;
					double positionOffset = position - gradient.StartPosition;
					double wrappedOffsetBase = positionOffset % range;
					double wrappedOffset =
						wrappedOffsetBase < 0
						? wrappedOffsetBase + range // Modulo could result in a negative number, so correct it
						: wrappedOffsetBase;
					double adjustedPosition = wrappedOffset + gradient.StartPosition;
					return gradient.GetColor (adjustedPosition);
				}

			case EdgeBehavior.Reflect: {
					if (position >= gradient.StartPosition && position <= gradient.EndPosition) break;
					double range = gradient.EndPosition - gradient.StartPosition;
					double doubleRange = 2 * range;
					double positionOffset = position - gradient.StartPosition;
					double reflectedOffsetBase = positionOffset % doubleRange;
					double reflectedOffset =
						reflectedOffsetBase < 0
						? reflectedOffsetBase + doubleRange // Modulo could result in a negative number, so correct it
						: reflectedOffsetBase;
					double adjustedPosition =
						(reflectedOffset < range)
						? gradient.StartPosition + reflectedOffset
						: gradient.EndPosition - (reflectedOffset - range);
					return gradient.GetColor (adjustedPosition);
				}
			case EdgeBehavior.Primary:
				if (position > gradient.EndPosition)
					return palette.PrimaryColor.ToColorBgra ();
				else if (position < gradient.StartPosition)
					return palette.PrimaryColor.ToColorBgra ();
				break;
			case EdgeBehavior.Secondary:
				if (position > gradient.EndPosition)
					return palette.SecondaryColor.ToColorBgra ();
				else if (position < gradient.StartPosition)
					return palette.SecondaryColor.ToColorBgra ();
				break;
			case EdgeBehavior.Transparent:
				if (position > gradient.EndPosition)
					return ColorBgra.Transparent;
				else if (position < gradient.StartPosition)
					return ColorBgra.Transparent;
				break;
			default:
				throw new InvalidEnumArgumentException (
					nameof (edgeBehavior),
					(int) edgeBehavior,
					typeof (EdgeBehavior));
		}
		return gradient.GetColor (position);
	}
}
