using System;
using System.Runtime.CompilerServices;
using static Pinta.Core.BlendOpHelper;

namespace Pinta.Core;

partial class UserBlendOps
{
	[Serializable]
	public sealed class NormalBlendOp : UserBlendOp
	{
		public static string StaticName
			=> "Normal";

		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs)
			=> ApplyStatic (lhs, rhs);

		public static ColorBgra ApplyStatic (in ColorBgra bottom, in ColorBgra top)
		{
			if (top.A == 255) return top; // Top layer is fully opaque
			if (top.A == 0) return bottom; // Top layer is fully transparent

			PremultipliedSeparable values = PrepareValues (bottom, top);

			int blendB = BlendChannel (bottom.B, top.B, bottom.A, top.A);
			int blendG = BlendChannel (bottom.G, top.G, bottom.A, top.A);
			int blendR = BlendChannel (bottom.R, top.R, bottom.A, top.A);

			return Combine (values, bottom, top, blendB, blendG, blendR);
		}

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		private static int BlendChannel (int Cb, int Ca, int Ab, int Aa)
			=> Ab * Ca;
	}
}
