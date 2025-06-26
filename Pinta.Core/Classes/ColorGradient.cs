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
public sealed class ColorGradient<TColor> where TColor : IColor<TColor>
{
	/// <summary>
	/// Color at the initial position in the gradient
	/// </summary>
	public TColor StartColor { get; }

	/// <summary>
	/// Color at the end position in the gradient
	/// </summary>
	public TColor EndColor { get; }

	/// <summary>
	/// Represents initial position in the gradient
	/// </summary>
	public double StartPosition { get; }

	/// <summary>
	/// Represents end position in the gradient
	/// </summary>
	public double EndPosition { get; }

	private readonly ImmutableArray<double> sorted_positions;
	private readonly ImmutableArray<TColor> sorted_colors;

	internal ColorGradient (
		TColor startColor,
		TColor endColor,
		double startPosition,
		double endPosition,
		IEnumerable<KeyValuePair<double, TColor>> gradientStops)
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
	public ColorGradient<TColor> Resized (double startPosition, double endPosition)
	{
		if (StartPosition == startPosition && EndPosition == endPosition) return this;

		CheckBoundsConsistency (startPosition, endPosition);

		double newSpan = endPosition - startPosition;
		double currentSpan = EndPosition - StartPosition;
		double newProportion = newSpan / currentSpan;
		double newMinRelativeOffset = startPosition - StartPosition;

		KeyValuePair<double, TColor> ToNewStop (KeyValuePair<double, TColor> stop)
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
	public ColorGradient<TColor> Reversed ()
	{
		var reversedPosition = sorted_positions.Select (p => EndPosition - p);

		var reversedStops =
			reversedPosition
			.Zip (sorted_colors, KeyValuePair.Create);

		return new ColorGradient<TColor> (
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
	public TColor GetColor (double position)
	{
		if (position <= StartPosition) return StartColor;
		if (position >= EndPosition) return EndColor;
		if (sorted_positions.Length == 0) return HandleNoStops (position);
		return HandleWithStops (position);
	}

	private TColor HandleNoStops (double position)
	{
		double fraction = Mathematics.InvLerp (StartPosition, EndPosition, position);
		return TColor.Lerp (StartColor, EndColor, fraction);
	}

	private TColor HandleWithStops (double position)
	{
		int matchIndex = sorted_positions.BinarySearch (position);
		if (matchIndex >= 0) return sorted_colors[matchIndex]; // Exact match
		int matchComplement = ~matchIndex;
		if (matchComplement == sorted_positions.Length) // Not found. Using end color
			return TColor.Lerp (
				sorted_colors[^1],
				EndColor,
				Mathematics.InvLerp (sorted_positions[^1], EndPosition, position));
		var immediatelyHigher = KeyValuePair.Create (sorted_positions[matchComplement], sorted_colors[matchComplement]);
		int immediatelyLowerIndex = matchComplement - 1;
		if (immediatelyLowerIndex < 0) // No stops before
			return TColor.Lerp (
				StartColor,
				immediatelyHigher.Value,
				Mathematics.InvLerp (StartPosition, immediatelyHigher.Key, position));
		var immediatelyLower = KeyValuePair.Create (sorted_positions[immediatelyLowerIndex], sorted_colors[immediatelyLowerIndex]);
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
	/// and the provided lower and upper bounds
	/// </summary>
	public static ColorGradient<TColor> Create<TColor> (
		TColor startColor,
		TColor endColor,
		double startPosition,
		double endPosition
	)
		where TColor : IColor<TColor>
	=> new (
		startColor,
		endColor,
		startPosition,
		endPosition,
		EmptyStops<TColor> ()
	);

	/// <summary>
	/// Creates gradient mapping based on start and end color,
	/// and the provided lower and upper bounds, and color stops
	/// </summary>
	public static ColorGradient<TColor> Create<TColor> (
		TColor startColor,
		TColor endColor,
		double startPosition,
		double endPosition,
		IEnumerable<KeyValuePair<double, TColor>> stops
	)
		where TColor : IColor<TColor>
	=> new (
		startColor,
		endColor,
		startPosition,
		endPosition,
		stops
	);
}
