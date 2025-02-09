namespace Pinta.Core;

partial struct ColorBgra
{
	/// <summary>
	/// Compares two ColorBgra instance to determine if they are equal.
	/// </summary>
	public static bool operator == (ColorBgra lhs, ColorBgra rhs)
		=> lhs.Bgra == rhs.Bgra;

	/// <summary>
	/// Compares two ColorBgra instance to determine if they are not equal.
	/// </summary>
	public static bool operator != (ColorBgra lhs, ColorBgra rhs)
		=> lhs.Bgra != rhs.Bgra;

	/// <summary>
	/// Compares two ColorBgra instance to determine if they are equal.
	/// </summary>
	public override readonly bool Equals (object? obj)
		=> obj is ColorBgra bgra && bgra.Bgra == Bgra;

	/// <summary>
	/// Returns a hash code for this color value.
	/// </summary>
	/// <returns></returns>
	public override readonly int GetHashCode () { unchecked { return (int) Bgra; } }

	public override readonly string ToString ()
		=> $"B: {B}, G: {G}, R: {R}, A: {A}";

	/// <summary>
	/// Casts a ColorBgra to a UInt32.
	/// </summary>
	public static explicit operator uint (ColorBgra color)
		=> color.Bgra;

	/// <summary>
	/// Casts a UInt32 to a ColorBgra.
	/// </summary>
	public static explicit operator ColorBgra (uint uint32)
		=> FromUInt32 (uint32);
}
