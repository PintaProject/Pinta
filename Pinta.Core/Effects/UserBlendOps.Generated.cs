/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;

// The generalized alpha compositing formula, "B OVER A" is:
// C(A,a,B,b) = bB + aA - baA
// where:
//    A = background color value
//    a = background alpha value
//    B = foreground color value
//    b = foreground alpha value
//
// However, we need a general formula for composition based on any type of
// blend operation and not just for 'normal' blending. We want multiplicative,
// additive, etc. blend operations.
//
// The generalized alpha compositing formula w.r.t. a replaceable blending
// function is:
//
// G(A,a,B,b,F) = (a - ab)A + (b - ab)B + abF(A, B)
// 
// Where F is a function of A and B, or F(A,B), that results in another color
// value. For A OVER B blending, we simply use F(A,B) = B. It can be easily 
// shown that the two formulas simplify to the same expression when this F is 
// used.
//
// G can be generalized even further to take a function for the other input
// values. This can be useful if one wishes to implement something like 
// (1 - B) OVER A blending.
//
// In this reality, F(A,B) is really F(A,B,r). The syntax "r = F(A,B)" is
// the same as "F(A,B,r)" where r is essentially an 'out' parameter.

// Multiplies a and b, which are [0,255] as if they were scaled to [0,1], and returns the result in r
// a and b are evaluated once. r is evaluated multiple times.

// F(A,B) = blending function for the pixel values
// h(a) = function for loading lhs.A, usually just ID
// j(a) = function for loading rhs.A, usually just ID
// n DIV d
//{ r = (((B) == 0) ? 0 : Math.Max(0, (255 - (((255 - (A)) * 255) / (B))))); }
// { r = ((B) == 255 ? 255 : Math.Min(255, ((A) * 255) / (255 - (B)))); }
// r = { (((B) == 255) ? 255 : Math.Min(255, ((A) * (A)) / (255 - (B)))); }
//{ r = ((B) + (A) - (((B) * (A)) / 255)); }

namespace Pinta.Core;

