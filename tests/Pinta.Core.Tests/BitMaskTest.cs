using System;
using System.Collections.Generic;
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

	[TestCase (DEFAULT_WIDTH, DEFAULT_HEIGHT, DEFAULT_WIDTH_INDEX, DEFAULT_HEIGHT_INDEX)]
	public void BitInvertsWithScanline (int maskWidth, int maskHeight, int bitToInvertX, int bitToInvertY)
	{
		var mask = new BitMask (maskWidth, maskHeight);
		var scanline = new Scanline (bitToInvertX, bitToInvertY, 1);
		mask.Invert (scanline);
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
}
