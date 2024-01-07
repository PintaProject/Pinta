using System;
using System.Collections.Generic;
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

	private readonly IReadOnlyList<GradientStop> sorted_stops;

	internal ColorGradient (
		ColorBgra startColor,
		ColorBgra endColor,
		double minPosition,
		double maxPosition,
		IEnumerable<KeyValuePair<double, ColorBgra>> gradientStops)
	{
		CheckBoundsConsistency (minPosition, maxPosition);

		var sortedStops = GetSortedStops (gradientStops);
		CheckStopsBounds (sortedStops, minPosition, maxPosition);
		CheckUniqueness (sortedStops);

		StartColor = startColor;
		EndColor = endColor;
		MinimumPosition = minPosition;
		MaximumPosition = maxPosition;
		sorted_stops = sortedStops;
	}

	private static IReadOnlyList<GradientStop> GetSortedStops (IEnumerable<KeyValuePair<double, ColorBgra>> gradientStops)
		=>
			gradientStops
			.Select (kvp => new GradientStop (kvp.Key, kvp.Value))
			.OrderBy (stop => stop.Position)
			.ToArray ();

	private static void CheckStopsBounds (IReadOnlyList<GradientStop> sortedStops, double minPosition, double maxPosition)
	{
		if (sortedStops.Count > 0 && sortedStops[0].Position <= minPosition) throw new ArgumentException ($"Lowest key in gradient stops has to be greater than {nameof (minPosition)}");
		if (sortedStops.Count > 0 && sortedStops[^1].Position >= maxPosition) throw new ArgumentException ($"Greatest key in gradient stops has to be lower than {nameof (maxPosition)}");
	}

	private static void CheckUniqueness (IReadOnlyList<GradientStop> sortedStops)
	{
		var distinctPositions = sortedStops.GroupBy (s => s.Position).Count ();
		if (distinctPositions != sortedStops.Count) throw new ArgumentException ("Cannot have more than one stop in the same position");
	}

	private static void CheckBoundsConsistency (double minPosition, double maxPosition)
	{
		if (minPosition >= maxPosition) throw new ArgumentException ($"{nameof (minPosition)} has to be lower than {nameof (maxPosition)}");
	}

	public ColorGradient Resized (double minPosition, double maxPosition)
	{
		CheckBoundsConsistency (minPosition, maxPosition);

		double newSpan = maxPosition - minPosition;
		double currentSpan = MaximumPosition - MinimumPosition;
		double newProportion = newSpan / currentSpan;
		double newMinRelativeOffset = minPosition - MinimumPosition;

		KeyValuePair<double, ColorBgra> ToNewStop (GradientStop stop)
		{
			double stopToMinOffset = stop.Position - MinimumPosition;
			double adjustedOffset = stopToMinOffset * newProportion;
			double newPosition = minPosition + adjustedOffset;
			return KeyValuePair.Create (newPosition, stop.Color);
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
		if (sorted_stops.Count == 0) return HandleNoStops (position);
		return HandleWithStops (position);
	}

	private ColorBgra HandleNoStops (double position)
	{
		double valueSpan = MaximumPosition - MinimumPosition;
		double positionOffset = position - MinimumPosition;
		double fraction = positionOffset / valueSpan;
		return ColorBgra.Lerp (StartColor, EndColor, fraction);
	}

	private ColorBgra HandleWithStops (double position)
	{
		int immediateLowerIndex = BinarySearchLowerOrEqual (sorted_stops, position);
		var immediatelyLower = immediateLowerIndex < 0 ? new GradientStop (MinimumPosition, StartColor) : sorted_stops[immediateLowerIndex];
		if (immediatelyLower.Position == position) return immediatelyLower.Color;
		int immediatelyHigherIndex = immediateLowerIndex + 1;
		var immediatelyHigher = (immediatelyHigherIndex < sorted_stops.Count) ? sorted_stops[immediatelyHigherIndex] : new GradientStop (MaximumPosition, EndColor);
		double valueSpan = immediatelyHigher.Position - immediatelyLower.Position;
		double positionOffset = position - immediatelyLower.Position;
		double fraction = positionOffset / valueSpan;
		return ColorBgra.Lerp (immediatelyLower.Color, immediatelyHigher.Color, fraction);
	}

	private readonly record struct GradientStop (double Position, ColorBgra Color);

	/// <returns>Index of number immediately lower than target. If not found, it returns -1</returns>
	/// <param name="arr">Array assumed to be sorted by key</param>
	private static int BinarySearchLowerOrEqual (IReadOnlyList<GradientStop> arr, double target)
	{
		// TODO: Make this method more generic?
		if (arr.Count == 0) return -1;

		int left = 0;
		int right = arr.Count - 1;

		while (left <= right) {
			int mid = left + (right - left) / 2;
			if (arr[mid].Position == target)
				return mid;
			else if (arr[mid].Position < target)
				left = mid + 1;
			else
				right = mid - 1;
		}

		// If target is not found, 'left' will be the index of the number just greater than target
		// so 'left - 1' will be the index of the number immediately lower than target
		return left - 1;
	}

	private static IEnumerable<KeyValuePair<double, ColorBgra>> EmptyStops () => Enumerable.Empty<KeyValuePair<double, ColorBgra>> ();

	public static ColorGradient Create (ColorBgra start, ColorBgra end, double minimum, double maximum)
		=> new (
			start,
			end,
			minimum,
			maximum,
			EmptyStops ()
		);

	public static ColorGradient Create (ColorBgra start, ColorBgra end, double minimum, double maximum, IEnumerable<KeyValuePair<double, ColorBgra>> stops)
		=> new (
			start,
			end,
			minimum,
			maximum,
			stops
		);
}
