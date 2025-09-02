using System;

namespace Pinta.Core;

partial class UserBlendOps
{
	[Serializable]
	public sealed class ScreenBlendOp : UserBlendOp
	{
		public static string StaticName
			=> "Screen";

		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs)
			=> ApplyStatic (lhs, rhs);

		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs)
		{
			// The Screen blend mode is the inverse of the Multiply blend mode.
			// It results in a lighter color, akin to projecting multiple images
			// onto the same screen:
			//
			// - Screening any color with black leaves the original color unchanged.
			// - Screening any color with white results in white.
			//
			// This is a 'separable' blend mode, meaning it is applied to each
			// color channel (R, G, B) independently.
			//
			// The general formula for a separable blend mode is:
			//
			// C_out = (1 - A_b) * C_a
			//       + (1 - A_a) * C_b
			//       + Blend(C_a, C_b)
			// 
			// The Blend term in this structure must be calculated as:
			//
			// This term is derived from the standard W3C compositing model. It applies the
			// Screen formula (which works on straight, non-premultiplied colors) to the
			// overlapping area of the layers and converts the result back into the
			// premultiplied color space, which simplifies to the expression below.
			// 
			// Blend(C_a, C_b) = A_a * C_b + A_b * C_a - C_a * C_b
			// 
			// Where:
			// 
			// - C refers to the premultiplied color channels (R, G, B)
			// - A refers to the alpha channel
			// - a refers to the top layer color (rhs)
			// - b refers to the bottom layer color (lhs)

			if (rhs.A == 0) return lhs;
			if (lhs.A == 0) return rhs;

			int inverseTopAlpha = 255 - rhs.A;
			int inverseBottomAlpha = 255 - lhs.A;

			int topContributionR = inverseBottomAlpha * rhs.R;
			int topContributionG = inverseBottomAlpha * rhs.G;
			int topContributionB = inverseBottomAlpha * rhs.B;

			int bottomContributionR = inverseTopAlpha * lhs.R;
			int bottomContributionG = inverseTopAlpha * lhs.G;
			int bottomContributionB = inverseTopAlpha * lhs.B;

			int blendR = rhs.A * lhs.R + lhs.A * rhs.R - lhs.R * rhs.R;
			int blendG = rhs.A * lhs.G + lhs.A * rhs.G - lhs.G * rhs.G;
			int blendB = rhs.A * lhs.B + lhs.A * rhs.B - lhs.B * rhs.B;

			int preRoundingR = topContributionR + bottomContributionR + blendR;
			int preRoundingG = topContributionG + bottomContributionG + blendG;
			int preRoundingB = topContributionB + bottomContributionB + blendB;

			const int ROUNDING_ADDEND = 128;

			byte outR = Utility.ClampToByte ((preRoundingR + ROUNDING_ADDEND) / 255);
			byte outG = Utility.ClampToByte ((preRoundingG + ROUNDING_ADDEND) / 255);
			byte outB = Utility.ClampToByte ((preRoundingB + ROUNDING_ADDEND) / 255);

			byte outA = Utility.ClampToByte (rhs.A + (lhs.A * inverseTopAlpha + ROUNDING_ADDEND) / 255);

			return ColorBgra.FromBgra (outB, outG, outR, outA);
		}
	}
}
