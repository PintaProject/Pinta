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
			// The mathematical formula for Screen blending is:
			//
			// B(C_a, C_b) = 1 - (1 - C_a) * (1 - C_b)
			//
			// which simplifies to:
			//
			// B(C_a, C_b) = C_a + C_b - (C_a * C_b)
			//
			// This is a 'separable' blend mode, meaning it is applied to each
			// color channel (R, G, B) independently.
			//
			// The general formula for a separable blend mode is:
			//
			// C_out = (1 - A_b) * C_a
			//       + (1 - A_a) * C_b
			//       + B(C_a, C_b)
			//
			// Where:
			//
			// - C refers to the non-premultiplied color channels (R, G, B)
			// - A refers to the alpha channel
			// - a refers to the top layer color (rhs)
			// - b refers to the bottom layer color (lhs)
			//
			// To implement this using integer arithmetic (0-255 range), the
			// entire expression is scaled by 255. The 'blend' part for Screen becomes:
			//
			// BlendInt(C_a, C_b) = (C_a * 255) + (C_b * 255) - (C_a * C_b)
			//
			// The whole calculation is then divided by 255 to scale it back.
			// The 'ROUNDING_ADDEND' is used to ensure proper rounding instead of truncation.

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

			int blendR = (rhs.R * 255) + (lhs.R * 255) - (rhs.R * lhs.R);
			int blendG = (rhs.G * 255) + (lhs.G * 255) - (rhs.G * lhs.G);
			int blendB = (rhs.B * 255) + (lhs.B * 255) - (rhs.B * lhs.B);

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
