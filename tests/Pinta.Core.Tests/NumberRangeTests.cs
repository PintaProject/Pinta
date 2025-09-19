using System;
using System.Collections.Immutable;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Pinta.Core.Tests;

[TestFixture]
public sealed class NumberRangeTests
{
	[Test]
	public void Creation_Throws_On_Invalid (
		[ValueSource (nameof (valid_doubles))] double valid,
		[ValueSource (nameof (invalid_doubles))] double invalid)
	{
		using var _ = Assert.EnterMultipleScope ();
		Assert.Throws<ArgumentOutOfRangeException> (() => NumberRange.Create (valid, invalid));
		Assert.Throws<ArgumentOutOfRangeException> (() => new NumberRange<double> (valid, invalid));
		Assert.Throws<ArgumentOutOfRangeException> (() => NumberRange.Create (invalid, valid));
		Assert.Throws<ArgumentOutOfRangeException> (() => new NumberRange<double> (invalid, valid));
		Assert.Throws<ArgumentOutOfRangeException> (() => NumberRange.Create (invalid, invalid));
		Assert.Throws<ArgumentOutOfRangeException> (() => new NumberRange<double> (invalid, invalid));
	}

	[Test]
	public void Creation_Throws_On_Inconsistent_Bounds (
		[ValueSource (nameof (valid_doubles))] double lower,
		[ValueSource (nameof (valid_doubles))] double upper)
	{
		if (lower <= upper) {
			Assert.Pass ($"Case does not apply, because {lower} <= {upper}");
			return;
		}
		using var _ = Assert.EnterMultipleScope ();
		Assert.Throws<ArgumentException> (() => NumberRange.Create (lower, upper));
		Assert.Throws<ArgumentException> (() => new NumberRange<double> (lower, upper));
	}

	[Test]
	public void Creation_Accepts_Consistent_Bounds (
		[ValueSource (nameof (valid_doubles))] double lower,
		[ValueSource (nameof (valid_doubles))] double upper)
	{
		if (lower > upper) {
			Assert.Pass ($"Case does not apply, because {lower} > {upper}");
			return;
		}
		using var _ = Assert.EnterMultipleScope ();
		Assert.DoesNotThrow (() => NumberRange.Create (lower, upper));
		Assert.DoesNotThrow (() => new NumberRange<double> (lower, upper));
	}

	[Test]
	[TestCase (5.5, 10.5)]
	public void Properties_Are_Set_Correctly (double lower, double upper)
	{
		NumberRange<double> range = new (lower, upper);
		using var _ = Assert.EnterMultipleScope ();
		Assert.That (range.Lower, Is.EqualTo (lower));
		Assert.That (range.Upper, Is.EqualTo (upper));
	}

	[Test]
	[TestCase (10, 20)]
	public void Equality (double lower, double upper)
	{
		NumberRange<double> a = new (lower, upper);
		NumberRange<double> b = new (lower, upper);
		using var _ = Assert.EnterMultipleScope ();
		Assert.That (a.Equals (b), Is.True);
		Assert.That (b.Equals (a), Is.True);
		Assert.That (a.Equals ((object) b), Is.True);
		Assert.That (b.Equals ((object) a), Is.True);
		Assert.That (a.Equals (null), Is.False);
		Assert.That (b.Equals (null), Is.False);
		Assert.That (a == b, Is.True);
		Assert.That (b == a, Is.True);
		Assert.That (a != b, Is.False);
		Assert.That (b != a, Is.False);
		Assert.That (a.GetHashCode (), Is.EqualTo (b.GetHashCode ()));
	}

	[Test]
	[TestCase (10, 20, 30, 40)]
	public void Inequality (double lowerA, double upperA, double lowerB, double upperB)
	{
		NumberRange<double> a = new (lowerA, upperA);
		NumberRange<double> b = new (lowerB, upperB);
		using var _ = Assert.EnterMultipleScope ();
		Assert.That (a.Equals (b), Is.False);
		Assert.That (b.Equals (a), Is.False);
		Assert.That (a.Equals ((object) b), Is.False);
		Assert.That (b.Equals ((object) a), Is.False);
		Assert.That (a.Equals (null), Is.False);
		Assert.That (b.Equals (null), Is.False);
		Assert.That (a == b, Is.False);
		Assert.That (b == a, Is.False);
		Assert.That (a != b, Is.True);
		Assert.That (b != a, Is.True);
	}


	private static readonly ImmutableArray<double> valid_doubles = [

		double.MinValue

		-double.Tau,
		-double.Pi,
		-double.E,
		-2,
		-1,
		-double.Epsilon,
		double.NegativeZero,
		0,
		double.Epsilon,
		1,
		2,
		double.E,
		double.Pi,
		double.Tau,

		double.MaxValue
	];

	private static readonly ImmutableArray<double> invalid_doubles =
		[double.NegativeInfinity, double.PositiveInfinity, double.NaN];
}