partial class UserBlendOps
{
	// i = z * 3;
	// (x / z) = ((x * masTable[i]) + masTable[i + 1]) >> masTable[i + 2)
	private static readonly uint[] masTable =
	{
	    0x00000000, 0x00000000, 0,  // 0
            0x00000001, 0x00000000, 0,  // 1
            0x00000001, 0x00000000, 1,  // 2
            0xAAAAAAAB, 0x00000000, 33, // 3
            0x00000001, 0x00000000, 2,  // 4
            0xCCCCCCCD, 0x00000000, 34, // 5
            0xAAAAAAAB, 0x00000000, 34, // 6
            0x49249249, 0x49249249, 33, // 7
            0x00000001, 0x00000000, 3,  // 8
            0x38E38E39, 0x00000000, 33, // 9
            0xCCCCCCCD, 0x00000000, 35, // 10
            0xBA2E8BA3, 0x00000000, 35, // 11
            0xAAAAAAAB, 0x00000000, 35, // 12
            0x4EC4EC4F, 0x00000000, 34, // 13
            0x49249249, 0x49249249, 34, // 14
            0x88888889, 0x00000000, 35, // 15
            0x00000001, 0x00000000, 4,  // 16
            0xF0F0F0F1, 0x00000000, 36, // 17
            0x38E38E39, 0x00000000, 34, // 18
            0xD79435E5, 0xD79435E5, 36, // 19
            0xCCCCCCCD, 0x00000000, 36, // 20
            0xC30C30C3, 0xC30C30C3, 36, // 21
            0xBA2E8BA3, 0x00000000, 36, // 22
            0xB21642C9, 0x00000000, 36, // 23
            0xAAAAAAAB, 0x00000000, 36, // 24
            0x51EB851F, 0x00000000, 35, // 25
            0x4EC4EC4F, 0x00000000, 35, // 26
            0x97B425ED, 0x97B425ED, 36, // 27
            0x49249249, 0x49249249, 35, // 28
            0x8D3DCB09, 0x00000000, 36, // 29
            0x88888889, 0x00000000, 36, // 30
            0x42108421, 0x42108421, 35, // 31
            0x00000001, 0x00000000, 5,  // 32
            0x3E0F83E1, 0x00000000, 35, // 33
            0xF0F0F0F1, 0x00000000, 37, // 34
            0x75075075, 0x75075075, 36, // 35
            0x38E38E39, 0x00000000, 35, // 36
            0x6EB3E453, 0x6EB3E453, 36, // 37
            0xD79435E5, 0xD79435E5, 37, // 38
            0x69069069, 0x69069069, 36, // 39
            0xCCCCCCCD, 0x00000000, 37, // 40
            0xC7CE0C7D, 0x00000000, 37, // 41
            0xC30C30C3, 0xC30C30C3, 37, // 42
            0x2FA0BE83, 0x00000000, 35, // 43
            0xBA2E8BA3, 0x00000000, 37, // 44
            0x5B05B05B, 0x5B05B05B, 36, // 45
            0xB21642C9, 0x00000000, 37, // 46
            0xAE4C415D, 0x00000000, 37, // 47
            0xAAAAAAAB, 0x00000000, 37, // 48
            0x5397829D, 0x00000000, 36, // 49
            0x51EB851F, 0x00000000, 36, // 50
            0xA0A0A0A1, 0x00000000, 37, // 51
            0x4EC4EC4F, 0x00000000, 36, // 52
            0x9A90E7D9, 0x9A90E7D9, 37, // 53
            0x97B425ED, 0x97B425ED, 37, // 54
            0x94F2094F, 0x94F2094F, 37, // 55
            0x49249249, 0x49249249, 36, // 56
            0x47DC11F7, 0x47DC11F7, 36, // 57
            0x8D3DCB09, 0x00000000, 37, // 58
            0x22B63CBF, 0x00000000, 35, // 59
            0x88888889, 0x00000000, 37, // 60
            0x4325C53F, 0x00000000, 36, // 61
            0x42108421, 0x42108421, 36, // 62
            0x41041041, 0x41041041, 36, // 63
            0x00000001, 0x00000000, 6,  // 64
            0xFC0FC0FD, 0x00000000, 38, // 65
            0x3E0F83E1, 0x00000000, 36, // 66
            0x07A44C6B, 0x00000000, 33, // 67
            0xF0F0F0F1, 0x00000000, 38, // 68
            0x76B981DB, 0x00000000, 37, // 69
            0x75075075, 0x75075075, 37, // 70
            0xE6C2B449, 0x00000000, 38, // 71
            0x38E38E39, 0x00000000, 36, // 72
            0x381C0E07, 0x381C0E07, 36, // 73
            0x6EB3E453, 0x6EB3E453, 37, // 74
            0x1B4E81B5, 0x00000000, 35, // 75
            0xD79435E5, 0xD79435E5, 38, // 76
            0x3531DEC1, 0x00000000, 36, // 77
            0x69069069, 0x69069069, 37, // 78
            0xCF6474A9, 0x00000000, 38, // 79
            0xCCCCCCCD, 0x00000000, 38, // 80
            0xCA4587E7, 0x00000000, 38, // 81
            0xC7CE0C7D, 0x00000000, 38, // 82
            0x3159721F, 0x00000000, 36, // 83
            0xC30C30C3, 0xC30C30C3, 38, // 84
            0xC0C0C0C1, 0x00000000, 38, // 85
            0x2FA0BE83, 0x00000000, 36, // 86
            0x2F149903, 0x00000000, 36, // 87
            0xBA2E8BA3, 0x00000000, 38, // 88
            0xB81702E1, 0x00000000, 38, // 89
            0x5B05B05B, 0x5B05B05B, 37, // 90
            0x2D02D02D, 0x2D02D02D, 36, // 91
            0xB21642C9, 0x00000000, 38, // 92
            0xB02C0B03, 0x00000000, 38, // 93
            0xAE4C415D, 0x00000000, 38, // 94
            0x2B1DA461, 0x2B1DA461, 36, // 95
            0xAAAAAAAB, 0x00000000, 38, // 96
            0xA8E83F57, 0xA8E83F57, 38, // 97
            0x5397829D, 0x00000000, 37, // 98
            0xA57EB503, 0x00000000, 38, // 99
            0x51EB851F, 0x00000000, 37, // 100
            0xA237C32B, 0xA237C32B, 38, // 101
            0xA0A0A0A1, 0x00000000, 38, // 102
            0x9F1165E7, 0x9F1165E7, 38, // 103
            0x4EC4EC4F, 0x00000000, 37, // 104
            0x27027027, 0x27027027, 36, // 105
            0x9A90E7D9, 0x9A90E7D9, 38, // 106
            0x991F1A51, 0x991F1A51, 38, // 107
            0x97B425ED, 0x97B425ED, 38, // 108
            0x2593F69B, 0x2593F69B, 36, // 109
            0x94F2094F, 0x94F2094F, 38, // 110
            0x24E6A171, 0x24E6A171, 36, // 111
            0x49249249, 0x49249249, 37, // 112
            0x90FDBC09, 0x90FDBC09, 38, // 113
            0x47DC11F7, 0x47DC11F7, 37, // 114
            0x8E78356D, 0x8E78356D, 38, // 115
            0x8D3DCB09, 0x00000000, 38, // 116
            0x23023023, 0x23023023, 36, // 117
            0x22B63CBF, 0x00000000, 36, // 118
            0x44D72045, 0x00000000, 37, // 119
            0x88888889, 0x00000000, 38, // 120
            0x8767AB5F, 0x8767AB5F, 38, // 121
            0x4325C53F, 0x00000000, 37, // 122
            0x85340853, 0x85340853, 38, // 123
            0x42108421, 0x42108421, 37, // 124
            0x10624DD3, 0x00000000, 35, // 125
            0x41041041, 0x41041041, 37, // 126
            0x10204081, 0x10204081, 35, // 127
            0x00000001, 0x00000000, 7,  // 128
            0x0FE03F81, 0x00000000, 35, // 129
            0xFC0FC0FD, 0x00000000, 39, // 130
            0xFA232CF3, 0x00000000, 39, // 131
            0x3E0F83E1, 0x00000000, 37, // 132
            0xF6603D99, 0x00000000, 39, // 133
            0x07A44C6B, 0x00000000, 34, // 134
            0xF2B9D649, 0x00000000, 39, // 135
            0xF0F0F0F1, 0x00000000, 39, // 136
            0x077975B9, 0x00000000, 34, // 137
            0x76B981DB, 0x00000000, 38, // 138
            0x75DED953, 0x00000000, 38, // 139
            0x75075075, 0x75075075, 38, // 140
            0x3A196B1F, 0x00000000, 37, // 141
            0xE6C2B449, 0x00000000, 39, // 142
            0xE525982B, 0x00000000, 39, // 143
            0x38E38E39, 0x00000000, 37, // 144
            0xE1FC780F, 0x00000000, 39, // 145
            0x381C0E07, 0x381C0E07, 37, // 146
            0xDEE95C4D, 0x00000000, 39, // 147
            0x6EB3E453, 0x6EB3E453, 38, // 148
            0xDBEB61EF, 0x00000000, 39, // 149
            0x1B4E81B5, 0x00000000, 36, // 150
            0x36406C81, 0x00000000, 37, // 151
            0xD79435E5, 0xD79435E5, 39, // 152
            0xD62B80D7, 0x00000000, 39, // 153
            0x3531DEC1, 0x00000000, 37, // 154
            0xD3680D37, 0x00000000, 39, // 155
            0x69069069, 0x69069069, 38, // 156
            0x342DA7F3, 0x00000000, 37, // 157
            0xCF6474A9, 0x00000000, 39, // 158
            0xCE168A77, 0xCE168A77, 39, // 159
            0xCCCCCCCD, 0x00000000, 39, // 160
            0xCB8727C1, 0x00000000, 39, // 161
            0xCA4587E7, 0x00000000, 39, // 162
            0xC907DA4F, 0x00000000, 39, // 163
            0xC7CE0C7D, 0x00000000, 39, // 164
            0x634C0635, 0x00000000, 38, // 165
            0x3159721F, 0x00000000, 37, // 166
            0x621B97C3, 0x00000000, 38, // 167
            0xC30C30C3, 0xC30C30C3, 39, // 168
            0x60F25DEB, 0x00000000, 38, // 169
            0xC0C0C0C1, 0x00000000, 39, // 170
            0x17F405FD, 0x17F405FD, 36, // 171
            0x2FA0BE83, 0x00000000, 37, // 172
            0xBD691047, 0xBD691047, 39, // 173
            0x2F149903, 0x00000000, 37, // 174
            0x5D9F7391, 0x00000000, 38, // 175
            0xBA2E8BA3, 0x00000000, 39, // 176
            0x5C90A1FD, 0x5C90A1FD, 38, // 177
            0xB81702E1, 0x00000000, 39, // 178
            0x5B87DDAD, 0x5B87DDAD, 38, // 179
            0x5B05B05B, 0x5B05B05B, 38, // 180
            0xB509E68B, 0x00000000, 39, // 181
            0x2D02D02D, 0x2D02D02D, 37, // 182
            0xB30F6353, 0x00000000, 39, // 183
            0xB21642C9, 0x00000000, 39, // 184
            0x1623FA77, 0x1623FA77, 36, // 185
            0xB02C0B03, 0x00000000, 39, // 186
            0xAF3ADDC7, 0x00000000, 39, // 187
            0xAE4C415D, 0x00000000, 39, // 188
            0x15AC056B, 0x15AC056B, 36, // 189
            0x2B1DA461, 0x2B1DA461, 37, // 190
            0xAB8F69E3, 0x00000000, 39, // 191
            0xAAAAAAAB, 0x00000000, 39, // 192
            0x15390949, 0x00000000, 36, // 193
            0xA8E83F57, 0xA8E83F57, 39, // 194
            0x15015015, 0x15015015, 36, // 195
            0x5397829D, 0x00000000, 38, // 196
            0xA655C439, 0xA655C439, 39, // 197
            0xA57EB503, 0x00000000, 39, // 198
            0x5254E78F, 0x00000000, 38, // 199
            0x51EB851F, 0x00000000, 38, // 200
            0x028C1979, 0x00000000, 33, // 201
            0xA237C32B, 0xA237C32B, 39, // 202
            0xA16B312F, 0x00000000, 39, // 203
            0xA0A0A0A1, 0x00000000, 39, // 204
            0x4FEC04FF, 0x00000000, 38, // 205
            0x9F1165E7, 0x9F1165E7, 39, // 206
            0x27932B49, 0x00000000, 37, // 207
            0x4EC4EC4F, 0x00000000, 38, // 208
            0x9CC8E161, 0x00000000, 39, // 209
            0x27027027, 0x27027027, 37, // 210
            0x9B4C6F9F, 0x00000000, 39, // 211
            0x9A90E7D9, 0x9A90E7D9, 39, // 212
            0x99D722DB, 0x00000000, 39, // 213
            0x991F1A51, 0x991F1A51, 39, // 214
            0x4C346405, 0x00000000, 38, // 215
            0x97B425ED, 0x97B425ED, 39, // 216
            0x4B809701, 0x4B809701, 38, // 217
            0x2593F69B, 0x2593F69B, 37, // 218
            0x12B404AD, 0x12B404AD, 36, // 219
            0x94F2094F, 0x94F2094F, 39, // 220
            0x25116025, 0x25116025, 37, // 221
            0x24E6A171, 0x24E6A171, 37, // 222
            0x24BC44E1, 0x24BC44E1, 37, // 223
            0x49249249, 0x49249249, 38, // 224
            0x91A2B3C5, 0x00000000, 39, // 225
            0x90FDBC09, 0x90FDBC09, 39, // 226
            0x905A3863, 0x905A3863, 39, // 227
            0x47DC11F7, 0x47DC11F7, 38, // 228
            0x478BBCED, 0x00000000, 38, // 229
            0x8E78356D, 0x8E78356D, 39, // 230
            0x46ED2901, 0x46ED2901, 38, // 231
            0x8D3DCB09, 0x00000000, 39, // 232
            0x2328A701, 0x2328A701, 37, // 233
            0x23023023, 0x23023023, 37, // 234
            0x45B81A25, 0x45B81A25, 38, // 235
            0x22B63CBF, 0x00000000, 37, // 236
            0x08A42F87, 0x08A42F87, 35, // 237
            0x44D72045, 0x00000000, 38, // 238
            0x891AC73B, 0x00000000, 39, // 239
            0x88888889, 0x00000000, 39, // 240
            0x10FEF011, 0x00000000, 36, // 241
            0x8767AB5F, 0x8767AB5F, 39, // 242
            0x86D90545, 0x00000000, 39, // 243
            0x4325C53F, 0x00000000, 38, // 244
            0x85BF3761, 0x85BF3761, 39, // 245
            0x85340853, 0x85340853, 39, // 246
            0x10953F39, 0x10953F39, 36, // 247
            0x42108421, 0x42108421, 38, // 248
            0x41CC9829, 0x41CC9829, 38, // 249
            0x10624DD3, 0x00000000, 36, // 250
            0x828CBFBF, 0x00000000, 39, // 251
            0x41041041, 0x41041041, 38, // 252
            0x81848DA9, 0x00000000, 39, // 253
            0x10204081, 0x10204081, 36, // 254
            0x80808081, 0x00000000, 39  // 255
        };

