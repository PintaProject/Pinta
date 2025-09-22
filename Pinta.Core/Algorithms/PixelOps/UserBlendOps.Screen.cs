using System;
using System.Runtime.CompilerServices;

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

			return BlendOpHelper.ComputePremultiplied<ChannelBlend> (bottom, top);
		}

		private readonly struct ChannelBlend : BlendOpHelper.IChannelBlend
		{
			[MethodImpl (MethodImplOptions.AggressiveInlining)]
			public static int BlendChannel (int Cb, int Ca, int Ab, int Aa)
				=> Aa * Cb + Ab * Ca - Ca * Cb;
		}
	}
}
