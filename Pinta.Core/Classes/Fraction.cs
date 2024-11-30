using System;
using System.Numerics;

namespace Pinta.Core;

/// <summary>Represents a reduced fraction</summary>
/// <remarks>
/// Only positive values are supported for now.
/// At the time of this writing, support for negative
/// numbers is not needed.
/// </remarks>
public readonly struct Fraction<TInt> where TInt : IBinaryInteger<TInt>
{
	public TInt Numerator { get; }
	public TInt Denominator { get; }
	public Fraction (TInt numerator, TInt denominator)
	{
		if (denominator <= TInt.Zero) throw new ArgumentOutOfRangeException (nameof (denominator), "must be greater than 0(denominator = " + denominator + ")");
		if (numerator < TInt.Zero) throw new ArgumentOutOfRangeException (nameof (numerator), "must be greater than 0(numerator = " + numerator + ")");
		if (numerator == TInt.Zero) {
			Numerator = TInt.Zero;
			Denominator = TInt.One;
		} else {
			TInt gcd = Mathematics.EuclidGCD (numerator, denominator);
			TInt reducedNumerator = numerator / gcd;
			TInt reducedDenominator = denominator / gcd;
			Numerator = reducedNumerator;
			Denominator = reducedDenominator;
		}
	}

	public static bool operator < (Fraction<TInt> lhs, Fraction<TInt> rhs)
		=> (lhs.Numerator * rhs.Denominator) < (rhs.Numerator * lhs.Denominator);

	public static bool operator > (Fraction<TInt> lhs, Fraction<TInt> rhs)
		=> (lhs.Numerator * rhs.Denominator) > (rhs.Numerator * lhs.Denominator);
}

public static class FractionExtensions
{
	public static bool LessThan<TInt> (
		this in Fraction<TInt> lhs,
		TInt rhsNumerator,
		TInt rhsDenominator
	)
		where TInt : IBinaryInteger<TInt>
	{
		return (lhs.Numerator * rhsDenominator) < (lhs.Denominator * rhsNumerator);
	}

	public static bool GreaterThan<TInt> (
		this in Fraction<TInt> lhs,
		TInt rhsNumerator,
		TInt rhsDenominator
	)
		where TInt : IBinaryInteger<TInt>
	{
		return (lhs.Numerator * rhsDenominator) > (lhs.Denominator * rhsNumerator);
	}
}
