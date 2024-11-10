using System;
using System.Numerics;

namespace Pinta.Core;

public static class Mathematics
{
	/// <param name="a">First integer</param>
	/// <param name="b">Second integer</param>
	/// <returns>Greatest common divisor of arguments</returns>
	public static TInt EuclidGCD<TInt> (TInt a, TInt b) where TInt : IBinaryInteger<TInt>
	{
		if (a <= TInt.Zero) throw new ArgumentOutOfRangeException (nameof (a), "Number must be strictly positive");
		if (b <= TInt.Zero) throw new ArgumentOutOfRangeException (nameof (b), "Number must be strictly positive");
		while (a > TInt.Zero && b > TInt.Zero) {
			if (a > b)
				a %= b;
			else
				b %= a;
		}
		return TInt.Max (a, b);
	}

	public static TNumber Magnitude<TNumber> (TNumber x, TNumber y) where TNumber : IFloatingPoint<TNumber>, IRootFunctions<TNumber>
		=> TNumber.Sqrt (x * x + y * y);

	public static TNumber MagnitudeSquared<TNumber> (TNumber x, TNumber y) where TNumber : INumber<TNumber>
		=> x * x + y * y;

	/// <summary>Linear interpolation</summary>
	public static TNumber Lerp<TNumber> (
		TNumber from,
		TNumber to,
		TNumber frac
	) where TNumber : IFloatingPoint<TNumber>
		=> from + frac * (to - from);

	/// <summary>Inverse linear interpolation</summary>
	/// <exception cref="ArgumentException">Difference between upper and lower bounds is zero</exception>
	public static TNumber InvLerp<TNumber> (
		TNumber from,
		TNumber to,
		TNumber value) where TNumber : IFloatingPoint<TNumber>
	{
		TNumber valueSpan = to - from;
		if (valueSpan == TNumber.Zero)
			throw new ArgumentException ("Difference between upper and lower bounds cannot be zero", $"{nameof (from)}, {nameof (to)}");
		TNumber offset = value - from;
		return offset / valueSpan;
	}
}
