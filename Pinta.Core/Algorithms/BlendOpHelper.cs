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

	public interface IChannelBlend
	{
		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		static abstract int BlendChannel (int Cb, int Ca, int Ab, int Aa);
	}

	[MethodImpl (MethodImplOptions.AggressiveInlining)]
	public static ColorBgra ComputePremultiplied<TChannelBlend> (in ColorBgra bottom, in ColorBgra top)
		where TChannelBlend : IChannelBlend
	{
		int inverseTopAlpha = 255 - top.A;
		int inverseBottomAlpha = 255 - bottom.A;

		int topContributionB = inverseBottomAlpha * top.B;
		int topContributionG = inverseBottomAlpha * top.G;
		int topContributionR = inverseBottomAlpha * top.R;

		int bottomContributionB = inverseTopAlpha * bottom.B;
		int bottomContributionG = inverseTopAlpha * bottom.G;
		int bottomContributionR = inverseTopAlpha * bottom.R;

		int blendedB = TChannelBlend.BlendChannel (bottom.B, top.B, bottom.A, top.A);
		int blendedG = TChannelBlend.BlendChannel (bottom.G, top.G, bottom.A, top.A);
		int blendedR = TChannelBlend.BlendChannel (bottom.R, top.R, bottom.A, top.A);

		int preRoundingB = topContributionB + bottomContributionB + blendedB;
		int preRoundingG = topContributionG + bottomContributionG + blendedG;
		int preRoundingR = topContributionR + bottomContributionR + blendedR;

		const int ROUNDING_ADDEND = 128;

		byte outB = Utility.ClampToByte ((preRoundingB + ROUNDING_ADDEND) / 255);
		byte outG = Utility.ClampToByte ((preRoundingG + ROUNDING_ADDEND) / 255);
		byte outR = Utility.ClampToByte ((preRoundingR + ROUNDING_ADDEND) / 255);

		byte outA = Utility.ClampToByte (top.A + (bottom.A * inverseTopAlpha + ROUNDING_ADDEND) / 255);

		return ColorBgra.FromBgra (outB, outG, outR, outA);
	}
}