	[Serializable]
	public sealed class NormalBlendOp : UserBlendOp
	{
		public static string StaticName => "Normal";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = ((rhs).B); }; { fG = ((rhs).G); }; { fR = ((rhs).R); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = ((rhs).B); }; { fG = ((rhs).G); }; { fR = ((rhs).R); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public override UserBlendOp CreateWithOpacity (int opacity) { return new NormalBlendOpWithOpacity (opacity); }

		private sealed class NormalBlendOpWithOpacity : UserBlendOp
		{
			private int opacity;
			private byte ApplyOpacity (byte a) { int r; { r = (a); }; { r = ((r) * (this.opacity) + 0x80); r = ((((r) >> 8) + (r)) >> 8); }; return (byte) r; }
			public static string StaticName => "Normal";
			public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ApplyOpacity ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = ((rhs).B); }; { fG = ((rhs).G); }; { fR = ((rhs).R); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
			public NormalBlendOpWithOpacity (int opacity) { if (this.opacity < 0 || this.opacity > 255) { throw new ArgumentOutOfRangeException (); } this.opacity = opacity; }
		}
	}

	[Serializable]
	public sealed class MultiplyBlendOp : UserBlendOp
	{
		public static string StaticName => "Multiply";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { { fB = ((((lhs).B)) * (((rhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; }; { { fG = ((((lhs).G)) * (((rhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; }; { { fR = ((((lhs).R)) * (((rhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { { fB = ((((lhs).B)) * (((rhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; }; { { fG = ((((lhs).G)) * (((rhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; }; { { fR = ((((lhs).R)) * (((rhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public override UserBlendOp CreateWithOpacity (int opacity) { return new MultiplyBlendOpWithOpacity (opacity); }
		private sealed class MultiplyBlendOpWithOpacity : UserBlendOp
		{
			private int opacity; private byte ApplyOpacity (byte a) { int r; { r = (a); }; { r = ((r) * (this.opacity) + 0x80); r = ((((r) >> 8) + (r)) >> 8); }; return (byte) r; }
			public static string StaticName => "UserBlendOps." + "Multiply" + "BlendOp.Name";
			public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ApplyOpacity ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { { fB = ((((lhs).B)) * (((rhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; }; { { fG = ((((lhs).G)) * (((rhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; }; { { fR = ((((lhs).R)) * (((rhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
			public MultiplyBlendOpWithOpacity (int opacity) { if (this.opacity < 0 || this.opacity > 255) { throw new ArgumentOutOfRangeException (); } this.opacity = opacity; }
		}
	}
	[Serializable]
	public sealed class AdditiveBlendOp : UserBlendOp
	{
		public static string StaticName => "Additive";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = Math.Min (255, ((lhs).B) + ((rhs).B)); }; { fG = Math.Min (255, ((lhs).G) + ((rhs).G)); }; { fR = Math.Min (255, ((lhs).R) + ((rhs).R)); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = Math.Min (255, ((lhs).B) + ((rhs).B)); }; { fG = Math.Min (255, ((lhs).G) + ((rhs).G)); }; { fR = Math.Min (255, ((lhs).R) + ((rhs).R)); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public override UserBlendOp CreateWithOpacity (int opacity) { return new AdditiveBlendOpWithOpacity (opacity); }
		private sealed class AdditiveBlendOpWithOpacity : UserBlendOp
		{
			private int opacity; private byte ApplyOpacity (byte a) { int r; { r = (a); }; { r = ((r) * (this.opacity) + 0x80); r = ((((r) >> 8) + (r)) >> 8); }; return (byte) r; }
			public static string StaticName => "UserBlendOps." + "Additive" + "BlendOp.Name";
			public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ApplyOpacity ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = Math.Min (255, ((lhs).B) + ((rhs).B)); }; { fG = Math.Min (255, ((lhs).G) + ((rhs).G)); }; { fR = Math.Min (255, ((lhs).R) + ((rhs).R)); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
			public AdditiveBlendOpWithOpacity (int opacity) { if (this.opacity < 0 || this.opacity > 255) { throw new ArgumentOutOfRangeException (); } this.opacity = opacity; }
		}
	}
	[Serializable]
	public sealed class ColorBurnBlendOp : UserBlendOp
	{
		public static string StaticName => "ColorBurn";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((rhs).B) == 0) { fB = 0; } else { { int i = (((rhs).B)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fB = (int) (((((255 - ((lhs).B)) * 255) * M) + A) >> (int) S); }; fB = 255 - fB; fB = Math.Max (0, fB); } }; { if (((rhs).G) == 0) { fG = 0; } else { { int i = (((rhs).G)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fG = (int) (((((255 - ((lhs).G)) * 255) * M) + A) >> (int) S); }; fG = 255 - fG; fG = Math.Max (0, fG); } }; { if (((rhs).R) == 0) { fR = 0; } else { { int i = (((rhs).R)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fR = (int) (((((255 - ((lhs).R)) * 255) * M) + A) >> (int) S); }; fR = 255 - fR; fR = Math.Max (0, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((rhs).B) == 0) { fB = 0; } else { { int i = (((rhs).B)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fB = (int) (((((255 - ((lhs).B)) * 255) * M) + A) >> (int) S); }; fB = 255 - fB; fB = Math.Max (0, fB); } }; { if (((rhs).G) == 0) { fG = 0; } else { { int i = (((rhs).G)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fG = (int) (((((255 - ((lhs).G)) * 255) * M) + A) >> (int) S); }; fG = 255 - fG; fG = Math.Max (0, fG); } }; { if (((rhs).R) == 0) { fR = 0; } else { { int i = (((rhs).R)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fR = (int) (((((255 - ((lhs).R)) * 255) * M) + A) >> (int) S); }; fR = 255 - fR; fR = Math.Max (0, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public override UserBlendOp CreateWithOpacity (int opacity) { return new ColorBurnBlendOpWithOpacity (opacity); }
		private sealed class ColorBurnBlendOpWithOpacity : UserBlendOp
		{
			private int opacity; private byte ApplyOpacity (byte a) { int r; { r = (a); }; { r = ((r) * (this.opacity) + 0x80); r = ((((r) >> 8) + (r)) >> 8); }; return (byte) r; }
			public static string StaticName => "UserBlendOps." + "ColorBurn" + "BlendOp.Name";
			public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ApplyOpacity ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((rhs).B) == 0) { fB = 0; } else { { int i = (((rhs).B)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fB = (int) (((((255 - ((lhs).B)) * 255) * M) + A) >> (int) S); }; fB = 255 - fB; fB = Math.Max (0, fB); } }; { if (((rhs).G) == 0) { fG = 0; } else { { int i = (((rhs).G)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fG = (int) (((((255 - ((lhs).G)) * 255) * M) + A) >> (int) S); }; fG = 255 - fG; fG = Math.Max (0, fG); } }; { if (((rhs).R) == 0) { fR = 0; } else { { int i = (((rhs).R)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fR = (int) (((((255 - ((lhs).R)) * 255) * M) + A) >> (int) S); }; fR = 255 - fR; fR = Math.Max (0, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
			public ColorBurnBlendOpWithOpacity (int opacity) { if (this.opacity < 0 || this.opacity > 255) { throw new ArgumentOutOfRangeException (); } this.opacity = opacity; }
		}
	}
	[Serializable]
	public sealed class ColorDodgeBlendOp : UserBlendOp
	{
		public static string StaticName => "ColorDodge";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((rhs).B) == 255) { fB = 255; } else { { int i = ((255 - ((rhs).B))) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fB = (int) ((((((lhs).B) * 255) * M) + A) >> (int) S); }; fB = Math.Min (255, fB); } }; { if (((rhs).G) == 255) { fG = 255; } else { { int i = ((255 - ((rhs).G))) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fG = (int) ((((((lhs).G) * 255) * M) + A) >> (int) S); }; fG = Math.Min (255, fG); } }; { if (((rhs).R) == 255) { fR = 255; } else { { int i = ((255 - ((rhs).R))) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fR = (int) ((((((lhs).R) * 255) * M) + A) >> (int) S); }; fR = Math.Min (255, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((rhs).B) == 255) { fB = 255; } else { { int i = ((255 - ((rhs).B))) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fB = (int) ((((((lhs).B) * 255) * M) + A) >> (int) S); }; fB = Math.Min (255, fB); } }; { if (((rhs).G) == 255) { fG = 255; } else { { int i = ((255 - ((rhs).G))) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fG = (int) ((((((lhs).G) * 255) * M) + A) >> (int) S); }; fG = Math.Min (255, fG); } }; { if (((rhs).R) == 255) { fR = 255; } else { { int i = ((255 - ((rhs).R))) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fR = (int) ((((((lhs).R) * 255) * M) + A) >> (int) S); }; fR = Math.Min (255, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public override UserBlendOp CreateWithOpacity (int opacity) { return new ColorDodgeBlendOpWithOpacity (opacity); }
		private sealed class ColorDodgeBlendOpWithOpacity : UserBlendOp
		{
			private int opacity; private byte ApplyOpacity (byte a) { int r; { r = (a); }; { r = ((r) * (this.opacity) + 0x80); r = ((((r) >> 8) + (r)) >> 8); }; return (byte) r; }
			public static string StaticName => "UserBlendOps." + "ColorDodge" + "BlendOp.Name";
			public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ApplyOpacity ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((rhs).B) == 255) { fB = 255; } else { { int i = ((255 - ((rhs).B))) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fB = (int) ((((((lhs).B) * 255) * M) + A) >> (int) S); }; fB = Math.Min (255, fB); } }; { if (((rhs).G) == 255) { fG = 255; } else { { int i = ((255 - ((rhs).G))) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fG = (int) ((((((lhs).G) * 255) * M) + A) >> (int) S); }; fG = Math.Min (255, fG); } }; { if (((rhs).R) == 255) { fR = 255; } else { { int i = ((255 - ((rhs).R))) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fR = (int) ((((((lhs).R) * 255) * M) + A) >> (int) S); }; fR = Math.Min (255, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
			public ColorDodgeBlendOpWithOpacity (int opacity) { if (this.opacity < 0 || this.opacity > 255) { throw new ArgumentOutOfRangeException (); } this.opacity = opacity; }
		}
	}
	[Serializable]
	public sealed class ReflectBlendOp : UserBlendOp
	{
		public static string StaticName => "Reflect";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((rhs).B) == 255) { fB = 255; } else { { int i = (255 - ((rhs).B)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fB = (int) (((((lhs).B) * ((lhs).B) * M) + A) >> (int) S); }; fB = Math.Min (255, fB); } }; { if (((rhs).G) == 255) { fG = 255; } else { { int i = (255 - ((rhs).G)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fG = (int) (((((lhs).G) * ((lhs).G) * M) + A) >> (int) S); }; fG = Math.Min (255, fG); } }; { if (((rhs).R) == 255) { fR = 255; } else { { int i = (255 - ((rhs).R)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fR = (int) (((((lhs).R) * ((lhs).R) * M) + A) >> (int) S); }; fR = Math.Min (255, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((rhs).B) == 255) { fB = 255; } else { { int i = (255 - ((rhs).B)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fB = (int) (((((lhs).B) * ((lhs).B) * M) + A) >> (int) S); }; fB = Math.Min (255, fB); } }; { if (((rhs).G) == 255) { fG = 255; } else { { int i = (255 - ((rhs).G)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fG = (int) (((((lhs).G) * ((lhs).G) * M) + A) >> (int) S); }; fG = Math.Min (255, fG); } }; { if (((rhs).R) == 255) { fR = 255; } else { { int i = (255 - ((rhs).R)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fR = (int) (((((lhs).R) * ((lhs).R) * M) + A) >> (int) S); }; fR = Math.Min (255, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public override UserBlendOp CreateWithOpacity (int opacity) { return new ReflectBlendOpWithOpacity (opacity); }
		private sealed class ReflectBlendOpWithOpacity : UserBlendOp
		{
			private int opacity; private byte ApplyOpacity (byte a) { int r; { r = (a); }; { r = ((r) * (this.opacity) + 0x80); r = ((((r) >> 8) + (r)) >> 8); }; return (byte) r; }
			public static string StaticName => "UserBlendOps." + "Reflect" + "BlendOp.Name";
			public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ApplyOpacity ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((rhs).B) == 255) { fB = 255; } else { { int i = (255 - ((rhs).B)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fB = (int) (((((lhs).B) * ((lhs).B) * M) + A) >> (int) S); }; fB = Math.Min (255, fB); } }; { if (((rhs).G) == 255) { fG = 255; } else { { int i = (255 - ((rhs).G)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fG = (int) (((((lhs).G) * ((lhs).G) * M) + A) >> (int) S); }; fG = Math.Min (255, fG); } }; { if (((rhs).R) == 255) { fR = 255; } else { { int i = (255 - ((rhs).R)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fR = (int) (((((lhs).R) * ((lhs).R) * M) + A) >> (int) S); }; fR = Math.Min (255, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
			public ReflectBlendOpWithOpacity (int opacity) { if (this.opacity < 0 || this.opacity > 255) { throw new ArgumentOutOfRangeException (); } this.opacity = opacity; }
		}
	}
	[Serializable]
	public sealed class GlowBlendOp : UserBlendOp
	{
		public static string StaticName => "Glow";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((lhs).B) == 255) { fB = 255; } else { { int i = (255 - ((lhs).B)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fB = (int) (((((rhs).B) * ((rhs).B) * M) + A) >> (int) S); }; fB = Math.Min (255, fB); } }; { if (((lhs).G) == 255) { fG = 255; } else { { int i = (255 - ((lhs).G)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fG = (int) (((((rhs).G) * ((rhs).G) * M) + A) >> (int) S); }; fG = Math.Min (255, fG); } }; { if (((lhs).R) == 255) { fR = 255; } else { { int i = (255 - ((lhs).R)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fR = (int) (((((rhs).R) * ((rhs).R) * M) + A) >> (int) S); }; fR = Math.Min (255, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((lhs).B) == 255) { fB = 255; } else { { int i = (255 - ((lhs).B)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fB = (int) (((((rhs).B) * ((rhs).B) * M) + A) >> (int) S); }; fB = Math.Min (255, fB); } }; { if (((lhs).G) == 255) { fG = 255; } else { { int i = (255 - ((lhs).G)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fG = (int) (((((rhs).G) * ((rhs).G) * M) + A) >> (int) S); }; fG = Math.Min (255, fG); } }; { if (((lhs).R) == 255) { fR = 255; } else { { int i = (255 - ((lhs).R)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fR = (int) (((((rhs).R) * ((rhs).R) * M) + A) >> (int) S); }; fR = Math.Min (255, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public override UserBlendOp CreateWithOpacity (int opacity) { return new GlowBlendOpWithOpacity (opacity); }
		private sealed class GlowBlendOpWithOpacity : UserBlendOp
		{
			private int opacity; private byte ApplyOpacity (byte a) { int r; { r = (a); }; { r = ((r) * (this.opacity) + 0x80); r = ((((r) >> 8) + (r)) >> 8); }; return (byte) r; }
			public static string StaticName => "UserBlendOps." + "Glow" + "BlendOp.Name";
			public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ApplyOpacity ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((lhs).B) == 255) { fB = 255; } else { { int i = (255 - ((lhs).B)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fB = (int) (((((rhs).B) * ((rhs).B) * M) + A) >> (int) S); }; fB = Math.Min (255, fB); } }; { if (((lhs).G) == 255) { fG = 255; } else { { int i = (255 - ((lhs).G)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fG = (int) (((((rhs).G) * ((rhs).G) * M) + A) >> (int) S); }; fG = Math.Min (255, fG); } }; { if (((lhs).R) == 255) { fR = 255; } else { { int i = (255 - ((lhs).R)) * 3; uint M = masTable[i]; uint A = masTable[i + 1]; uint S = masTable[i + 2]; fR = (int) (((((rhs).R) * ((rhs).R) * M) + A) >> (int) S); }; fR = Math.Min (255, fR); } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
			public GlowBlendOpWithOpacity (int opacity) { if (this.opacity < 0 || this.opacity > 255) { throw new ArgumentOutOfRangeException (); } this.opacity = opacity; }
		}
	}
	[Serializable]
	public sealed class OverlayBlendOp : UserBlendOp
	{
		public static string StaticName => "Overlay";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((lhs).B) < 128) { { fB = ((2 * ((lhs).B)) * (((rhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; } else { { fB = ((2 * (255 - ((lhs).B))) * (255 - ((rhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; fB = 255 - fB; } }; { if (((lhs).G) < 128) { { fG = ((2 * ((lhs).G)) * (((rhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; } else { { fG = ((2 * (255 - ((lhs).G))) * (255 - ((rhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; fG = 255 - fG; } }; { if (((lhs).R) < 128) { { fR = ((2 * ((lhs).R)) * (((rhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; } else { { fR = ((2 * (255 - ((lhs).R))) * (255 - ((rhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; fR = 255 - fR; } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((lhs).B) < 128) { { fB = ((2 * ((lhs).B)) * (((rhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; } else { { fB = ((2 * (255 - ((lhs).B))) * (255 - ((rhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; fB = 255 - fB; } }; { if (((lhs).G) < 128) { { fG = ((2 * ((lhs).G)) * (((rhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; } else { { fG = ((2 * (255 - ((lhs).G))) * (255 - ((rhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; fG = 255 - fG; } }; { if (((lhs).R) < 128) { { fR = ((2 * ((lhs).R)) * (((rhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; } else { { fR = ((2 * (255 - ((lhs).R))) * (255 - ((rhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; fR = 255 - fR; } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public override UserBlendOp CreateWithOpacity (int opacity) { return new OverlayBlendOpWithOpacity (opacity); }
		private sealed class OverlayBlendOpWithOpacity : UserBlendOp
		{
			private int opacity; private byte ApplyOpacity (byte a) { int r; { r = (a); }; { r = ((r) * (this.opacity) + 0x80); r = ((((r) >> 8) + (r)) >> 8); }; return (byte) r; }
			public static string StaticName => "UserBlendOps." + "Overlay" + "BlendOp.Name";
			public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ApplyOpacity ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { if (((lhs).B) < 128) { { fB = ((2 * ((lhs).B)) * (((rhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; } else { { fB = ((2 * (255 - ((lhs).B))) * (255 - ((rhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; fB = 255 - fB; } }; { if (((lhs).G) < 128) { { fG = ((2 * ((lhs).G)) * (((rhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; } else { { fG = ((2 * (255 - ((lhs).G))) * (255 - ((rhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; fG = 255 - fG; } }; { if (((lhs).R) < 128) { { fR = ((2 * ((lhs).R)) * (((rhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; } else { { fR = ((2 * (255 - ((lhs).R))) * (255 - ((rhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; fR = 255 - fR; } }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
			public OverlayBlendOpWithOpacity (int opacity) { if (this.opacity < 0 || this.opacity > 255) { throw new ArgumentOutOfRangeException (); } this.opacity = opacity; }
		}
	}
	[Serializable]
	public sealed class DifferenceBlendOp : UserBlendOp
	{
		public static string StaticName => "Difference";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = Math.Abs (((rhs).B) - ((lhs).B)); }; { fG = Math.Abs (((rhs).G) - ((lhs).G)); }; { fR = Math.Abs (((rhs).R) - ((lhs).R)); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = Math.Abs (((rhs).B) - ((lhs).B)); }; { fG = Math.Abs (((rhs).G) - ((lhs).G)); }; { fR = Math.Abs (((rhs).R) - ((lhs).R)); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public override UserBlendOp CreateWithOpacity (int opacity) { return new DifferenceBlendOpWithOpacity (opacity); }
		private sealed class DifferenceBlendOpWithOpacity : UserBlendOp
		{
			private int opacity; private byte ApplyOpacity (byte a) { int r; { r = (a); }; { r = ((r) * (this.opacity) + 0x80); r = ((((r) >> 8) + (r)) >> 8); }; return (byte) r; }
			public static string StaticName => "UserBlendOps." + "Difference" + "BlendOp.Name";
			public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ApplyOpacity ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = Math.Abs (((rhs).B) - ((lhs).B)); }; { fG = Math.Abs (((rhs).G) - ((lhs).G)); }; { fR = Math.Abs (((rhs).R) - ((lhs).R)); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
			public DifferenceBlendOpWithOpacity (int opacity) { if (this.opacity < 0 || this.opacity > 255) { throw new ArgumentOutOfRangeException (); } this.opacity = opacity; }
		}
	}
	[Serializable]
	public sealed class NegationBlendOp : UserBlendOp
	{
		public static string StaticName => "Negation";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = (255 - Math.Abs (255 - ((lhs).B) - ((rhs).B))); }; { fG = (255 - Math.Abs (255 - ((lhs).G) - ((rhs).G))); }; { fR = (255 - Math.Abs (255 - ((lhs).R) - ((rhs).R))); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = (255 - Math.Abs (255 - ((lhs).B) - ((rhs).B))); }; { fG = (255 - Math.Abs (255 - ((lhs).G) - ((rhs).G))); }; { fR = (255 - Math.Abs (255 - ((lhs).R) - ((rhs).R))); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public override UserBlendOp CreateWithOpacity (int opacity) { return new NegationBlendOpWithOpacity (opacity); }
		private sealed class NegationBlendOpWithOpacity : UserBlendOp
		{
			private int opacity; private byte ApplyOpacity (byte a) { int r; { r = (a); }; { r = ((r) * (this.opacity) + 0x80); r = ((((r) >> 8) + (r)) >> 8); }; return (byte) r; }
			public static string StaticName => "UserBlendOps." + "Negation" + "BlendOp.Name";
			public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ApplyOpacity ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = (255 - Math.Abs (255 - ((lhs).B) - ((rhs).B))); }; { fG = (255 - Math.Abs (255 - ((lhs).G) - ((rhs).G))); }; { fR = (255 - Math.Abs (255 - ((lhs).R) - ((rhs).R))); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
			public NegationBlendOpWithOpacity (int opacity) { if (this.opacity < 0 || this.opacity > 255) { throw new ArgumentOutOfRangeException (); } this.opacity = opacity; }
		}
	}
	[Serializable]
	public sealed class LightenBlendOp : UserBlendOp
	{
		public static string StaticName => "Lighten";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = Math.Max ((lhs).B, (rhs).B); }; { fG = Math.Max ((lhs).G, (rhs).G); }; { fR = Math.Max ((lhs).R, (rhs).R); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = Math.Max ((lhs).B, (rhs).B); }; { fG = Math.Max ((lhs).G, (rhs).G); }; { fR = Math.Max ((lhs).R, (rhs).R); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public override UserBlendOp CreateWithOpacity (int opacity) { return new LightenBlendOpWithOpacity (opacity); }
		private sealed class LightenBlendOpWithOpacity : UserBlendOp
		{
			private int opacity; private byte ApplyOpacity (byte a) { int r; { r = (a); }; { r = ((r) * (this.opacity) + 0x80); r = ((((r) >> 8) + (r)) >> 8); }; return (byte) r; }
			public static string StaticName => "UserBlendOps." + "Lighten" + "BlendOp.Name";
			public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ApplyOpacity ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = Math.Max ((lhs).B, (rhs).B); }; { fG = Math.Max ((lhs).G, (rhs).G); }; { fR = Math.Max ((lhs).R, (rhs).R); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
			public LightenBlendOpWithOpacity (int opacity) { if (this.opacity < 0 || this.opacity > 255) { throw new ArgumentOutOfRangeException (); } this.opacity = opacity; }
		}
	}
	[Serializable]
	public sealed class DarkenBlendOp : UserBlendOp
	{
		public static string StaticName => "Darken";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = Math.Min ((lhs).B, (rhs).B); }; { fG = Math.Min ((lhs).G, (rhs).G); }; { fR = Math.Min ((lhs).R, (rhs).R); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = Math.Min ((lhs).B, (rhs).B); }; { fG = Math.Min ((lhs).G, (rhs).G); }; { fR = Math.Min ((lhs).R, (rhs).R); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public override UserBlendOp CreateWithOpacity (int opacity) { return new DarkenBlendOpWithOpacity (opacity); }
		private sealed class DarkenBlendOpWithOpacity : UserBlendOp
		{
			private int opacity; private byte ApplyOpacity (byte a) { int r; { r = (a); }; { r = ((r) * (this.opacity) + 0x80); r = ((((r) >> 8) + (r)) >> 8); }; return (byte) r; }
			public static string StaticName => "UserBlendOps." + "Darken" + "BlendOp.Name";
			public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ApplyOpacity ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = Math.Min ((lhs).B, (rhs).B); }; { fG = Math.Min ((lhs).G, (rhs).G); }; { fR = Math.Min ((lhs).R, (rhs).R); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
			public DarkenBlendOpWithOpacity (int opacity) { if (this.opacity < 0 || this.opacity > 255) { throw new ArgumentOutOfRangeException (); } this.opacity = opacity; }
		}
	}
	[Serializable]
	public sealed class ScreenBlendOp : UserBlendOp
	{
		public static string StaticName => "Screen";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { { fB = ((((rhs).B)) * (((lhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; fB = ((rhs).B) + ((lhs).B) - fB; }; { { fG = ((((rhs).G)) * (((lhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; fG = ((rhs).G) + ((lhs).G) - fG; }; { { fR = ((((rhs).R)) * (((lhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; fR = ((rhs).R) + ((lhs).R) - fR; }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { { fB = ((((rhs).B)) * (((lhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; fB = ((rhs).B) + ((lhs).B) - fB; }; { { fG = ((((rhs).G)) * (((lhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; fG = ((rhs).G) + ((lhs).G) - fG; }; { { fR = ((((rhs).R)) * (((lhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; fR = ((rhs).R) + ((lhs).R) - fR; }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public override UserBlendOp CreateWithOpacity (int opacity) { return new ScreenBlendOpWithOpacity (opacity); }
		private sealed class ScreenBlendOpWithOpacity : UserBlendOp
		{
			private int opacity; private byte ApplyOpacity (byte a) { int r; { r = (a); }; { r = ((r) * (this.opacity) + 0x80); r = ((((r) >> 8) + (r)) >> 8); }; return (byte) r; }
			public static string StaticName => "UserBlendOps." + "Screen" + "BlendOp.Name";
			public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ApplyOpacity ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { { fB = ((((rhs).B)) * (((lhs).B)) + 0x80); fB = ((((fB) >> 8) + (fB)) >> 8); }; fB = ((rhs).B) + ((lhs).B) - fB; }; { { fG = ((((rhs).G)) * (((lhs).G)) + 0x80); fG = ((((fG) >> 8) + (fG)) >> 8); }; fG = ((rhs).G) + ((lhs).G) - fG; }; { { fR = ((((rhs).R)) * (((lhs).R)) + 0x80); fR = ((((fR) >> 8) + (fR)) >> 8); }; fR = ((rhs).R) + ((lhs).R) - fR; }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = masTable[masIndex]; uint taA = masTable[masIndex + 1]; uint taS = masTable[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
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
			public ScreenBlendOpWithOpacity (int opacity) { if (this.opacity < 0 || this.opacity > 255) { throw new ArgumentOutOfRangeException (); } this.opacity = opacity; }
		}
	}

	[Serializable]
	public sealed class XorBlendOp : UserBlendOp
	{
		public static string StaticName => "Xor";

		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs)
		{
			return ApplyStatic (lhs, rhs);
		}

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
			};
			int totalA = y + rhs.A;
			uint ret;
			if (totalA == 0) {
				ret = 0;
			} else {
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
				uint taM = masTable[masIndex];
				uint taA = masTable[masIndex + 1];
				uint taS = masTable[masIndex + 2];
				uint b = (uint) (((((long) ((lhs.B * y) + (rhs.B * z) + (fB * x))) * taM) + taA) >> (int) taS);
				uint g = (uint) (((((long) ((lhs.G * y) + (rhs.G * z) + (fG * x))) * taM) + taA) >> (int) taS);
				uint r = (uint) (((((long) ((lhs.R * y) + (rhs.R * z) + (fR * x))) * taM) + taA) >> (int) taS);
				int a;
				{
					a = lhs.A * (255 - rhs.A) + 0x80;
					a = ((a >> 8) + a) >> 8;
					a += rhs.A;
				}
				ret = b + (g << 8) + (r << 16) + ((uint) a << 24);
			};
			return ColorBgra.FromUInt32 (ret);
		}
		public override UserBlendOp CreateWithOpacity (int opacity) => new XorBlendOpWithOpacity (opacity);
		private sealed class XorBlendOpWithOpacity : UserBlendOp
		{
			private readonly int opacity;
			private byte ApplyOpacity (byte a)
			{
				int r;
				{
					r = a;
					r = r * opacity + 0x80;
					r = ((r >> 8) + r) >> 8;
				}
				return (byte) r;
			}
			public static string StaticName => "UserBlendOps." + "Xor" + "BlendOp.Name";
			public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs)
			{
				int lhsA = lhs.A;
				int rhsA = ApplyOpacity (rhs.A);
				int y;
				{
					y = lhsA * (255 - rhsA) + 0x80;
					y = ((y >> 8) + y) >> 8;
				};
				int totalA = y + rhsA;
				uint ret;
				if (totalA == 0) {
					ret = 0;
				} else {
					int fB = lhs.B ^ rhs.B;
					int fG = lhs.G ^ rhs.G;
					int fR = lhs.R ^ rhs.R;
					int x;
					{
						x = lhsA * rhsA + 0x80;
						x = ((x >> 8) + x) >> 8;
					}
					int z = rhsA - x;
					int masIndex = totalA * 3;
					uint taM = masTable[masIndex];
					uint taA = masTable[masIndex + 1];
					uint taS = masTable[masIndex + 2];
					uint b = (uint) (((((long) ((lhs.B * y) + (rhs.B * z) + (fB * x))) * taM) + taA) >> (int) taS);
					uint g = (uint) (((((long) ((lhs.G * y) + (rhs.G * z) + (fG * x))) * taM) + taA) >> (int) taS);
					uint r = (uint) (((((long) ((lhs.R * y) + (rhs.R * z) + (fR * x))) * taM) + taA) >> (int) taS);
					int a;
					{
						a = lhsA * (255 - rhsA) + 0x80;
						a = ((a >> 8) + a) >> 8;
						a += rhsA;
					}
					ret = b + (g << 8) + (r << 16) + ((uint) a << 24);
				}
				return ColorBgra.FromUInt32 (ret);
			}
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
			public XorBlendOpWithOpacity (int opacity)
			{
				if (opacity < 0 || opacity > 255)
					throw new ArgumentOutOfRangeException (nameof (opacity));

				this.opacity = opacity;
			}
		}
	}
}
