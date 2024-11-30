using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class MathematicsTests
{
	[TestCase (207, 60, 3)] // Multi-step, common factor
	[TestCase (209, 78, 1)] // Multi-step, no common factor
	[TestCase (10, 5, 5)] // Prime and non-prime, common factor
	[TestCase (4, 2, 2)] // Prime and non-prime, common factor
	[TestCase (9, 5, 1)] // Prime and non-prime, no common factor
	[TestCase (9, 4, 1)] // Two small non-primes, no common factors
	[TestCase (5, 3, 1)] // Two primes
	[TestCase (5, 5, 5)] // Same prime
	[TestCase (5, 1, 1)] // Edge case
	[TestCase (4, 1, 1)] // Edge case
	[TestCase (1, 1, 1)] // Edge case
	public void EuclidGCD_ComputesValues (int a, int b, int expected)
	{
		int result1 = Mathematics.EuclidGCD (a, b);
		int result2 = Mathematics.EuclidGCD (b, a);
		Assert.That (result1, Is.EqualTo (expected));
		Assert.That (result2, Is.EqualTo (expected));
	}

	[TestCase (1, 0)]
	[TestCase (0, 0)]
	[TestCase (-1, 1)]
	[TestCase (-1, 0)]
	[TestCase (-1, -1)]
	public void EuclidGCD_RejectsInvalidInputs (int a, int b)
	{
		Assert.Throws<ArgumentOutOfRangeException> (() => Mathematics.EuclidGCD (a, b));
		Assert.Throws<ArgumentOutOfRangeException> (() => Mathematics.EuclidGCD (b, a));
	}

	[TestCaseSource (nameof (lerp_cases))]
	public void Lerp_ComputesValues (LerpCase values)
	{
		double result = Mathematics.Lerp (
			values.from,
			values.to,
			values.frac);

		Assert.That (result, Is.EqualTo (values.result).Within (1e-10));
	}

	[TestCaseSource (nameof (inv_lerp_cases))]
	public void InvLerp_ComputesValues (LerpCase values)
	{
		double inverse = Mathematics.InvLerp (
			values.from,
			values.to,
			values.result);

		Assert.That (inverse, Is.EqualTo (values.frac).Within (1e-10));
	}

	[TestCaseSource (nameof (one_way_lerp_cases))]
	public void InvLerp_RejectsSame (LerpCase values)
	{
		Assert.Throws<ArgumentException> (() => Mathematics.InvLerp (values.from, values.to, values.result));
	}

	[TestCaseSource (nameof (unequal_fraction_cases))]
	public void Fraction_LessThan (UnequalFractionCase fractions)
	{
		Assert.That (fractions.lesser < fractions.greater, "Comparison failed");
		Assert.That (!(fractions.lesser > fractions.greater), "Negated comparison failed");
	}

	[TestCaseSource (nameof (unequal_fraction_cases))]
	public void Fraction_GreaterThan (UnequalFractionCase fractions)
	{
		Assert.That (fractions.greater > fractions.lesser, "Comparison failed");
		Assert.That (!(fractions.greater < fractions.lesser), "Negated comparison failed");
	}

	[TestCaseSource (nameof (unequal_fraction_cases))]
	public void Fraction_Unequal (UnequalFractionCase fractions)
	{
		Assert.That (fractions.lesser != fractions.greater, "Inequality operator comparison failed");
		Assert.That (!(fractions.lesser == fractions.greater), "Equality operator comparison failed");
	}

	[TestCaseSource (nameof (equal_fraction_cases))]
	public void Fraction_Equal_Reduction (EqualFractionCase fractions)
	{
		Assert.That (fractions.lhs.Numerator == fractions.rhs.Numerator, "Numerators not equal");
		Assert.That (fractions.lhs.Denominator == fractions.rhs.Denominator, "Denominators not equal");
	}

	[TestCaseSource (nameof (equal_fraction_cases))]
	public void Fraction_Equal (EqualFractionCase fractions)
	{
		Assert.That (fractions.lhs == fractions.rhs, "Operator comparison failed");
		Assert.That (!(fractions.lhs != fractions.rhs), "Inequality operator comparison failed");
		Assert.That (fractions.lhs.GetHashCode () == fractions.rhs.GetHashCode (), "Hashes are not equal");
	}

	private static readonly IReadOnlyList<TestCaseData> lerp_cases =
		GenerateRegularLerpCases ()
		.Concat (GenerateOneWayLerpCases ())
		.Select (c => new TestCaseData (c))
		.ToArray ();

	private static readonly IReadOnlyList<TestCaseData> inv_lerp_cases =
		GenerateRegularLerpCases ()
		.Select (c => new TestCaseData (c))
		.ToArray ();

	private static readonly IReadOnlyList<TestCaseData> one_way_lerp_cases =
		GenerateOneWayLerpCases ()
		.Select (c => new TestCaseData (c))
		.ToArray ();

	public readonly record struct EqualFractionCase (
		Fraction<int> lhs,
		Fraction<int> rhs);

	private static readonly IReadOnlyList<TestCaseData> equal_fraction_cases =
		GenerateEqualFractions ()
		.Select (c => new TestCaseData (c))
		.ToArray ();

	private static IEnumerable<EqualFractionCase> GenerateEqualFractions ()
	{
		yield return new (lhs: new (1, 3), rhs: new (2, 6));
		yield return new (lhs: new (1, 2), rhs: new (2, 4));
		yield return new (lhs: new (1, 1), rhs: new (1, 1));
		yield return new (lhs: new (1, 1), rhs: new (2, 2));
		yield return new (lhs: new (2, 3), rhs: new (4, 6));
	}

	public readonly record struct UnequalFractionCase (
		Fraction<int> lesser,
		Fraction<int> greater);

	private static readonly IReadOnlyList<TestCaseData> unequal_fraction_cases =
		GenerateUnequalFractions ()
		.Select (c => new TestCaseData (c))
		.ToArray ();

	private static IEnumerable<UnequalFractionCase> GenerateUnequalFractions ()
	{
		yield return new (lesser: new (1, 2), greater: new (1, 1));
		yield return new (lesser: new (1, 1), greater: new (2, 1));
		yield return new (lesser: new (2, 3), greater: new (2, 1));
		yield return new (lesser: new (3, 2), greater: new (2, 1));
		yield return new (lesser: new (2, 1), greater: new (5, 2));
		yield return new (lesser: new (5, 2), greater: new (3, 1));
	}

	public readonly record struct LerpCase (
		double from,
		double to,
		double frac,
		double result);

	// These should succeed (in their own way) in both Lerp and InvLerp
	private static IEnumerable<LerpCase> GenerateRegularLerpCases ()
	{
		yield return new (0.0, 10.0, 0.0, 0.0);
		yield return new (0.0, 10.0, 1.0, 10.0);
		yield return new (0.0, 10.0, 1.5, 15.0);
		yield return new (0.0, 10.0, 2.0, 20.0);
		yield return new (0.0, 10.0, -1.0, -10.0);
		yield return new (1.0e10, 2.0e10, 0.5, 1.5e10);
		yield return new (1.0e-5, 2.0e-5, 0.5, 1.5e-5);
		yield return new (1.0e10, 2.0e10, -1.0, 0);
		yield return new (1.0e-5, 2.0e-5, -1.0, 0);
	}

	// Regular cases that throw if they are inverted
	private static IEnumerable<LerpCase> GenerateOneWayLerpCases ()
	{
		yield return new (3.0, 3.0, 10000.0, 3.0);
	}

	// TODO: Add cases like NaN and infinity}
}
