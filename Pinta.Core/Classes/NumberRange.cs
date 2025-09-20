using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Pinta.Core;

public readonly struct NumberRange<TNumber> : IEquatable<NumberRange<TNumber>>
	where TNumber : struct, INumber<TNumber>
{
	public TNumber Lower { get; }
	public TNumber Upper { get; }

	public NumberRange (TNumber lower, TNumber upper)
	{
		if (!TNumber.IsFinite (lower)) throw new ArgumentOutOfRangeException (nameof (lower));
		if (!TNumber.IsFinite (upper)) throw new ArgumentOutOfRangeException (nameof (upper));
		if (lower > upper) throw new ArgumentException ($"{nameof (lower)} cannot be greater than {nameof (upper)}");
		Lower = lower;
		Upper = upper;
	}

	public static bool operator == (NumberRange<TNumber> a, NumberRange<TNumber> b)
		=> a.Equals (b);

	public static bool operator != (NumberRange<TNumber> a, NumberRange<TNumber> b)
		=> !a.Equals (b);

	public override bool Equals ([NotNullWhen (true)] object? @object)
		=> @object is NumberRange<TNumber> other && Equals (other);

	public bool Equals (NumberRange<TNumber> other)
		=> Lower == other.Lower && Upper == other.Upper;

	public override int GetHashCode ()
		=> HashCode.Combine (Lower, Upper);
}

public static class NumberRange
{
	public static NumberRange<TNumber> Create<TNumber> (TNumber lower, TNumber upper) where TNumber : struct, INumber<TNumber>
		=> new (lower, upper);
}
