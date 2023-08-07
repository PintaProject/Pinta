using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class ScanlineTest
{
	// Perhaps most (all?) of these tests can be removed if Scanline is
	// made into a record struct (a feature offered by C#10+)

	[TestCaseSource (nameof (sample_initializations))]
	public void MembersInitializingCorrectly (int x, int y, int length)
	{
		var scanline = new Scanline (
			x: x,
			y: y,
			length: length
		);
		Assert.AreEqual (scanline.X, x);
		Assert.AreEqual (scanline.Y, y);
		Assert.AreEqual (scanline.Length, length);
	}

	[TestCaseSource (nameof (sample_initializations))]
	public void EqualsOperator_TrueWithEqual (int x, int y, int length)
	{
		var scanline1 = new Scanline (x, y, length);
		var scanline2 = new Scanline (x, y, length);
		bool comparison = scanline1 == scanline2;
		Assert.IsTrue (comparison);
	}

	[TestCaseSource (nameof (sample_unequal))]
	public void EqualsOperator_FalseWithUnequal (int x1, int y1, int length1, int x2, int y2, int length2)
	{
		var scanline1 = new Scanline (x1, y1, length1);
		var scanline2 = new Scanline (x2, y2, length2);
		bool comparison = scanline1 == scanline2;
		Assert.IsFalse (comparison);
	}

	[TestCaseSource (nameof (sample_initializations))]
	public void NotEqualsOperator_FalseWithEqual (int x, int y, int length)
	{
		var scanline1 = new Scanline (x, y, length);
		var scanline2 = new Scanline (x, y, length);
		bool comparison = scanline1 != scanline2;
		Assert.IsFalse (comparison);
	}

	[TestCaseSource (nameof (sample_unequal))]
	public void NotEqualsOperator_TrueWithUnequal (int x1, int y1, int length1, int x2, int y2, int length2)
	{
		var scanline1 = new Scanline (x1, y1, length1);
		var scanline2 = new Scanline (x2, y2, length2);
		bool comparison = scanline1 != scanline2;
		Assert.IsTrue (comparison);
	}

	[TestCaseSource (nameof (overflowing_hash_code_cases))]
	public void HashCode_DoesNotOverflow (int x, int y, int length)
	{
		// At the time of writing this code:
		// - The hash of an int is the int itself.
		// - The hash of Scanline is the overflow-unchecked addition
		//   of the hash of all its members.
		// If any of the above changes, I recommend preserving the existing test cases
		// _and_ adding other test cases that may be of interest.
		var scanline = new Scanline (x, y, length);
		Assert.DoesNotThrow (() => _ = scanline.GetHashCode ());
	}

	static readonly IReadOnlyList<TestCaseData> sample_initializations = new TestCaseData[] {
		new (1, 2, 3),
		new (3, 1, 2),
		new (2, 3, 1),
	};

	static readonly IReadOnlyList<TestCaseData> sample_unequal = new TestCaseData[] {
		new (
			1, 1, 1,
			2, 2, 2),
		new (
			1, 1, 1,
			1, 1, 2),
		new (
			1, 1, 1,
			1, 2, 1),
		new (
			1, 1, 1,
			2, 1, 1),
	};

	static readonly IReadOnlyList<TestCaseData> overflowing_hash_code_cases = new TestCaseData[] {
		new (int.MaxValue, 1, 1),
		new (1, int.MaxValue, 1),
		new (1, 1, int.MaxValue),
		new (int.MaxValue / 2, int.MaxValue / 2, int.MaxValue / 2),
	};
}
