using System;
using System.Runtime.CompilerServices;

namespace Pinta.Core;

partial class UserBlendOps
{
	[Serializable]
	public sealed class ColorDodgeBlendOp : UserBlendOp
	{
		public static string StaticName
			=> "ColorDodge";

		public override ColorBgra Apply (in ColorBgra bottom, in ColorBgra top)
			=> ApplyStatic (bottom, top);

		public static ColorBgra ApplyStatic (in ColorBgra bottom, in ColorBgra top)
		{
			// The color dodge blend mode brightens the bottom to reflect the top
			//
			// - The resulting color is never darker than the bottom color,
			//   each channel can only stay the same or become brighter.
			//   It can be darker than the top.
			// - Blending with black leaves the original color unchanged
			// - Blending with white results in white (if the bottom isn't black)

			if (top.A == 0) return bottom;
			if (bottom.A == 0) return top;

			return BlendOpHelper.ComputePremultiplied<ChannelBlend> (bottom, top);
		}

		private readonly struct ChannelBlend : BlendOpHelper.IChannelBlend
		{
			[MethodImpl (MethodImplOptions.AggressiveInlining)]
			public static int BlendChannel (int Cb, int Ca, int Ab, int Aa)
			{
				if (Cb == 0) return 0;
				if (Ca >= Aa) return Aa * Ab; // top is white
				return Math.Min (Aa * Ab, Cb * Aa * Aa / (Aa - Ca));
			}
		}
	}
}
