using System;
using System.Collections.Generic;
using System.Linq;

namespace Pinta.Core;

public sealed class ImmutableColorGradient
{
	private readonly ColorBgra _startColor;
	private readonly ColorBgra _endColor;
	private readonly double _minPosition;
	private readonly double _maxPosition;
	private readonly IReadOnlyList<KeyValuePair<double, ColorBgra>> _sortedStops;

	public ImmutableColorGradient (
		ColorBgra startColor,
		ColorBgra endColor,
		double minValue,
		double maxValue,
		IEnumerable<KeyValuePair<double, ColorBgra>> gradientStops)
	{
		if (minValue >= maxValue) throw new ArgumentException ($"{nameof (minValue)} has to be lower than {nameof (maxValue)}");
		var sortedStops = gradientStops.OrderBy (kvp => kvp.Key).ToArray ();
		if (sortedStops[0].Key <= minValue) throw new ArgumentException ($"Lowest key in {nameof (gradientStops)} has to be greater than {nameof (minValue)}");
		if (sortedStops[^1].Key >= maxValue) throw new ArgumentException ($"Greatest key in {nameof (gradientStops)} has to be lower than {nameof (maxValue)}");

		_startColor = startColor;
		_endColor = endColor;
		_minPosition = minValue;
		_maxPosition = maxValue;
		_sortedStops = sortedStops;
	}

	public ColorBgra GetColor (double position)
	{
		if (position == _minPosition) return _startColor;
		if (position == _maxPosition) return _endColor;
		if (position < _minPosition) throw new ArgumentOutOfRangeException (nameof (position));
		if (position > _maxPosition) throw new ArgumentOutOfRangeException (nameof (position));
		if (_sortedStops.Count == 0) return HandleNoStops (position);
		return HandleWithStops (position);
	}

	private ColorBgra HandleNoStops (double position)
	{
		double valueSpan = _maxPosition - _minPosition;
		double positionOffset = position - _minPosition;
		double fraction = positionOffset / valueSpan;
		return ColorBgra.Lerp (_startColor, _endColor, fraction);
	}

	private ColorBgra HandleWithStops (double position)
	{
		int immediateLowerIndex = BinarySearchLowerOrEqual (_sortedStops, position);
		var immediatelyLower = immediateLowerIndex < 0 ? KeyValuePair.Create (_minPosition, _startColor) : _sortedStops[immediateLowerIndex];
		if (immediatelyLower.Key == position) return immediatelyLower.Value;
		int immediatelyHigherIndex = immediateLowerIndex + 1;
		var immediatelyHigher = (immediatelyHigherIndex < _sortedStops.Count) ? _sortedStops[immediatelyHigherIndex] : KeyValuePair.Create (_maxPosition, _endColor);
		double valueSpan = immediatelyHigher.Key - immediatelyLower.Key;
		double positionOffset = position - immediatelyLower.Key;
		double fraction = positionOffset / valueSpan;
		return ColorBgra.Lerp (immediatelyLower.Value, immediatelyHigher.Value, fraction);
	}

	/// <returns>Index of number immediately lower than target. If not found, it returns -1</returns>
	/// <param name="arr">Array assumed to be sorted by key</param>
	public static int BinarySearchLowerOrEqual (IReadOnlyList<KeyValuePair<double, ColorBgra>> arr, double target)
	{
		// TODO: Make this method more generic?
		if (arr.Count == 0) return -1;
		int left = 0;
		int right = arr.Count - 1;
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
