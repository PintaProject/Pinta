/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace Pinta.Core;

partial class UserBlendOps
{
	[Serializable]
	public sealed class XorBlendOp : UserBlendOp
	{
		public static string StaticName => "Xor";

		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs)
			=> ApplyStatic (lhs, rhs);

		public override void Apply (Span<ColorBgra> dst, ReadOnlySpan<ColorBgra> src)
		{
			for (int i = 0; i < src.Length; ++i)
				dst[i] = ApplyStatic (dst[i], src[i]);
		}

		public override void Apply (Span<ColorBgra> dst, ReadOnlySpan<ColorBgra> lhs, ReadOnlySpan<ColorBgra> rhs)
		{
			for (int i = 0; i < dst.Length; ++i)
				dst[i] = ApplyStatic (lhs[i], rhs[i]);
		}

		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs)
		{
			int y;
			{
				y = lhs.A * (255 - rhs.A) + 0x80;
				y = ((y >> 8) + y) >> 8;
			}

			int totalA = y + rhs.A;

			if (totalA == 0) return ColorBgra.FromUInt32 (0);

			int fB = lhs.B ^ rhs.B;
			int fG = lhs.G ^ rhs.G;
			int fR = lhs.R ^ rhs.R;
			int x;
			{
				x = lhs.A * rhs.A + 0x80;
				x = ((x >> 8) + x) >> 8;
			}
			int z = rhs.A - x;
			int masIndex = totalA * 3;
			uint taM = mas_table[masIndex];
			uint taA = mas_table[masIndex + 1];
			uint taS = mas_table[masIndex + 2];
			uint b = (uint) (((((lhs.B * y) + (rhs.B * z) + (fB * x)) * taM) + taA) >> (int) taS);
			uint g = (uint) (((((lhs.G * y) + (rhs.G * z) + (fG * x)) * taM) + taA) >> (int) taS);
			uint r = (uint) (((((lhs.R * y) + (rhs.R * z) + (fR * x)) * taM) + taA) >> (int) taS);
			int a;
			{
				a = lhs.A * (255 - rhs.A) + 0x80;
				a = ((a >> 8) + a) >> 8;
				a += rhs.A;
			}
			return ColorBgra.FromUInt32 (b + (g << 8) + (r << 16) + ((uint) a << 24));
		}
	}
}
