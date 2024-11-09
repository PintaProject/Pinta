using System;
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
		int result2 = Mathematics.EuclidGCD (a, b);
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
}
