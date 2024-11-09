using System;
using System.Numerics;

namespace Pinta.Core;

public static class Mathematics
{
	/// <param name="a">First integer</param>
	/// <param name="b">Second integer</param>
	/// <returns>Greatest common denominator of arguments</returns>
	public static TInt EuclidGCD<TInt> (TInt a, TInt b) where TInt : IBinaryInteger<TInt>
	{
		if (a <= TInt.Zero) throw new ArgumentOutOfRangeException (nameof (b), "Number must be strictly positive");
		if (b <= TInt.Zero) throw new ArgumentOutOfRangeException (nameof (b), "Number must be strictly positive");
		while (a > TInt.Zero && b > TInt.Zero) {
			if (a > b)
				a %= b;
			else
				b %= a;
		}
		return TInt.Max (a, b);
	}
}
