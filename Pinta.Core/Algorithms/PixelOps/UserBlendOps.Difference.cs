using System;
using System.Runtime.CompilerServices;

namespace Pinta.Core;

partial class UserBlendOps
{
	[Serializable]
	public sealed class DifferenceBlendOp : UserBlendOp
	{
		public static string StaticName
			=> "Difference";

		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs)
			=> ApplyStatic (lhs, rhs);

		public static ColorBgra ApplyStatic (in ColorBgra bottom, in ColorBgra top)
		{
			// This blend more subtracts the darker of the two colors from the lighter color
			// 
			// - Since all the components of black are zero, if one of the colors is black there is no change
			// - Since all the components of white are 255, the channels of the other color are "inverted" in a way

			return BlendOpHelper.ComputePremultiplied<ChannelBlend> (bottom, top);
		}

		private readonly struct ChannelBlend : BlendOpHelper.IChannelBlend
		{
			[MethodImpl (MethodImplOptions.AggressiveInlining)]
			public static int BlendChannel (int Cb, int Ca, int Ab, int Aa)
				=> Math.Abs (Cb * Aa - Ca * Ab);
		}
	}
}
