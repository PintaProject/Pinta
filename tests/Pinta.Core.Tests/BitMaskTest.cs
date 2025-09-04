using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class BitMaskTest
{
	const int DEFAULT_SIZE = 1;
	const int DEFAULT_OFFSET = 0;

	const int DEFAULT_HEIGHT = DEFAULT_SIZE;
	const int DEFAULT_HEIGHT_INDEX = DEFAULT_OFFSET;

	const int DEFAULT_WIDTH = DEFAULT_SIZE;
	const int DEFAULT_WIDTH_INDEX = DEFAULT_OFFSET;

	[TestCase (-1)]
	public void Constructor_RejectsInvalidWidth (int width)
	{
		Assert.Throws<ArgumentOutOfRangeException> (() => new BitMask (width, DEFAULT_HEIGHT));
	}

	[TestCase (-1)]
	public void Constructor_RejectsInvalidHeight (int height)
	{
		Assert.Throws<ArgumentOutOfRangeException> (() => new BitMask (DEFAULT_WIDTH, height));
	}

	[TestCaseSource (nameof (out_of_bounds_access_cases))]
	public void WidthAccessOutOfBoundsFails (int desiredWidth, int indexToAccess)
	{
		BitMask mask = new (desiredWidth, DEFAULT_HEIGHT);
		PointI coordinates = new (indexToAccess, DEFAULT_HEIGHT_INDEX);
		Assert.Throws<ArgumentOutOfRangeException> (() => _ = mask[indexToAccess, DEFAULT_HEIGHT_INDEX]);
		Assert.Throws<ArgumentOutOfRangeException> (() => _ = mask[coordinates]);
	}

	[TestCaseSource (nameof (within_bounds_access_cases))]
	public void WidthAccessWithinBoundsSucceeds (int desiredWidth, int indexToAccess)
	{
		BitMask mask = new (desiredWidth, DEFAULT_HEIGHT);
		PointI coordinates = new (indexToAccess, DEFAULT_HEIGHT_INDEX);
		Assert.DoesNotThrow (() => _ = mask[indexToAccess, DEFAULT_HEIGHT_INDEX]);
		Assert.DoesNotThrow (() => _ = mask[coordinates]);
	}

	[TestCaseSource (nameof (out_of_bounds_access_cases))]
	public void HeightAccessOutOfBoundsFails (int desiredHeight, int indexToAccess)
	{
		BitMask mask = new (DEFAULT_WIDTH, desiredHeight);
		PointI coordinates = new (DEFAULT_WIDTH_INDEX, indexToAccess);
		Assert.Throws<ArgumentOutOfRangeException> (() => _ = mask[DEFAULT_WIDTH_INDEX, indexToAccess]);
		Assert.Throws<ArgumentOutOfRangeException> (() => _ = mask[coordinates]);
	}

	[TestCaseSource (nameof (within_bounds_access_cases))]
	public void HeightAccessWithinBoundsSucceeds (int desiredHeight, int indexToAccess)
	{
		BitMask mask = new (DEFAULT_WIDTH, desiredHeight);
		PointI coordinates = new (DEFAULT_WIDTH_INDEX, indexToAccess);
		Assert.DoesNotThrow (() => _ = mask[DEFAULT_WIDTH_INDEX, indexToAccess]);
		Assert.DoesNotThrow (() => _ = mask[coordinates]);
	}

	[TestCase (DEFAULT_WIDTH, DEFAULT_HEIGHT, DEFAULT_WIDTH_INDEX, DEFAULT_HEIGHT_INDEX)]
	public void BitInitializedToFalse (int maskWidth, int maskHeight, int bitToTestX, int bitToTestY)
	{
		BitMask mask = new (maskWidth, maskHeight);
		bool bit = mask[bitToTestX, bitToTestY];
		Assert.That (bit, Is.False);
	}

	[TestCase (DEFAULT_WIDTH, DEFAULT_HEIGHT, DEFAULT_WIDTH_INDEX, DEFAULT_HEIGHT_INDEX)]
	public void BitInvertsWithXY (int maskWidth, int maskHeight, int bitToInvertX, int bitToInvertY)
	{
		BitMask mask = new (maskWidth, maskHeight);
		mask.Invert (bitToInvertX, bitToInvertY);
		bool bit = mask[bitToInvertX, bitToInvertY];
		Assert.That (bit, Is.True);
	}

	[TestCase (DEFAULT_WIDTH, DEFAULT_HEIGHT, DEFAULT_WIDTH_INDEX, DEFAULT_HEIGHT_INDEX, new[] { true, false, true, false })]
	public void BitGetsSetXY (int maskWidth, int maskHeight, int bitToSetX, int bitToSetY, bool[] valuesToSetAndTest)
	{
		BitMask mask = new (maskWidth, maskHeight);
		PointI coordinates = new (bitToSetX, bitToSetY);
		foreach (var value in valuesToSetAndTest) {
			mask.Set (bitToSetX, bitToSetY, value);
			Assert.That (mask[bitToSetX, bitToSetY], Is.EqualTo (value));
			Assert.That (mask[coordinates], Is.EqualTo (value));
		}
	}

	[TestCaseSource (nameof (invalid_indexing))]
	public void RejectsInvalidIndexing_PairIndexer (int width, int height, int x, int y)
	{
		BitMask bitmask = new (width, height);
		Assert.Throws<ArgumentOutOfRangeException> (() => _ = bitmask[x, y]);
	}

	[TestCaseSource (nameof (invalid_indexing))]
	public void RejectsInvalidIndexing_PointIndexer (int width, int height, int x, int y)
	{
		BitMask bitmask = new (width, height);
		PointI point = new (x, y);
		Assert.Throws<ArgumentOutOfRangeException> (() => _ = bitmask[point]);
	}

	[TestCaseSource (nameof (invalid_indexing))]
	public void RejectsInvalidIndexing_GetMethod (int width, int height, int x, int y)
	{
		BitMask bitmask = new (width, height);
		Assert.Throws<ArgumentOutOfRangeException> (() => _ = bitmask.Get (x, y));
	}

	[TestCaseSource (nameof (invalid_indexing))]
	public void RejectsInvalidIndexing_Invert (int width, int height, int x, int y)
	{
		BitMask bitmask = new (width, height);
		Assert.Throws<ArgumentOutOfRangeException> (() => bitmask.Invert (x, y));
	}

	[TestCaseSource (nameof (invalid_indexing))]
	public void RejectsInvalidIndexing_SetPair (int width, int height, int x, int y)
	{
		BitMask bitmask1 = new (width, height);
		BitMask bitmask2 = new (width, height);
		Assert.Throws<ArgumentOutOfRangeException> (() => bitmask1.Set (x, y, true));
		Assert.Throws<ArgumentOutOfRangeException> (() => bitmask2.Set (x, y, false));
	}

	[TestCaseSource (nameof (rectangle_set_test_cases))]
	public void RectangleSetCorrectly (int width, int height, IEnumerable<KeyValuePair<RectangleI, bool>> areasToSet, IReadOnlyDictionary<PointI, bool> checks)
	{
		BitMask bitmask = new (width, height);

		foreach (var kvp in areasToSet)
			bitmask.Set (kvp.Key, kvp.Value);

		foreach (var kvp in checks) {
			Assert.That (kvp.Value, Is.EqualTo (bitmask[kvp.Key]));
			Assert.That (kvp.Value, Is.EqualTo (bitmask[kvp.Key.X, kvp.Key.Y]));
		}
	}

	[TestCaseSource (nameof (scanline_invert_test_cases))]
	public void ScanlineInvertedCorrectly (int width, int height, IEnumerable<Scanline> scanlineInversionSequence, IReadOnlyDictionary<PointI, bool> checks)
	{
		BitMask bitmask = new (width, height);

		foreach (var scanline in scanlineInversionSequence)
			bitmask.Invert (scanline);

		foreach (var kvp in checks) {
			Assert.That (kvp.Value, Is.EqualTo (bitmask[kvp.Key]));
			Assert.That (kvp.Value, Is.EqualTo (bitmask[kvp.Key.X, kvp.Key.Y]));
		}
	}

	[TestCaseSource (nameof (vertical_flip_cases))]
	public void VerticalFlip (BitMask mask, IReadOnlyDictionary<PointI, bool> checksAfter)
	{
		BitMask clone = mask.Clone ();
		clone.FlipVertical ();
		foreach (var kvp in checksAfter)
			Assert.That (kvp.Value, Is.EqualTo (clone[kvp.Key]));
	}

	[TestCaseSource (nameof (horizontal_flip_cases))]
	public void HorizontalFlip (BitMask mask, IReadOnlyDictionary<PointI, bool> checksAfter)
	{
		BitMask clone = mask.Clone ();
		clone.FlipHorizontal ();
		foreach (var kvp in checksAfter)
			Assert.That (kvp.Value, Is.EqualTo (clone[kvp.Key]));
	}

	[TestCaseSource (nameof (and_cases))]
	public void And (BitMask left, BitMask right, IReadOnlyDictionary<PointI, bool> checksAfter)
	{
		BitMask leftClone = left.Clone ();
		leftClone.And (right);
		foreach (var kvp in checksAfter)
			Assert.That (kvp.Value, Is.EqualTo (leftClone[kvp.Key]));
	}

	[TestCaseSource (nameof (or_cases))]
	public void Or (BitMask left, BitMask right, IReadOnlyDictionary<PointI, bool> checksAfter)
	{
		BitMask leftClone = left.Clone ();
		leftClone.Or (right);
		foreach (var kvp in checksAfter)
			Assert.That (kvp.Value, Is.EqualTo (leftClone[kvp.Key]));
	}

	[TestCaseSource (nameof (xor_cases))]
	public void Xor (BitMask left, BitMask right, IReadOnlyDictionary<PointI, bool> checksAfter)
	{
		BitMask leftClone = left.Clone ();
		leftClone.Xor (right);
		foreach (var kvp in checksAfter)
			Assert.That (kvp.Value, Is.EqualTo (leftClone[kvp.Key]));
	}

	static readonly IReadOnlyList<TestCaseData> xor_cases = CreateXorCases ().ToArray ();
	static IEnumerable<TestCaseData> CreateXorCases ()
	{
		PointI topLeft = new (0, 0);
		BitMask topLeftEnabled = new (2, 2);
		topLeftEnabled[topLeft] = true;

		PointI topRight = new (1, 0);
		BitMask topRightEnabled = new (2, 2);
		topRightEnabled[topRight] = true;

		PointI bottomLeft = new (0, 1);
		BitMask bottomLeftEnabled = new (2, 2);
		bottomLeftEnabled[bottomLeft] = true;

		PointI bottomRight = new (1, 1);
		BitMask bottomRightEnabled = new (2, 2);
		bottomRightEnabled[bottomRight] = true;

		yield return new (
			topLeftEnabled,
			topRightEnabled,
			new Dictionary<PointI, bool> {
				[topLeft] = true,
				[topRight] = true,
				[bottomLeft] = false,
				[bottomRight] = false,
			}
		);

		yield return new (
			topLeftEnabled,
			bottomLeftEnabled,
			new Dictionary<PointI, bool> {
				[topLeft] = true,
				[topRight] = false,
				[bottomLeft] = true,
				[bottomRight] = false,
			}
		);

		yield return new (
			topLeftEnabled,
			bottomRightEnabled,
			new Dictionary<PointI, bool> {
				[topLeft] = true,
				[topRight] = false,
				[bottomLeft] = false,
				[bottomRight] = true,
			}
		);

		yield return new (
			topLeftEnabled,
			topLeftEnabled,
			new Dictionary<PointI, bool> {
				[topLeft] = false,
				[topRight] = false,
				[bottomLeft] = false,
				[bottomRight] = false,
			}
		);
	}

	static readonly IReadOnlyList<TestCaseData> or_cases = CreateOrCases ().ToArray ();
	static IEnumerable<TestCaseData> CreateOrCases ()
	{
		PointI topLeft = new (0, 0);
		BitMask topLeftEnabled = new (2, 2);
		topLeftEnabled[topLeft] = true;

		PointI topRight = new (1, 0);
		BitMask topRightEnabled = new (2, 2);
		topRightEnabled[topRight] = true;

		PointI bottomLeft = new (0, 1);
		BitMask bottomLeftEnabled = new (2, 2);
		bottomLeftEnabled[bottomLeft] = true;

		PointI bottomRight = new (1, 1);
		BitMask bottomRightEnabled = new (2, 2);
		bottomRightEnabled[bottomRight] = true;

		yield return new (
			topLeftEnabled,
			topRightEnabled,
			new Dictionary<PointI, bool> {
				[topLeft] = true,
				[topRight] = true,
				[bottomLeft] = false,
				[bottomRight] = false,
			}
		);

		yield return new (
			topLeftEnabled,
			bottomLeftEnabled,
			new Dictionary<PointI, bool> {
				[topLeft] = true,
				[topRight] = false,
				[bottomLeft] = true,
				[bottomRight] = false,
			}
		);

		yield return new (
			topLeftEnabled,
			bottomRightEnabled,
			new Dictionary<PointI, bool> {
				[topLeft] = true,
				[topRight] = false,
				[bottomLeft] = false,
				[bottomRight] = true,
			}
		);

		yield return new (
			topLeftEnabled,
			topLeftEnabled,
			new Dictionary<PointI, bool> {
				[topLeft] = true,
				[topRight] = false,
				[bottomLeft] = false,
				[bottomRight] = false,
			}
		);
	}

	static readonly IReadOnlyList<TestCaseData> and_cases = CreateAndCases ().ToArray ();
	static IEnumerable<TestCaseData> CreateAndCases ()
	{
		PointI topLeft = new (0, 0);
		BitMask topLeftEnabled = new (2, 2);
		topLeftEnabled[topLeft] = true;

		PointI topRight = new (1, 0);
		BitMask topRightEnabled = new (2, 2);
		topRightEnabled[topRight] = true;

		PointI bottomLeft = new (0, 1);
		BitMask bottomLeftEnabled = new (2, 2);
		bottomLeftEnabled[bottomLeft] = true;

		PointI bottomRight = new (1, 1);
		BitMask bottomRightEnabled = new (2, 2);
		bottomRightEnabled[bottomRight] = true;

		BitMask biggerAllEnabled = new (3, 3);
		biggerAllEnabled.Clear (true);

		BitMask smallerEnabled = new (1, 1);
		smallerEnabled.Clear (true);

		yield return new (
			topLeftEnabled,
			topRightEnabled,
			new Dictionary<PointI, bool> {
				[topLeft] = false,
				[topRight] = false,
				[bottomLeft] = false,
				[bottomRight] = false,
			}
		);

		yield return new (
			topLeftEnabled,
			bottomLeftEnabled,
			new Dictionary<PointI, bool> {
				[topLeft] = false,
				[topRight] = false,
				[bottomLeft] = false,
				[bottomRight] = false,
			}
		);

		yield return new (
			topLeftEnabled,
			bottomRightEnabled,
			new Dictionary<PointI, bool> {
				[topLeft] = false,
				[topRight] = false,
				[bottomLeft] = false,
				[bottomRight] = false,
			}
		);

		yield return new (
			topLeftEnabled,
			topLeftEnabled,
			new Dictionary<PointI, bool> {
				[topLeft] = true,
				[topRight] = false,
				[bottomLeft] = false,
				[bottomRight] = false,
			}
		);

		yield return new (
			bottomLeftEnabled,
			biggerAllEnabled,
			new Dictionary<PointI, bool> {
				[topLeft] = false,
				[topRight] = false,
				[bottomLeft] = true,
				[bottomRight] = false,
			}
		);

		yield return new (
			bottomLeftEnabled,
			smallerEnabled,
			new Dictionary<PointI, bool> {
				[topLeft] = false,
				[topRight] = false,
				[bottomLeft] = true,
				[bottomRight] = false,
			}
		);
	}

	static readonly IReadOnlyList<TestCaseData> vertical_flip_cases = CreateVerticalFlipCases ().ToArray ();
	static IEnumerable<TestCaseData> CreateVerticalFlipCases ()
	{
		BitMask topLeftEnabled = new (2, 2);
		topLeftEnabled[0, 0] = true;
		yield return new (
			topLeftEnabled,
			new Dictionary<PointI, bool> {
				[new (0, 0)] = false,
				[new (0, 1)] = true,
			}
		);
	}

	static readonly IReadOnlyList<TestCaseData> horizontal_flip_cases = CreateHorizontalFlipCases ().ToArray ();
	static IEnumerable<TestCaseData> CreateHorizontalFlipCases ()
	{
		BitMask topLeftEnabled = new (2, 2);
		topLeftEnabled[0, 0] = true;
		yield return new (
			topLeftEnabled,
			new Dictionary<PointI, bool> {
				[new (0, 0)] = false,
				[new (1, 0)] = true,
			}
		);
	}

	static readonly IReadOnlyList<TestCaseData> scanline_invert_test_cases = CreateScanlineInvertTestCases ().ToArray ();
	static IEnumerable<TestCaseData> CreateScanlineInvertTestCases ()
	{
		const int WIDTH = 16;
		const int HEIGHT = 16;

		Scanline topLeftLine = new (0, 0, 4);

		var singleTopLeftSequence = new[] { topLeftLine };
		Dictionary<PointI, bool> singleTopLeftChangedChecks = new () {
			[new (0, 0)] = true,
			[new (3, 0)] = true,
		};
		Dictionary<PointI, bool> singleTopLeftOutOfRangeChecks = new () {
			[new (4, 0)] = false,
			[new (0, 1)] = false,
		};
		yield return new (WIDTH, HEIGHT, singleTopLeftSequence, singleTopLeftChangedChecks);
		yield return new (WIDTH, HEIGHT, singleTopLeftSequence, singleTopLeftOutOfRangeChecks);

		var doubleTopLeftSequence = Enumerable.Repeat (topLeftLine, 2);
		var doubleTopLeftChecks = singleTopLeftChangedChecks.ToDictionary (kvp => kvp.Key, kvp => !kvp.Value);
		yield return new (WIDTH, HEIGHT, doubleTopLeftSequence, doubleTopLeftChecks);
		yield return new (WIDTH, HEIGHT, doubleTopLeftSequence, singleTopLeftOutOfRangeChecks);

		var singlePixelSequence = new[] { new Scanline (DEFAULT_WIDTH_INDEX, DEFAULT_HEIGHT_INDEX, 1) };
		Dictionary<PointI, bool> singlePixelChecks = new () { [new (DEFAULT_WIDTH_INDEX, DEFAULT_HEIGHT_INDEX)] = true };
		yield return new (DEFAULT_WIDTH, DEFAULT_HEIGHT, singlePixelSequence, singlePixelChecks);
	}

	static readonly IReadOnlyList<TestCaseData> rectangle_set_test_cases = CreateRectangleSetTestCases ().ToArray ();
	static IEnumerable<TestCaseData> CreateRectangleSetTestCases ()
	{
		const int WIDTH = 4;
		const int HEIGHT = 4;

		RectangleI topLeftArea = new (0, 0, 2, 2);
		var topLeftAreaSequence = new[] { KeyValuePair.Create (topLeftArea, true) };
		Dictionary<PointI, bool> topLeftChecks = new () {
			[new (0, 0)] = true,
			[new (3, 0)] = false,
			[new (3, 3)] = false,
			[new (0, 3)] = false,
			[new (1, 1)] = true,
			[new (2, 2)] = false,
		};
		yield return new (WIDTH, HEIGHT, topLeftAreaSequence, topLeftChecks);

		RectangleI bottomRightArea = new (2, 2, 2, 2);
		var bottomRightAreaSequence = new[] { KeyValuePair.Create (bottomRightArea, true) };
		Dictionary<PointI, bool> bottomRightChecks = new () {
			[new (0, 0)] = false,
			[new (3, 0)] = false,
			[new (3, 3)] = true,
			[new (0, 3)] = false,
			[new (1, 1)] = false,
			[new (2, 2)] = true,
		};
		yield return new (WIDTH, HEIGHT, bottomRightAreaSequence, bottomRightChecks);
	}

	static readonly IReadOnlyList<TestCaseData> out_of_bounds_access_cases =
	[
		new (0, 0),
		new (0, 1),
		new (1, 1),
		new (1, 2),
		new (1, -1),
		new (1, int.MinValue),
		new (1, int.MinValue + 1),
		new (1, int.MaxValue),
		new (1, int.MaxValue - 1),
		new (2, 2),
	];

	static readonly IReadOnlyList<TestCaseData> within_bounds_access_cases =
	[
		new (DEFAULT_SIZE, DEFAULT_OFFSET),
		new (2, 1),
	];

	static readonly IReadOnlyList<TestCaseData> invalid_indexing =
	[
		// Negative indexing

		new (DEFAULT_WIDTH, DEFAULT_HEIGHT, -1, 0),
		new (DEFAULT_WIDTH, DEFAULT_HEIGHT, 0, -1),
		new (DEFAULT_WIDTH, DEFAULT_HEIGHT, -1, -1),

		// Invalid rows and columns

		new (DEFAULT_WIDTH, DEFAULT_HEIGHT, DEFAULT_WIDTH, 0),
		new (DEFAULT_WIDTH, DEFAULT_HEIGHT, 0, DEFAULT_HEIGHT),
		new (DEFAULT_WIDTH, DEFAULT_HEIGHT, DEFAULT_WIDTH, DEFAULT_HEIGHT),

		new (5, 8, 5, 7),
		new (5, 8, 4, 8),
		new (5, 8, 5, 8),

		new (5, 8, 6, 7),
		new (5, 8, 4, 9),
		new (5, 8, 6, 9),
	];
}
