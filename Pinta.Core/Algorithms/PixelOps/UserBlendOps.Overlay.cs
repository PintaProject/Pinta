using System;
using System.Runtime.CompilerServices;

namespace Pinta.Core;

partial class UserBlendOps
{
	[Serializable]
	public sealed class OverlayBlendOp : UserBlendOp
	{
		public static string StaticName
			=> "Overlay";

		public override ColorBgra Apply (in ColorBgra bottom, in ColorBgra top)
			=> ApplyStatic (bottom, top);

		public static ColorBgra ApplyStatic (in ColorBgra bottom, in ColorBgra top)
		{
			// The overlay op multiplies where the bottom is dark and screens where it is light,
			// so it increases contrast in the direction of the top color.
			//
			// - Blending with 50% gray on top leaves the bottom unchanged
			// - Blending with black on top: dark bottoms become black, light bottoms are darkened
			// - Blending with white on top: light bottoms become white, dark bottoms are lightened
			// - The bottom decides which half of the formula applies (hard light with the layers swapped)

			if (top.A == 0) return bottom;
			if (bottom.A == 0) return top;

			return BlendOpHelper.ComputePremultiplied<ChannelBlend> (bottom, top);
		}

		private readonly struct ChannelBlend : BlendOpHelper.IChannelBlend
		{
			[MethodImpl (MethodImplOptions.AggressiveInlining)]
			public static int BlendChannel (int Cb, int Ca, int Ab, int Aa)
				=> 2 * Cb <= Ab
				? 2 * Cb * Ca
				: Aa * Ab - 2 * (Ab - Cb) * (Aa - Ca);
		}
	}
}
