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
	public sealed class ColorBurnBlendOp : UserBlendOp
	{
		public static string StaticName => "ColorBurn";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((rhs).B) == 0) { fB = 0; } else { { int i = (((rhs).B)) * 3; uint M = mas_table[i]; uint A = mas_table[i + 1]; uint S = mas_table[i + 2]; fB = (int) (((((255 - ((lhs).B)) * 255) * M) + A) >> (int) S); }; fB = 255 - fB; fB = Math.Max (0, fB); } }; { if (((rhs).G) == 0) { fG = 0; } else { { int i = (((rhs).G)) * 3; uint M = mas_table[i]; uint A = mas_table[i + 1]; uint S = mas_table[i + 2]; fG = (int) (((((255 - ((lhs).G)) * 255) * M) + A) >> (int) S); }; fG = 255 - fG; fG = Math.Max (0, fG); } }; { if (((rhs).R) == 0) { fR = 0; } else { { int i = (((rhs).R)) * 3; uint M = mas_table[i]; uint A = mas_table[i + 1]; uint S = mas_table[i + 2]; fR = (int) (((((255 - ((lhs).R)) * 255) * M) + A) >> (int) S); }; fR = 255 - fR; fR = Math.Max (0, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = mas_table[masIndex]; uint taA = mas_table[masIndex + 1]; uint taS = mas_table[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((rhs).B) == 0) { fB = 0; } else { { int i = (((rhs).B)) * 3; uint M = mas_table[i]; uint A = mas_table[i + 1]; uint S = mas_table[i + 2]; fB = (int) (((((255 - ((lhs).B)) * 255) * M) + A) >> (int) S); }; fB = 255 - fB; fB = Math.Max (0, fB); } }; { if (((rhs).G) == 0) { fG = 0; } else { { int i = (((rhs).G)) * 3; uint M = mas_table[i]; uint A = mas_table[i + 1]; uint S = mas_table[i + 2]; fG = (int) (((((255 - ((lhs).G)) * 255) * M) + A) >> (int) S); }; fG = 255 - fG; fG = Math.Max (0, fG); } }; { if (((rhs).R) == 0) { fR = 0; } else { { int i = (((rhs).R)) * 3; uint M = mas_table[i]; uint A = mas_table[i + 1]; uint S = mas_table[i + 2]; fR = (int) (((((255 - ((lhs).R)) * 255) * M) + A) >> (int) S); }; fR = 255 - fR; fR = Math.Max (0, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = mas_table[masIndex]; uint taA = mas_table[masIndex + 1]; uint taS = mas_table[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
	}
}
