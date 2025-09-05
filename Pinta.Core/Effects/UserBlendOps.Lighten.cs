using System;
using System.Runtime.CompilerServices;

namespace Pinta.Core;

partial class UserBlendOps
{
	[Serializable]
	public sealed class LightenBlendOp : UserBlendOp
	{
		public static string StaticName
			=> "Lighten";

		public override ColorBgra Apply (in ColorBgra bottom, in ColorBgra top)
			=> ApplyStatic (bottom, top);

		public static ColorBgra ApplyStatic (in ColorBgra bottom, in ColorBgra top)
		{
			// The Lighten blend mode selects the lighter of
			// the top and bottom colors for each color channel.
			//
			// - The resulting color is always at least as light as either of the original colors.
			// - Blending any color with white results in white.
			// - Blending any color with black leaves the original color unchanged.

			if (top.A == 0) return bottom;
			if (bottom.A == 0) return top;

			return BlendOpHelper.ComputePremultiplied<ChannelBlend> (bottom, top);
		}

		private readonly struct ChannelBlend : BlendOpHelper.IChannelBlend
		{
			[MethodImpl (MethodImplOptions.AggressiveInlining)]
			public static int BlendChannel (int Cb, int Ca, int Ab, int Aa)
				=> Math.Max (Ab * Ca, Aa * Cb);
		}
	}
}
