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
	public double MinimumPosition { get; }

	/// <summary>
	/// Represents end position in the gradient
	/// </summary>
	public double MaximumPosition { get; }

	private readonly ImmutableArray<double> sorted_positions;
	private readonly ImmutableArray<ColorBgra> sorted_colors;

	internal ColorGradient (
		ColorBgra startColor,
		ColorBgra endColor,
		double minPosition,
		double maxPosition,
		IEnumerable<KeyValuePair<double, ColorBgra>> gradientStops)
	{
		CheckBoundsConsistency (minPosition, maxPosition);

		var sortedStops = gradientStops.OrderBy (stop => stop.Key).ToArray ();
		var sortedPositions = sortedStops.Select (stop => stop.Key).ToImmutableArray ();
		var sortedColors = sortedStops.Select (stop => stop.Value).ToImmutableArray ();
		CheckStopsBounds (sortedPositions, minPosition, maxPosition);
		CheckUniqueness (sortedPositions);

		StartColor = startColor;
		EndColor = endColor;
		MinimumPosition = minPosition;
		MaximumPosition = maxPosition;
		sorted_positions = sortedPositions;
		sorted_colors = sortedColors;
	}

	private static void CheckStopsBounds (ImmutableArray<double> sortedPositions, double minPosition, double maxPosition)
	{
		if (sortedPositions.Length == 0) return;
		if (sortedPositions[0] <= minPosition) throw new ArgumentException ($"Lowest key in gradient stops has to be greater than {nameof (minPosition)}");
		if (sortedPositions[^1] >= maxPosition) throw new ArgumentException ($"Greatest key in gradient stops has to be lower than {nameof (maxPosition)}");
	}

	private static void CheckUniqueness (ImmutableArray<double> sortedPositions)
	{
		var distinctPositions = sortedPositions.GroupBy (s => s).Count ();
		if (distinctPositions != sortedPositions.Length) throw new ArgumentException ("Cannot have more than one stop in the same position");
	}

	private static void CheckBoundsConsistency (double minPosition, double maxPosition)
	{
		if (minPosition >= maxPosition) throw new ArgumentException ($"{nameof (minPosition)} has to be lower than {nameof (maxPosition)}");
	}

	/// <summary>
	/// Creates new gradient object with the lower and upper bounds
	/// (along with all of its stops) adjusted, proportionally,
	/// to the provided lower and upper bounds.
	/// </summary>
	public ColorGradient Resized (double minPosition, double maxPosition)
	{
		CheckBoundsConsistency (minPosition, maxPosition);

		double newSpan = maxPosition - minPosition;
		double currentSpan = MaximumPosition - MinimumPosition;
		double newProportion = newSpan / currentSpan;
		double newMinRelativeOffset = minPosition - MinimumPosition;

		KeyValuePair<double, ColorBgra> ToNewStop (KeyValuePair<double, ColorBgra> stop)
		{
			double stopToMinOffset = stop.Key - MinimumPosition;
			double adjustedOffset = stopToMinOffset * newProportion;
			double newPosition = minPosition + adjustedOffset;
			return KeyValuePair.Create (newPosition, stop.Value);
		}

		return new (
			StartColor,
			EndColor,
			minPosition,
			maxPosition,
			sorted_positions.Zip (sorted_colors, KeyValuePair.Create).Select (ToNewStop)
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
		if (position <= MinimumPosition) return StartColor;
		if (position >= MaximumPosition) return EndColor;
		if (sorted_positions.Length == 0) return HandleNoStops (position);
		return HandleWithStops (position);
	}

	private ColorBgra HandleNoStops (double position)
	{
		double fraction = Utility.InvLerp (MinimumPosition, MaximumPosition, position);
		return ColorBgra.Lerp (StartColor, EndColor, fraction);
	}

	private ColorBgra HandleWithStops (double position)
	{
		int immediatelyHigherIndex = BinarySearchHigherOrEqual (sorted_positions, position);

		if (immediatelyHigherIndex < 0)
			return ColorBgra.Lerp (
				sorted_colors[^1],
				EndColor,
				Utility.InvLerp (sorted_positions[^1], MaximumPosition, position));

		var immediatelyHigher = KeyValuePair.Create (sorted_positions[immediatelyHigherIndex], sorted_colors[immediatelyHigherIndex]);

		bool singleItem = sorted_positions.Length == 1;

		if (singleItem && immediatelyHigher.Key == position)
			return sorted_colors[0];

		if (singleItem)
			return ColorBgra.Lerp (
				StartColor,
				sorted_colors[0],
				Utility.InvLerp (MinimumPosition, sorted_positions[0], position));

		int immediatelyLowerIndex = immediatelyHigherIndex - 1;

		var immediatelyLower = KeyValuePair.Create (sorted_positions[immediatelyLowerIndex], sorted_colors[immediatelyLowerIndex]);

		double fraction = Utility.InvLerp (immediatelyLower.Key, immediatelyHigher.Key, position);

		return ColorBgra.Lerp (immediatelyLower.Value, immediatelyHigher.Value, fraction);
	}

	private static int BinarySearchHigherOrEqual (ImmutableArray<double> sortedPositions, double target)
	{
		if (sortedPositions.Length == 0) return -1;
		int found = sortedPositions.BinarySearch (target);
		if (found > 0) return found; // Exact match
		int foundComplement = ~found;
		if (foundComplement == sortedPositions.Length) return -1; // Not found
		return foundComplement; // Found larger
	}

	private static IEnumerable<KeyValuePair<double, ColorBgra>> EmptyStops ()
		=> Enumerable.Empty<KeyValuePair<double, ColorBgra>> ();

	/// <summary>
	/// Creates gradient mapping based on start and end color,
	/// and the provided lower and upper bounds
	/// </summary>
	public static ColorGradient Create (ColorBgra start, ColorBgra end, double minimum, double maximum)
		=> new (
			start,
			end,
			minimum,
			maximum,
			EmptyStops ()
		);

	/// <summary>
	/// Creates gradient mapping based on start and end color,
	/// and the provided lower and upper bounds, and color stops
	/// </summary>
	public static ColorGradient Create (ColorBgra start, ColorBgra end, double minimum, double maximum, IEnumerable<KeyValuePair<double, ColorBgra>> stops)
		=> new (
			start,
			end,
			minimum,
			maximum,
			stops
		);
}
