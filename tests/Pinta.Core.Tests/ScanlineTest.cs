using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class ScanlineTest
{
	[Test]
	[TestCase (0, 0, -1)]
	public void Constructor_RejectsInvalidArguments (int x, int y, int length)
	{
		Assert.Throws<ArgumentOutOfRangeException> (() => _ = new Scanline (x, y, length));
	}

	[Test]
	[TestCaseSource (nameof (sample_initializations))]
	public void MembersInitializingCorrectly (int x, int y, int length)
	{
		Scanline scanline = new (
			x: x,
			y: y,
			length: length);
		using var _ = Assert.EnterMultipleScope ();
		Assert.That (x, Is.EqualTo (scanline.X));
		Assert.That (y, Is.EqualTo (scanline.Y));
		Assert.That (length, Is.EqualTo (scanline.Length));
	}

	[Test]
	[TestCaseSource (nameof (sample_initializations))]
	public void EqualsOperator_TrueWithEqual (int x, int y, int length)
	{
		Scanline scanline1 = new (x, y, length);
		Scanline scanline2 = new (x, y, length);
		using var _ = Assert.EnterMultipleScope ();
		Assert.That (scanline1 == scanline2, Is.True);
		Assert.That (scanline2 == scanline1, Is.True);
	}

	[Test]
	[TestCaseSource (nameof (sample_initializations))]
	public void HashCodes_Are_Same_For_Equal_Values (int x, int y, int length)
	{
		Scanline scanline1 = new (x, y, length);
		Scanline scanline2 = new (x, y, length);
		using var _ = Assert.EnterMultipleScope ();
		Assert.That (scanline1.GetHashCode (), Is.EqualTo (scanline2.GetHashCode ()));
		Assert.That (scanline2.GetHashCode (), Is.EqualTo (scanline1.GetHashCode ()));
	}

	[Test]
	[TestCaseSource (nameof (unequal_values))]
	public void EqualsOperator_FalseWithUnequal (int x1, int y1, int length1, int x2, int y2, int length2)
	{
		Scanline scanline1 = new (x1, y1, length1);
		Scanline scanline2 = new (x2, y2, length2);
		using var _ = Assert.EnterMultipleScope ();
		Assert.That (scanline1 == scanline2, Is.False);
		Assert.That (scanline2 == scanline1, Is.False);
	}

	[Test]
	[TestCaseSource (nameof (sample_initializations))]
	public void NotEqualsOperator_FalseWithEqual (int x, int y, int length)
	{
		Scanline scanline1 = new (x, y, length);
		Scanline scanline2 = new (x, y, length);
		using var _ = Assert.EnterMultipleScope ();
		Assert.That (scanline1 != scanline2, Is.False);
		Assert.That (scanline2 != scanline1, Is.False);
	}

	[Test]
	[TestCaseSource (nameof (unequal_values))]
	public void NotEqualsOperator_TrueWithUnequal (int x1, int y1, int length1, int x2, int y2, int length2)
	{
		Scanline scanline1 = new (x1, y1, length1);
		Scanline scanline2 = new (x2, y2, length2);
		using var _ = Assert.EnterMultipleScope ();
		Assert.That (scanline1 != scanline2, Is.True);
		Assert.That (scanline2 != scanline1, Is.True);
	}

	[Test]
	[TestCaseSource (nameof (sample_initializations))]
	public void EqualsMethod_TrueWithEqual (int x, int y, int length)
	{
		Scanline scanline1 = new (x, y, length);
		Scanline scanline2 = new (x, y, length);
		using var _ = Assert.EnterMultipleScope ();
		Assert.That (scanline1.Equals (scanline2), Is.True);
		Assert.That (scanline2.Equals (scanline1), Is.True);
	}

	[Test]
	public void EqualsMethod_FalseWithUnequalTypes ([ValueSource (nameof (unrelated_objects))] object? other)
	{
		Scanline scanline = new (1, 1, 1);
		bool comparison = scanline.Equals (other);
		Assert.That (comparison, Is.False);
	}

	[Test]
	[TestCaseSource (nameof (unequal_values))]
	public void EqualsMethod_FalseWithUnequalValues (int x1, int y1, int length1, int x2, int y2, int length2)
	{
		Scanline scanline1 = new (x1, y1, length1);
		Scanline scanline2 = new (x2, y2, length2);
		using var _ = Assert.EnterMultipleScope ();
		Assert.That (scanline1.Equals (scanline2), Is.False);
		Assert.That (scanline2.Equals (scanline1), Is.False);
	}

	static readonly IReadOnlyList<object?> unrelated_objects = [
		"This is not a scanline",
		null,
		new[] { 1, 1, 1 },
		111,
	];

	static readonly IReadOnlyList<TestCaseData> sample_initializations = [
		new (1, 2, 3),
		new (3, 1, 2),
		new (2, 3, 1),
	];

	static readonly IReadOnlyList<TestCaseData> unequal_values = [
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
		new (
			2, 2, 2,
			1, 1, 1),
		new (
			1, 1, 2,
			1, 1, 1),
		new (
			1, 2, 1,
			1, 1, 1),
		new (
			2, 1, 1,
			1, 1, 1),
	];
}
