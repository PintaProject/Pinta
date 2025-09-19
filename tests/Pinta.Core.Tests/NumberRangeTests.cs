using System;
using System.Collections.Immutable;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
public sealed class NumberRangeTests
{
	[Test]
	public void Creation_Throws_On_Invalid (
		[ValueSource (nameof (valid_doubles))] double valid,
		[ValueSource (nameof (invalid_doubles))] double invalid)
	{
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

		Assert.DoesNotThrow (() => NumberRange.Create (lower, upper));
		Assert.DoesNotThrow (() => new NumberRange<double> (lower, upper));
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
