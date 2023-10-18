using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Pinta.Core;

public sealed class ImmutableColorGradient
{
	public ColorBgra StartColor { get; }
	public ColorBgra EndColor { get; }
	public double MinPosition { get; }
	public double MaxPosition { get; }

	public ImmutableArray<GradientStop> SortedStops { get; }

	public ImmutableColorGradient (
		ColorBgra startColor,
		ColorBgra endColor,
		double minPosition,
		double maxPosition,
		IEnumerable<KeyValuePair<double, ColorBgra>> gradientStops)
	{
		if (minPosition >= maxPosition) throw new ArgumentException ($"{nameof (minPosition)} has to be lower than {nameof (maxPosition)}");
		var sortedStops = gradientStops.Select (kvp => new GradientStop (kvp.Key, kvp.Value)).OrderBy (stop => stop.Position).ToImmutableArray ();
		if (sortedStops.Length > 0 && sortedStops[0].Position <= minPosition) throw new ArgumentException ($"Lowest key in {nameof (gradientStops)} has to be greater than {nameof (minPosition)}");
		if (sortedStops.Length > 0 && sortedStops[^1].Position >= maxPosition) throw new ArgumentException ($"Greatest key in {nameof (gradientStops)} has to be lower than {nameof (maxPosition)}");

		StartColor = startColor;
		EndColor = endColor;
		MinPosition = minPosition;
		MaxPosition = maxPosition;
		SortedStops = sortedStops;
	}

	public ColorBgra GetColor (double position)
	{
		if (position < MinPosition) throw new ArgumentOutOfRangeException (nameof (position));
		if (position > MaxPosition) throw new ArgumentOutOfRangeException (nameof (position));
		if (position == MinPosition) return StartColor;
		if (position == MaxPosition) return EndColor;
		if (SortedStops.Length == 0) return HandleNoStops (position);
		return HandleWithStops (position);
	}

	private ColorBgra HandleNoStops (double position)
	{
		double valueSpan = MaxPosition - MinPosition;
		double positionOffset = position - MinPosition;
		double fraction = positionOffset / valueSpan;
		return ColorBgra.Lerp (StartColor, EndColor, fraction);
	}

	private ColorBgra HandleWithStops (double position)
	{
		int immediateLowerIndex = BinarySearchLowerOrEqual (SortedStops, position);
		var immediatelyLower = immediateLowerIndex < 0 ? new GradientStop (MinPosition, StartColor) : SortedStops[immediateLowerIndex];
		if (immediatelyLower.Position == position) return immediatelyLower.Color;
		int immediatelyHigherIndex = immediateLowerIndex + 1;
		var immediatelyHigher = (immediatelyHigherIndex < SortedStops.Length) ? SortedStops[immediatelyHigherIndex] : new GradientStop (MaxPosition, EndColor);
		double valueSpan = immediatelyHigher.Position - immediatelyLower.Position;
		double positionOffset = position - immediatelyLower.Position;
		double fraction = positionOffset / valueSpan;
		return ColorBgra.Lerp (immediatelyLower.Color, immediatelyHigher.Color, fraction);
	}

	public readonly record struct GradientStop (double Position, ColorBgra Color);

	/// <returns>Index of number immediately lower than target. If not found, it returns -1</returns>
	/// <param name="arr">Array assumed to be sorted by key</param>
	public static int BinarySearchLowerOrEqual (ImmutableArray<GradientStop> arr, double target)
	{
		// TODO: Make this method more generic?
		if (arr.Length == 0) return -1;
		int left = 0;
		int right = arr.Length - 1;
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
}
