using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Pinta.Core;

namespace Pinta.Effects;

internal sealed class ImmutableColorGradient
{
	public ColorBgra StartColor { get; }
	public ColorBgra EndColor { get; }
	public double MinPosition { get; }
	public double MaxPosition { get; }

	public ImmutableArray<KeyValuePair<double, ColorBgra>> SortedStops { get; }

	public ImmutableColorGradient (
		ColorBgra startColor,
		ColorBgra endColor,
		double minPosition,
		double maxPosition,
		IEnumerable<KeyValuePair<double, ColorBgra>> gradientStops)
	{
		if (minPosition >= maxPosition) throw new ArgumentException ($"{nameof (minPosition)} has to be lower than {nameof (maxPosition)}");
		var sortedStops = gradientStops.OrderBy (kvp => kvp.Key).ToImmutableArray ();
		if (sortedStops.Length > 0 && sortedStops[0].Key <= minPosition) throw new ArgumentException ($"Lowest key in {nameof (gradientStops)} has to be greater than {nameof (minPosition)}");
		if (sortedStops.Length > 0 && sortedStops[^1].Key >= maxPosition) throw new ArgumentException ($"Greatest key in {nameof (gradientStops)} has to be lower than {nameof (maxPosition)}");

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
		var immediatelyLower = immediateLowerIndex < 0 ? KeyValuePair.Create (MinPosition, StartColor) : SortedStops[immediateLowerIndex];
		if (immediatelyLower.Key == position) return immediatelyLower.Value;
		int immediatelyHigherIndex = immediateLowerIndex + 1;
		var immediatelyHigher = (immediatelyHigherIndex < SortedStops.Length) ? SortedStops[immediatelyHigherIndex] : KeyValuePair.Create (MaxPosition, EndColor);
		double valueSpan = immediatelyHigher.Key - immediatelyLower.Key;
		double positionOffset = position - immediatelyLower.Key;
		double fraction = positionOffset / valueSpan;
		return ColorBgra.Lerp (immediatelyLower.Value, immediatelyHigher.Value, fraction);
	}

	/// <returns>Index of number immediately lower than target. If not found, it returns -1</returns>
	/// <param name="arr">Array assumed to be sorted by key</param>
	public static int BinarySearchLowerOrEqual (ImmutableArray<KeyValuePair<double, ColorBgra>> arr, double target)
	{
		// TODO: Make this method more generic?
		if (arr.Length == 0) return -1;
		int left = 0;
		int right = arr.Length - 1;
		while (left <= right) {
			int mid = left + (right - left) / 2;
			if (arr[mid].Key == target) {
				return mid;
			} else if (arr[mid].Key < target) {
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
