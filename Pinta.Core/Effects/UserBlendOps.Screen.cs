using System;
using System.Runtime.CompilerServices;
using static Pinta.Core.BlendOpHelper;

namespace Pinta.Core;

partial class UserBlendOps
{
	[Serializable]
	public sealed class ScreenBlendOp : UserBlendOp
	{
		public static string StaticName
			=> "Screen";

		public override ColorBgra Apply (in ColorBgra bottom, in ColorBgra top)
			=> ApplyStatic (bottom, top);

		public static ColorBgra ApplyStatic (in ColorBgra bottom, in ColorBgra top)
		{
			// The Screen blend mode is the inverse of the Multiply blend mode.
			// 
			// It results in a lighter color, akin to projecting multiple images
			// onto the same screen:
			//
			// - Screening any color with black leaves the original color unchanged.
			// - Screening any color with white results in white.

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
			=> Aa * Cb + Ab * Ca - Ca * Cb;
	}
}
