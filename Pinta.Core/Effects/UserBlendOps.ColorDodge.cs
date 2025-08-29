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
	public sealed class ColorDodgeBlendOp : UserBlendOp
	{
		public static string StaticName => "ColorDodge";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((rhs).B) == 255) { fB = 255; } else { { int i = ((255 - ((rhs).B))) * 3; uint M = mas_table[i]; uint A = mas_table[i + 1]; uint S = mas_table[i + 2]; fB = (int) ((((((lhs).B) * 255) * M) + A) >> (int) S); }; fB = Math.Min (255, fB); } }; { if (((rhs).G) == 255) { fG = 255; } else { { int i = ((255 - ((rhs).G))) * 3; uint M = mas_table[i]; uint A = mas_table[i + 1]; uint S = mas_table[i + 2]; fG = (int) ((((((lhs).G) * 255) * M) + A) >> (int) S); }; fG = Math.Min (255, fG); } }; { if (((rhs).R) == 255) { fR = 255; } else { { int i = ((255 - ((rhs).R))) * 3; uint M = mas_table[i]; uint A = mas_table[i + 1]; uint S = mas_table[i + 2]; fR = (int) ((((((lhs).R) * 255) * M) + A) >> (int) S); }; fR = Math.Min (255, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = mas_table[masIndex]; uint taA = mas_table[masIndex + 1]; uint taS = mas_table[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((rhs).B) == 255) { fB = 255; } else { { int i = ((255 - ((rhs).B))) * 3; uint M = mas_table[i]; uint A = mas_table[i + 1]; uint S = mas_table[i + 2]; fB = (int) ((((((lhs).B) * 255) * M) + A) >> (int) S); }; fB = Math.Min (255, fB); } }; { if (((rhs).G) == 255) { fG = 255; } else { { int i = ((255 - ((rhs).G))) * 3; uint M = mas_table[i]; uint A = mas_table[i + 1]; uint S = mas_table[i + 2]; fG = (int) ((((((lhs).G) * 255) * M) + A) >> (int) S); }; fG = Math.Min (255, fG); } }; { if (((rhs).R) == 255) { fR = 255; } else { { int i = ((255 - ((rhs).R))) * 3; uint M = mas_table[i]; uint A = mas_table[i + 1]; uint S = mas_table[i + 2]; fR = (int) ((((((lhs).R) * 255) * M) + A) >> (int) S); }; fR = Math.Min (255, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = mas_table[masIndex]; uint taA = mas_table[masIndex + 1]; uint taS = mas_table[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
	}
}
