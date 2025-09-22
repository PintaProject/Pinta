using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Pinta.Core;

/// <summary>
/// Helps obtain intermediate colors at a certain position,
/// based on the start and end colors, and any additional color stops
/// </summary>
public sealed class ColorGradient<TColor> where TColor : IInterpolableColor<TColor>
{
	/// <summary>
	/// Color at the initial position in the gradient
	/// </summary>
	public TColor StartColor { get; }

	/// <summary>
	/// Color at the end position in the gradient
	/// </summary>
	public TColor EndColor { get; }

	/// <summary>Numerical endpoints of gradient</summary>
	public NumberRange<double> Range { get; }

	public int StopsCount { get; }

	/// <remarks>Sorted</remarks>
	public ImmutableArray<double> Positions { get; }

	/// <remarks>Sorted by position</remarks>
	public ImmutableArray<TColor> Colors { get; }

	internal ColorGradient (
		TColor startColor,
		TColor endColor,
		NumberRange<double> range,
		IEnumerable<KeyValuePair<double, TColor>> gradientStops)
	{
		var sortedStops = gradientStops.OrderBy (stop => stop.Key).ToArray ();
		var sortedPositions = sortedStops.Select (stop => stop.Key).ToImmutableArray ();
		var sortedColors = sortedStops.Select (stop => stop.Value).ToImmutableArray ();
		CheckStopsBounds (sortedPositions, range);
		CheckUniqueness (sortedPositions);

		StartColor = startColor;
		EndColor = endColor;
		Range = range;
		StopsCount = sortedStops.Length;
		Positions = sortedPositions;
		Colors = sortedColors;
	}

	private static void CheckStopsBounds (ImmutableArray<double> sortedPositions, NumberRange<double> range)
	{
		if (sortedPositions.Length == 0) return;
		if (sortedPositions[0] <= range.Lower) throw new ArgumentException ($"Lowest key in gradient stops has to be greater than {nameof (range.Lower)}");
		if (sortedPositions[^1] >= range.Upper) throw new ArgumentException ($"Greatest key in gradient stops has to be lower than {nameof (range.Upper)}");
	}

	private static void CheckUniqueness (ImmutableArray<double> sortedPositions)
	{
		var distinctPositions = sortedPositions.GroupBy (s => s).Count ();
		if (distinctPositions != sortedPositions.Length) throw new ArgumentException ("Cannot have more than one stop in the same position");
	}

	/// <summary>
	/// Creates new gradient object with the lower and upper bounds
	/// (along with all of its stops) adjusted, proportionally,
	/// to the provided lower and upper bounds.
	/// </summary>
	public ColorGradient<TColor> Resized (NumberRange<double> newRange)
	{
		if (Range.Lower == newRange.Lower && Range.Upper == newRange.Upper) return this;

		double newSpan = newRange.Upper - newRange.Lower;
		double currentSpan = Range.Upper - Range.Lower;
		double newProportion = newSpan / currentSpan;
		double newMinRelativeOffset = newRange.Lower - Range.Lower;

		KeyValuePair<double, TColor> ToNewStop (KeyValuePair<double, TColor> stop)
		{
			double stopToMinOffset = stop.Key - Range.Lower;
			double adjustedOffset = stopToMinOffset * newProportion;
			double newPosition = newRange.Lower + adjustedOffset;
			return KeyValuePair.Create (newPosition, stop.Value);
		}

		return new (
			StartColor,
			EndColor,
			newRange,
			Positions.Zip (Colors, KeyValuePair.Create).Select (ToNewStop));
	}

	/// <returns>
	/// New gradient where the start color is now the end color,
	/// and vice versa. Also, the color stops are in reversed positions
	/// (in the new gradient, the colors are at the same distance
	/// from the end color as they were from the start color in the original)
	/// </returns>
	public ColorGradient<TColor> Reversed ()
	{
		var reversedPositions =
			Positions
			.Select (p => Range.Upper - p + Range.Lower);

		var reversedStops =
			reversedPositions
			.Zip (Colors, KeyValuePair.Create);

		return new ColorGradient<TColor> (
			startColor: EndColor,
			endColor: StartColor,
			range: Range,
			gradientStops: reversedStops);
	}

	/// <returns>
	/// Intermediate color, according to start and end colors,
	/// and gradient stops.
	/// No overflow occurs as such;
	/// if the target position is lower than the start position,
	/// the start color will be returned, and if it's higher than
	/// the end position, the end color will be returned.
	/// </returns>
	public TColor GetColor (double position)
	{
		if (position <= Range.Lower) return StartColor;
		if (position >= Range.Upper) return EndColor;
		if (Positions.Length == 0) return HandleNoStops (position);
		return HandleWithStops (position);
	}

	private TColor HandleNoStops (double position)
	{
		double fraction = Mathematics.InvLerp (Range.Lower, Range.Upper, position);
		return TColor.Lerp (StartColor, EndColor, fraction);
	}

	private TColor HandleWithStops (double position)
	{
		int matchIndex = Positions.BinarySearch (position);
		if (matchIndex >= 0) return Colors[matchIndex]; // Exact match
		int matchComplement = ~matchIndex;
		if (matchComplement == Positions.Length) // Not found. Using end color
			return TColor.Lerp (
				Colors[^1],
				EndColor,
				Mathematics.InvLerp (Positions[^1], Range.Upper, position));
		var immediatelyHigher = KeyValuePair.Create (Positions[matchComplement], Colors[matchComplement]);
		int immediatelyLowerIndex = matchComplement - 1;
		if (immediatelyLowerIndex < 0) // No stops before
			return TColor.Lerp (
				StartColor,
				immediatelyHigher.Value,
				Mathematics.InvLerp (Range.Lower, immediatelyHigher.Key, position));
		var immediatelyLower = KeyValuePair.Create (Positions[immediatelyLowerIndex], Colors[immediatelyLowerIndex]);
		return TColor.Lerp ( // Stops exist both before and after
			immediatelyLower.Value,
			immediatelyHigher.Value,
			Mathematics.InvLerp (immediatelyLower.Key, immediatelyHigher.Key, position));
	}
}

public static class ColorGradient
{
	private static IEnumerable<KeyValuePair<double, TColor>> EmptyStops<TColor> ()
		=> [];

	/// <summary>
	/// Creates gradient mapping based on start and end color,
	/// and a default lower bound of 0 and an upper bound of 1
	/// </summary>
	public static ColorGradient<TColor> Create<TColor> (
		TColor startColor,
		TColor endColor
	)
		where TColor : IInterpolableColor<TColor>
	=> new (
		startColor,
		endColor,
		NumberRange.Create<double> (0, 1),
		EmptyStops<TColor> ()
	);

	/// <summary>
	/// Creates gradient mapping based on start and end color,
	/// and the provided lower and upper bounds
	/// </summary>
	public static ColorGradient<TColor> Create<TColor> (
		TColor startColor,
		TColor endColor,
		NumberRange<double> range
	)
		where TColor : IInterpolableColor<TColor>
	=> new (
		startColor,
		endColor,
		range,
		EmptyStops<TColor> ()
	);

	/// <summary>
	/// Creates gradient mapping based on start and end color,
	/// and the provided lower and upper bounds, and color stops
	/// </summary>
	public static ColorGradient<TColor> Create<TColor> (
		TColor startColor,
		TColor endColor,
		NumberRange<double> range,
		IEnumerable<KeyValuePair<double, TColor>> stops
	)
		where TColor : IInterpolableColor<TColor>
	=> new (
		startColor,
		endColor,
		range,
		stops
	);
}
