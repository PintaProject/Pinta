using System;

namespace Pinta.Core;

public readonly struct RandomSeed
{
	public readonly int Value { get; }
	public RandomSeed (int value)
	{
		if (value < 0) throw new ArgumentOutOfRangeException (nameof (value));
		if (value == int.MaxValue) throw new ArgumentOutOfRangeException (nameof (value));
		Value = value;
	}
	public override readonly bool Equals (object? obj)
	{
		if (obj is not RandomSeed other) return false;
		return Value == other.Value;
	}
	public override readonly int GetHashCode () => Value.GetHashCode ();
	public static bool operator == (RandomSeed left, RandomSeed right) => left.Equals (right);
	public static bool operator != (RandomSeed left, RandomSeed right) => !left.Equals (right);
}
