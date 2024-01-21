using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Pinta.Core;

namespace Pinta.Effects;

/// <summary>
/// Helps obtain intermediate colors at a certain position,
/// based on the start and end colors, and any additional color stops
/// </summary>
internal sealed class ColorGradient
{
	/// <summary>
	/// Color at the initial position in the gradient
	/// </summary>
	public ColorBgra StartColor { get; }

	/// <summary>
	/// Color at the end position in the gradient
	/// </summary>
	public ColorBgra EndColor { get; }

	/// <summary>
	/// Represents initial position in the gradient
	/// </summary>
	public double StartPosition { get; }

	/// <summary>
	/// Represents end position in the gradient
	/// </summary>
	public double EndPosition { get; }

	private readonly ImmutableArray<double> sorted_positions;
	private readonly ImmutableArray<ColorBgra> sorted_colors;

	internal ColorGradient (
		ColorBgra startColor,
		ColorBgra endColor,
		double startPosition,
		double endPosition,
		IEnumerable<KeyValuePair<double, ColorBgra>> gradientStops)
	{
		CheckBoundsConsistency (startPosition, endPosition);

		var sortedStops = gradientStops.OrderBy (stop => stop.Key).ToArray ();
		var sortedPositions = sortedStops.Select (stop => stop.Key).ToImmutableArray ();
		var sortedColors = sortedStops.Select (stop => stop.Value).ToImmutableArray ();
		CheckStopsBounds (sortedPositions, startPosition, endPosition);
		CheckUniqueness (sortedPositions);

		StartColor = startColor;
		EndColor = endColor;
		StartPosition = startPosition;
		EndPosition = endPosition;
		sorted_positions = sortedPositions;
		sorted_colors = sortedColors;
	}

	private static void CheckStopsBounds (ImmutableArray<double> sortedPositions, double startPosition, double endPosition)
	{
		if (sortedPositions.Length == 0) return;
		if (sortedPositions[0] <= startPosition) throw new ArgumentException ($"Lowest key in gradient stops has to be greater than {nameof (startPosition)}");
		if (sortedPositions[^1] >= endPosition) throw new ArgumentException ($"Greatest key in gradient stops has to be lower than {nameof (endPosition)}");
	}

	private static void CheckUniqueness (ImmutableArray<double> sortedPositions)
	{
		var distinctPositions = sortedPositions.GroupBy (s => s).Count ();
		if (distinctPositions != sortedPositions.Length) throw new ArgumentException ("Cannot have more than one stop in the same position");
	}

	private static void CheckBoundsConsistency (double startPosition, double endPosition)
	{
		if (startPosition >= endPosition) throw new ArgumentException ($"{nameof (startPosition)} has to be lower than {nameof (endPosition)}");
	}

	/// <summary>
	/// Creates new gradient object with the lower and upper bounds
	/// (along with all of its stops) adjusted, proportionally,
	/// to the provided lower and upper bounds.
	/// </summary>
	public ColorGradient Resized (double startPosition, double endPosition)
	{
		if (StartPosition == startPosition && EndPosition == endPosition) return this;

		CheckBoundsConsistency (startPosition, endPosition);

		double newSpan = endPosition - startPosition;
		double currentSpan = EndPosition - StartPosition;
		double newProportion = newSpan / currentSpan;
		double newMinRelativeOffset = startPosition - StartPosition;

		KeyValuePair<double, ColorBgra> ToNewStop (KeyValuePair<double, ColorBgra> stop)
		{
			double stopToMinOffset = stop.Key - StartPosition;
			double adjustedOffset = stopToMinOffset * newProportion;
			double newPosition = startPosition + adjustedOffset;
			return KeyValuePair.Create (newPosition, stop.Value);
		}

		return new (
			StartColor,
			EndColor,
			startPosition,
			endPosition,
			sorted_positions.Zip (sorted_colors, KeyValuePair.Create).Select (ToNewStop)
		);
	}

	/// <returns>
	/// New gradient where the start color is now the end color,
	/// and vice versa. Also, the color stops are in reversed positions
	/// (in the new gradient, the colors are at the same distance
	/// from the end color as they were from the start color in the original)
	/// </returns>
	public ColorGradient Reversed ()
	{
		var reversedPosition = sorted_positions.Select (p => EndPosition - p);

		var reversedStops =
			reversedPosition
			.Zip (sorted_colors, KeyValuePair.Create);

		return new ColorGradient (
			startColor: EndColor,
			endColor: StartColor,
			startPosition: StartPosition,
			endPosition: EndPosition,
			gradientStops: reversedStops
		);
	}

	/// <returns>
	/// Intermediate color, according to start and end colors,
	/// and gradient stops.
	/// No overflow occurs as such;
	/// if the target position is lower than the start position,
	/// the start color will be returned, and if it's higher than
	/// the end position, the end color will be returned.
	/// </returns>
	public ColorBgra GetColor (double position)
	{
		if (position <= StartPosition) return StartColor;
		if (position >= EndPosition) return EndColor;
		if (sorted_positions.Length == 0) return HandleNoStops (position);
		return HandleWithStops (position);
	}

	private ColorBgra HandleNoStops (double position)
	{
		double fraction = Utility.InvLerp (StartPosition, EndPosition, position);
		return ColorBgra.Lerp (StartColor, EndColor, fraction);
	}

	private ColorBgra HandleWithStops (double position)
	{
		int matchIndex = sorted_positions.BinarySearch (position);
		if (matchIndex >= 0) return sorted_colors[matchIndex]; // Exact match
		int matchComplement = ~matchIndex;
		if (matchComplement == sorted_positions.Length) // Not found. Using end color
			return ColorBgra.Lerp (
				sorted_colors[^1],
				EndColor,
				Utility.InvLerp (sorted_positions[^1], EndPosition, position));
		var immediatelyHigher = KeyValuePair.Create (sorted_positions[matchComplement], sorted_colors[matchComplement]);
		int immediatelyLowerIndex = matchComplement - 1;
		if (immediatelyLowerIndex < 0) // No stops before
			return ColorBgra.Lerp (
				StartColor,
				immediatelyHigher.Value,
				Utility.InvLerp (StartPosition, immediatelyHigher.Key, position));
		var immediatelyLower = KeyValuePair.Create (sorted_positions[immediatelyLowerIndex], sorted_colors[immediatelyLowerIndex]);
		return ColorBgra.Lerp ( // Stops exist both before and after
			immediatelyLower.Value,
			immediatelyHigher.Value,
			Utility.InvLerp (immediatelyLower.Key, immediatelyHigher.Key, position));
	}

	private static IEnumerable<KeyValuePair<double, ColorBgra>> EmptyStops ()
		=> Enumerable.Empty<KeyValuePair<double, ColorBgra>> ();

	/// <summary>
	/// Creates gradient mapping based on start and end color,
	/// and the provided lower and upper bounds
	/// </summary>
	public static ColorGradient Create (ColorBgra startColor, ColorBgra endColor, double startPosition, double endPosition)
		=> new (
			startColor,
			endColor,
			startPosition,
			endPosition,
			EmptyStops ()
		);

	/// <summary>
	/// Creates gradient mapping based on start and end color,
	/// and the provided lower and upper bounds, and color stops
	/// </summary>
	public static ColorGradient Create (ColorBgra startColor, ColorBgra endColor, double startPosition, double endPosition, IEnumerable<KeyValuePair<double, ColorBgra>> stops)
		=> new (
			startColor,
			endColor,
			startPosition,
			endPosition,
			stops
		);
}
