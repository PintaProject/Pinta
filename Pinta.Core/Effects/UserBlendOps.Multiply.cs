using System;
using System.Runtime.CompilerServices;

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

			return BlendOpHelper.ComputePremultiplied<ChannelBlend> (bottom, top);
		}

		private readonly struct ChannelBlend : BlendOpHelper.IChannelBlend
		{
			[MethodImpl (MethodImplOptions.AggressiveInlining)]
			public static int BlendChannel (int Cb, int Ca, int Ab, int Aa)
				=> Ca * Cb;
		}
	}
}
