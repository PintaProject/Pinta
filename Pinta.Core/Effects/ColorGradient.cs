using System;
using System.Collections.Generic;
using System.Linq;

namespace Pinta.Core;

public sealed class ColorGradient : ColorMapping
{
	public ColorBgra StartColor { get; }
	public ColorBgra EndColor { get; }
	public double MinimumPosition { get; }
	public double MaximumPosition { get; }

	private readonly IReadOnlyList<GradientStop> sorted_stops; // TODO: Before making it public think of nice structures

	internal ColorGradient (
		ColorBgra startColor,
		ColorBgra endColor,
		double minPosition,
		double maxPosition,
		IEnumerable<KeyValuePair<double, ColorBgra>> gradientStops)
	{
		if (minPosition >= maxPosition) throw new ArgumentException ($"{nameof (minPosition)} has to be lower than {nameof (maxPosition)}");
		var sortedStops = gradientStops.Select (kvp => new GradientStop (kvp.Key, kvp.Value)).OrderBy (stop => stop.Position).ToArray ();
		if (sortedStops.Length > 0 && sortedStops[0].Position <= minPosition) throw new ArgumentException ($"Lowest key in {nameof (gradientStops)} has to be greater than {nameof (minPosition)}");
		if (sortedStops.Length > 0 && sortedStops[^1].Position >= maxPosition) throw new ArgumentException ($"Greatest key in {nameof (gradientStops)} has to be lower than {nameof (maxPosition)}");
		var distinctPositions = sortedStops.GroupBy (s => s.Position).Count ();
		if (distinctPositions != sortedStops.Length) throw new ArgumentException ("Cannot have more than one stop in the same position");

		StartColor = startColor;
		EndColor = endColor;
		MinimumPosition = minPosition;
		MaximumPosition = maxPosition;
		sorted_stops = sortedStops;
	}

	public sealed override ColorBgra GetColor (double position)
	{
		if (position == MinimumPosition) return StartColor;
		if (position == MaximumPosition) return EndColor;
		if (!IsMapped (position)) throw new ArgumentOutOfRangeException (nameof (position));
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
			if (arr[mid].Position == target) {
				return mid;
			} else if (arr[mid].Position < target) {
				left = mid + 1;
			} else {
				right = mid - 1;
			}
		}

		// If target is not found, 'left' will be the index of the number just greater than target
		// so 'left - 1' will be the index of the number immediately lower than target
		return left - 1;
	}

	public sealed override bool IsMapped (double position) => position >= MinimumPosition && position <= MaximumPosition;
}
