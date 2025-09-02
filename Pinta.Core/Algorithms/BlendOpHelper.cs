using System.Runtime.CompilerServices;

namespace Pinta.Core;

internal static class BlendOpHelper
{
	// A 'separable' blend mode acts on each color channel independently
	// 
	// The general formula for a separable blend mode **with premultiplied alpha** is:
	// 
	// C_out = (1 - A_b) * C_a
	//       + (1 - A_a) * C_b
	//       + Blend(C_a, C_b)
	// 
	// Where:
	// 
	// - C refers to the premultiplied color channels (R, G, B)
	// - A refers to the alpha channel
	// - a refers to the top layer color (rhs)
	// - b refers to the bottom layer color (lhs)
	// 
	// This helper is meant for blend ops that use bytes and integer arithmetic for efficiency.
	// These calculations achieve similar results to operations with channels ranging from 0 to 1,
	// except that these channels are being represented by bytes. That is, ranging from 0 to 255.
	// 
	// This is achieved by scaling the calculations by a factor of 255 with respect to their
	// "theoretical" counterparts, and then scaling everything back (see the `ROUNDING_ADDEND`
	// constant, which is a neat trick for using the truncation operator for rounding).

	public readonly struct PremultipliedSeparable (in ColorBgra bottom, in ColorBgra top)
	{
		public int InverseTopAlpha { get; } = 255 - top.A;
		public int InverseBottomAlpha { get; } = 255 - bottom.A;
	}

	[MethodImpl (MethodImplOptions.AggressiveInlining)]
	public static PremultipliedSeparable PrepareValues (in ColorBgra bottom, in ColorBgra top)
		=> new (bottom, top);


	[MethodImpl (MethodImplOptions.AggressiveInlining)]
	public static ColorBgra Combine (
		in PremultipliedSeparable values,
		in ColorBgra bottom,
		in ColorBgra top,
		int blendedB,
		int blendedG,
		int blendedR)
	{
		int topContributionB = values.InverseBottomAlpha * top.B;
		int topContributionG = values.InverseBottomAlpha * top.G;
		int topContributionR = values.InverseBottomAlpha * top.R;

		int bottomContributionB = values.InverseTopAlpha * bottom.B;
		int bottomContributionG = values.InverseTopAlpha * bottom.G;
		int bottomContributionR = values.InverseTopAlpha * bottom.R;

		int preRoundingB = topContributionB + bottomContributionB + blendedB;
		int preRoundingG = topContributionG + bottomContributionG + blendedG;
		int preRoundingR = topContributionR + bottomContributionR + blendedR;

		const int ROUNDING_ADDEND = 128;

		byte outB = Utility.ClampToByte ((preRoundingB + ROUNDING_ADDEND) / 255);
		byte outG = Utility.ClampToByte ((preRoundingG + ROUNDING_ADDEND) / 255);
		byte outR = Utility.ClampToByte ((preRoundingR + ROUNDING_ADDEND) / 255);

		byte outA = Utility.ClampToByte (top.A + (bottom.A * values.InverseTopAlpha + ROUNDING_ADDEND) / 255);

		return ColorBgra.FromBgra (outB, outG, outR, outA);
	}
}
