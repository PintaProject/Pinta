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

	private readonly ImmutableArray<KeyValuePair<double, ColorBgra>> sorted_stops;

	internal ColorGradient (
		ColorBgra startColor,
		ColorBgra endColor,
		double minPosition,
		double maxPosition,
		IEnumerable<KeyValuePair<double, ColorBgra>> gradientStops)
	{
		CheckBoundsConsistency (minPosition, maxPosition);

		var sortedStops = gradientStops.OrderBy (stop => stop.Key).ToImmutableArray ();
		CheckStopsBounds (sortedStops, minPosition, maxPosition);
		CheckUniqueness (sortedStops);

		StartColor = startColor;
		EndColor = endColor;
		MinimumPosition = minPosition;
		MaximumPosition = maxPosition;
		sorted_stops = sortedStops;
	}

	private static void CheckStopsBounds (ImmutableArray<KeyValuePair<double, ColorBgra>> sortedStops, double minPosition, double maxPosition)
	{
		if (sortedStops.Length > 0 && sortedStops[0].Key <= minPosition) throw new ArgumentException ($"Lowest key in gradient stops has to be greater than {nameof (minPosition)}");
		if (sortedStops.Length > 0 && sortedStops[^1].Key >= maxPosition) throw new ArgumentException ($"Greatest key in gradient stops has to be lower than {nameof (maxPosition)}");
	}

	private static void CheckUniqueness (ImmutableArray<KeyValuePair<double, ColorBgra>> sortedStops)
	{
		var distinctPositions = sortedStops.GroupBy (s => s.Key).Count ();
		if (distinctPositions != sortedStops.Length) throw new ArgumentException ("Cannot have more than one stop in the same position");
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
			sorted_stops.Select (ToNewStop)
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
		if (sorted_stops.Length == 0) return HandleNoStops (position);
		return HandleWithStops (position);
	}

	private ColorBgra HandleNoStops (double position)
	{
		double fraction = Utility.InvLerp (MinimumPosition, MaximumPosition, position);
		return ColorBgra.Lerp (StartColor, EndColor, fraction);
	}

	private ColorBgra HandleWithStops (double position)
	{
		int immediatelyHigherIndex = BinarySearchHigherOrEqual (sorted_stops, position);
		var immediatelyHigher = immediatelyHigherIndex < 0 ? KeyValuePair.Create (MaximumPosition, EndColor) : sorted_stops[immediatelyHigherIndex];
		if (immediatelyHigher.Key == position) return immediatelyHigher.Value;
		int immediatelyLowerIndex = immediatelyHigherIndex - 1;
		var immediatelyLower = immediatelyLowerIndex >= 0 ? sorted_stops[immediatelyLowerIndex] : KeyValuePair.Create (MinimumPosition, StartColor);
		double fraction = Utility.InvLerp (immediatelyLower.Key, immediatelyHigher.Key, position);
		return ColorBgra.Lerp (immediatelyLower.Value, immediatelyHigher.Value, fraction);
	}

	private static readonly IComparer<KeyValuePair<double, ColorBgra>> stop_position_comparer
		= Comparer<KeyValuePair<double, ColorBgra>>.Create ((a, b) => a.Key.CompareTo (b.Key));
	private static int BinarySearchHigherOrEqual (ImmutableArray<KeyValuePair<double, ColorBgra>> sortedStops, double target)
	{
		// https://learn.microsoft.com/en-us/dotnet/api/system.collections.immutable.immutablearray.binarysearch
		var adapted = KeyValuePair.Create (target, ColorBgra.Black);
		int found = sortedStops.BinarySearch (adapted, stop_position_comparer);
		if (found > 0) return found; // Exact match
		int foundComplement = ~found;
		if (foundComplement == sortedStops.Length) return -1; // Not found
		return foundComplement; // Found larger
	}

	private static IEnumerable<KeyValuePair<double, ColorBgra>> EmptyStops () => Enumerable.Empty<KeyValuePair<double, ColorBgra>> ();

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
