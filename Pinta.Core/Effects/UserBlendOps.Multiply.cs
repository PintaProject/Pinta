using System;

namespace Pinta.Core;

partial class UserBlendOps
{
	[Serializable]
	public sealed class MultiplyBlendOp : UserBlendOp
	{
		public static string StaticName
			=> "Multiply";

		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs)
			=> ApplyStatic (lhs, rhs);

		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs)
		{
			// This blend mode multiplies the color channels of the base and blend layers.
			// 
			// Think of what happens when the channels are normalized to be in a range from 0 to 1:
			// 
			// - The resulting color is always at least as dark as either of the original colors.
			// - Multiplying any color with black results in black.
			// - Multiplying any color with white leaves the original color unchanged.
			// 
			// This implementation uses integer arithmetic for efficiency.
			// It can achieve this by scaling the entire calculation by a factor of 255
			// 
			// This is a separable blend mode.
			// 
			// A 'separable' blend mode acts on each color channel independently.
			// The formula for a separable blend mode with premultiplied alpha is:
			// 
			// C_out = (1 - A_b) * C_a
			//       + (1 - A_a) * C_b
			//       + Blend(C_a, C_b)
			// 
			// For the Multiply blend mode:
			// 
			// Blend(C_a, C_b) = C_a * C_b.
			// 
			// Where:
			// 
			// - C refers to the premultiplied color channels (R, G, B)
			// - A refers to the alpha channel
			// - a refers to the top layer color (rhs)
			// - b refers to the bottom layer color (lhs)
			// 
			// The 'ROUNDING_ADDEND' mechanism is a neat trick that
			// forces the truncation operator to function as a rounding operator.

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

			int blendR = rhs.R * lhs.R;
			int blendG = rhs.G * lhs.G;
			int blendB = rhs.B * lhs.B;

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
