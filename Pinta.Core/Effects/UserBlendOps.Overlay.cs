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
	public sealed class OverlayBlendOp : UserBlendOp
	{
		public static string StaticName => "Overlay";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((lhs).B) < 128) { { fB = ((2 * ((lhs).B)) * (((rhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; } else { { fB = ((2 * (255 - ((lhs).B))) * (255 - ((rhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; fB = 255 - fB; } }; { if (((lhs).G) < 128) { { fG = ((2 * ((lhs).G)) * (((rhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; } else { { fG = ((2 * (255 - ((lhs).G))) * (255 - ((rhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; fG = 255 - fG; } }; { if (((lhs).R) < 128) { { fR = ((2 * ((lhs).R)) * (((rhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; } else { { fR = ((2 * (255 - ((lhs).R))) * (255 - ((rhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; fR = 255 - fR; } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = mas_table[masIndex]; uint taA = mas_table[masIndex + 1]; uint taS = mas_table[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((lhs).B) < 128) { { fB = ((2 * ((lhs).B)) * (((rhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; } else { { fB = ((2 * (255 - ((lhs).B))) * (255 - ((rhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; fB = 255 - fB; } }; { if (((lhs).G) < 128) { { fG = ((2 * ((lhs).G)) * (((rhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; } else { { fG = ((2 * (255 - ((lhs).G))) * (255 - ((rhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; fG = 255 - fG; } }; { if (((lhs).R) < 128) { { fR = ((2 * ((lhs).R)) * (((rhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; } else { { fR = ((2 * (255 - ((lhs).R))) * (255 - ((rhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; fR = 255 - fR; } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = mas_table[masIndex]; uint taA = mas_table[masIndex + 1]; uint taS = mas_table[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
	}
}
