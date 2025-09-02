using System;
using System.Runtime.CompilerServices;
using static Pinta.Core.BlendOpHelper;

namespace Pinta.Core;

partial class UserBlendOps
{
	[Serializable]
	public sealed class MultiplyBlendOp : UserBlendOp
	{
		public static string StaticName
			=> "Multiply";

		public override ColorBgra Apply (in ColorBgra bottom, in ColorBgra top)
			=> ApplyStatic (bottom, top);

		public static ColorBgra ApplyStatic (in ColorBgra bottom, in ColorBgra top)
		{
			// This blend mode multiplies the color channels of the base and blend layers.
			// 
			// Think of what happens when the channels are normalized to be in a range from 0 to 1:
			// 
			// - The resulting color is always at least as dark as either of the original colors.
			// - Multiplying any color with black results in black.
			// - Multiplying any color with white leaves the original color unchanged.

			if (top.A == 0) return bottom;
			if (bottom.A == 0) return top;

			PremultipliedSeparable values = PrepareValues (bottom, top);

			int blendB = BlendChannel (bottom.B, top.B, bottom.A, top.A);
			int blendG = BlendChannel (bottom.G, top.G, bottom.A, top.A);
			int blendR = BlendChannel (bottom.R, top.R, bottom.A, top.A);

			return Combine (values, bottom, top, blendB, blendG, blendR);
		}

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		private static int BlendChannel (int Cb, int Ca, int Ab, int Aa)
			=> Ca * Cb;
	}
}
