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
		var mask = new BitMask (desiredWidth, DEFAULT_HEIGHT);
		var coordinates = new PointI (indexToAccess, DEFAULT_HEIGHT_INDEX);
		Assert.Throws<ArgumentOutOfRangeException> (() => _ = mask[indexToAccess, DEFAULT_HEIGHT_INDEX]);
		Assert.Throws<ArgumentOutOfRangeException> (() => _ = mask[coordinates]);
	}

	[TestCaseSource (nameof (within_bounds_access_cases))]
	public void WidthAccessWithinBoundsSucceeds (int desiredWidth, int indexToAccess)
	{
		var mask = new BitMask (desiredWidth, DEFAULT_HEIGHT);
		var coordinates = new PointI (indexToAccess, DEFAULT_HEIGHT_INDEX);
		Assert.DoesNotThrow (() => _ = mask[indexToAccess, DEFAULT_HEIGHT_INDEX]);
		Assert.DoesNotThrow (() => _ = mask[coordinates]);
	}

	[TestCaseSource (nameof (out_of_bounds_access_cases))]
	public void HeightAccessOutOfBoundsFails (int desiredHeight, int indexToAccess)
	{
		var mask = new BitMask (DEFAULT_WIDTH, desiredHeight);
		var coordinates = new PointI (DEFAULT_WIDTH_INDEX, indexToAccess);
		Assert.Throws<ArgumentOutOfRangeException> (() => _ = mask[DEFAULT_WIDTH_INDEX, indexToAccess]);
		Assert.Throws<ArgumentOutOfRangeException> (() => _ = mask[coordinates]);
	}

	[TestCaseSource (nameof (within_bounds_access_cases))]
	public void HeightAccessWithinBoundsSucceeds (int desiredHeight, int indexToAccess)
	{
		var mask = new BitMask (DEFAULT_WIDTH, desiredHeight);
		var coordinates = new PointI (DEFAULT_WIDTH_INDEX, indexToAccess);
		Assert.DoesNotThrow (() => _ = mask[DEFAULT_WIDTH_INDEX, indexToAccess]);
		Assert.DoesNotThrow (() => _ = mask[coordinates]);
	}

	[TestCase (DEFAULT_WIDTH, DEFAULT_HEIGHT, DEFAULT_WIDTH_INDEX, DEFAULT_HEIGHT_INDEX)]
	public void BitInitializedToFalse (int maskWidth, int maskHeight, int bitToTestX, int bitToTestY)
	{
		var mask = new BitMask (maskWidth, maskHeight);
		var bit = mask[bitToTestX, bitToTestY];
		Assert.IsFalse (bit);
	}

	[TestCase (DEFAULT_WIDTH, DEFAULT_HEIGHT, DEFAULT_WIDTH_INDEX, DEFAULT_HEIGHT_INDEX)]
	public void BitInvertsWithXY (int maskWidth, int maskHeight, int bitToInvertX, int bitToInvertY)
	{
		var mask = new BitMask (maskWidth, maskHeight);
		mask.Invert (bitToInvertX, bitToInvertY);
		var bit = mask[bitToInvertX, bitToInvertY];
		Assert.IsTrue (bit);
	}

	[TestCase (DEFAULT_WIDTH, DEFAULT_HEIGHT, DEFAULT_WIDTH_INDEX, DEFAULT_HEIGHT_INDEX, new[] { true, false, true, false })]
	public void BitGetsSetXY (int maskWidth, int maskHeight, int bitToSetX, int bitToSetY, bool[] valuesToSetAndTest)
	{
		var mask = new BitMask (maskWidth, maskHeight);
		var coordinates = new PointI (bitToSetX, bitToSetY);
		foreach (var value in valuesToSetAndTest) {
			mask.Set (bitToSetX, bitToSetY, value);
			Assert.AreEqual (value, mask[bitToSetX, bitToSetY]);
			Assert.AreEqual (value, mask[coordinates]);
		}
	}

	[TestCaseSource (nameof (invalid_indexing))]
	public void RejectsInvalidIndexing_PairIndexer (int width, int height, int x, int y)
	{
		var bitmask = new BitMask (width, height);
		Assert.Throws<ArgumentOutOfRangeException> (() => _ = bitmask[x, y]);
	}

	[TestCaseSource (nameof (invalid_indexing))]
	public void RejectsInvalidIndexing_PointIndexer (int width, int height, int x, int y)
	{
		var bitmask = new BitMask (width, height);
		var point = new PointI (x, y);
		Assert.Throws<ArgumentOutOfRangeException> (() => _ = bitmask[point]);
	}

	[TestCaseSource (nameof (invalid_indexing))]
	public void RejectsInvalidIndexing_GetMethod (int width, int height, int x, int y)
	{
		var bitmask = new BitMask (width, height);
		Assert.Throws<ArgumentOutOfRangeException> (() => _ = bitmask.Get (x, y));
	}

	[TestCaseSource (nameof (invalid_indexing))]
	public void RejectsInvalidIndexing_Invert (int width, int height, int x, int y)
	{
		var bitmask = new BitMask (width, height);
		Assert.Throws<ArgumentOutOfRangeException> (() => bitmask.Invert (x, y));
	}

	[TestCaseSource (nameof (invalid_indexing))]
	public void RejectsInvalidIndexing_SetPair (int width, int height, int x, int y)
	{
		var bitmask1 = new BitMask (width, height);
		var bitmask2 = new BitMask (width, height);
		Assert.Throws<ArgumentOutOfRangeException> (() => bitmask1.Set (x, y, true));
		Assert.Throws<ArgumentOutOfRangeException> (() => bitmask2.Set (x, y, false));
	}

	[TestCaseSource (nameof (rectangle_set_test_cases))]
	public void RectangleSetCorrectly (int width, int height, IEnumerable<KeyValuePair<RectangleI, bool>> areasToSet, IReadOnlyDictionary<PointI, bool> checks)
	{
		var bitmask = new BitMask (width, height);
		foreach (var kvp in areasToSet) {
			bitmask.Set (kvp.Key, kvp.Value);
		}
		foreach (var kvp in checks) {
			Assert.AreEqual (bitmask[kvp.Key], kvp.Value);
			Assert.AreEqual (bitmask[kvp.Key.X, kvp.Key.Y], kvp.Value);
		}
	}

	[TestCaseSource (nameof (scanline_invert_test_cases))]
	public void ScanlineInvertedCorrectly (int width, int height, IEnumerable<Scanline> scanlineInversionSequence, IReadOnlyDictionary<PointI, bool> checks)
	{
		var bitmask = new BitMask (width, height);
		foreach (var scanline in scanlineInversionSequence) {
			bitmask.Invert (scanline);
		}
		foreach (var kvp in checks) {
			Assert.AreEqual (bitmask[kvp.Key], kvp.Value);
			Assert.AreEqual (bitmask[kvp.Key.X, kvp.Key.Y], kvp.Value);
		}
	}

	static readonly IReadOnlyList<TestCaseData> scanline_invert_test_cases = CreateScanlineInvertTestCases ().ToArray ();
	static IEnumerable<TestCaseData> CreateScanlineInvertTestCases ()
	{
		const int WIDTH = 16;
		const int HEIGHT = 16;

		Scanline topLeftLine = new (0, 0, 4);

		var singleTopLeftSequence = new[] { topLeftLine };
		var singleTopLeftChangedChecks = new Dictionary<PointI, bool> {
			[new (0, 0)] = true,
			[new (3, 0)] = true,
		};
		var singleTopLeftOutOfRangeChecks = new Dictionary<PointI, bool> {
			[new (4, 0)] = false,
			[new (0, 1)] = false,
		};
		yield return new TestCaseData (WIDTH, HEIGHT, singleTopLeftSequence, singleTopLeftChangedChecks);
		yield return new TestCaseData (WIDTH, HEIGHT, singleTopLeftSequence, singleTopLeftOutOfRangeChecks);

		var doubleTopLeftSequence = Enumerable.Repeat (topLeftLine, 2);
		var doubleTopLeftChecks = singleTopLeftChangedChecks.ToDictionary (kvp => kvp.Key, kvp => !kvp.Value);
		yield return new TestCaseData (WIDTH, HEIGHT, doubleTopLeftSequence, doubleTopLeftChecks);
		yield return new TestCaseData (WIDTH, HEIGHT, doubleTopLeftSequence, singleTopLeftOutOfRangeChecks);

		var singlePixelSequence = new[] { new Scanline (DEFAULT_WIDTH_INDEX, DEFAULT_HEIGHT_INDEX, 1) };
		var singlePixelChecks = new Dictionary<PointI, bool> { [new (DEFAULT_WIDTH_INDEX, DEFAULT_HEIGHT_INDEX)] = true };
		yield return new TestCaseData (DEFAULT_WIDTH, DEFAULT_HEIGHT, singlePixelSequence, singlePixelChecks);
	}

	static readonly IReadOnlyList<TestCaseData> rectangle_set_test_cases = CreateRectangleSetTestCases ().ToArray ();
	static IEnumerable<TestCaseData> CreateRectangleSetTestCases ()
	{
		const int WIDTH = 4;
		const int HEIGHT = 4;

		RectangleI topLeftArea = new (0, 0, 2, 2);
		var topLeftAreaSequence = new[] { KeyValuePair.Create (topLeftArea, true) };
		var topLeftChecks = new Dictionary<PointI, bool> {
			[new (0, 0)] = true,
			[new (3, 0)] = false,
			[new (3, 3)] = false,
			[new (0, 3)] = false,
			[new (1, 1)] = true,
			[new (2, 2)] = false,
		};
		yield return new TestCaseData (WIDTH, HEIGHT, topLeftAreaSequence, topLeftChecks);

		RectangleI bottomRightArea = new (2, 2, 2, 2);
		var bottomRightAreaSequence = new[] { KeyValuePair.Create (bottomRightArea, true) };
		var bottomRightChecks = new Dictionary<PointI, bool> {
			[new (0, 0)] = false,
			[new (3, 0)] = false,
			[new (3, 3)] = true,
			[new (0, 3)] = false,
			[new (1, 1)] = false,
			[new (2, 2)] = true,
		};
		yield return new TestCaseData (WIDTH, HEIGHT, bottomRightAreaSequence, bottomRightChecks);
	}

	static readonly IReadOnlyList<TestCaseData> out_of_bounds_access_cases = new TestCaseData[]
	{
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
	};

	static readonly IReadOnlyList<TestCaseData> within_bounds_access_cases = new TestCaseData[]
	{
		new (DEFAULT_SIZE, DEFAULT_OFFSET),
		new (2, 1),
	};

	static readonly IReadOnlyList<TestCaseData> invalid_indexing = new TestCaseData[]
	{
		new (DEFAULT_WIDTH, DEFAULT_HEIGHT, -1, 0),
		new (DEFAULT_WIDTH, DEFAULT_HEIGHT, 0, -1),
		new (DEFAULT_WIDTH, DEFAULT_HEIGHT, -1, -1),
	};
}
