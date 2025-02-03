/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Reflection;
using Cairo;

namespace Pinta.Core;

public static class Utility
{
	internal static bool IsNumber (float x)
		=> x >= float.MinValue && x <= float.MaxValue;

	public static byte ClampToByte (double x)
		=> (byte) Math.Clamp (x, byte.MinValue, byte.MaxValue);

	public static byte ClampToByte (float x)
		=> (byte) Math.Clamp (x, byte.MinValue, byte.MaxValue);

	public static byte ClampToByte (int x)
		=> (byte) Math.Clamp (x, byte.MinValue, byte.MaxValue);

	public static double MagnitudeSquared (PointD p)
		=> Mathematics.MagnitudeSquared (p.X, p.Y);

	public static double Magnitude (this PointD point)
		=> Mathematics.Magnitude (point.X, point.Y);

	public static double Magnitude (this PointI point)
		=> Mathematics.Magnitude<double> (point.X, point.Y);

	public static double Distance (this PointD origin, in PointD dest)
		=> Magnitude (origin - dest);

	public static PointD Lerp (
		PointD from,
		PointD to,
		float frac
	)
		=> new (
			X: Mathematics.Lerp (from.X, to.X, frac),
			Y: Mathematics.Lerp (from.Y, to.Y, frac));

	public static void Swap<T> (
		ref T a,
		ref T b) where T : struct
	{
		T t;

		t = a;
		a = b;
		b = t;
	}

	/// <summary>
	/// Allows you to find the bounding box for a "region" that is described as an
	/// array of bounding boxes.
	/// </summary>
	/// <param name="rects">The "region" you want to find a bounding box for.</param>
	/// <param name="startIndex">Index of the first rectangle in the array to examine.</param>
	/// <param name="length">Number of rectangles to examine, beginning at <b>startIndex</b>.</param>
	/// <returns>A rectangle that surrounds the region.</returns>
	public static RectangleI GetRegionBounds (ReadOnlySpan<RectangleI> rects, int startIndex, int length)
	{
		if (rects.Length == 0)
			return RectangleI.Zero;

		RectangleI unionsAggregate = rects[startIndex];

		for (int i = startIndex + 1; i < startIndex + length; ++i)
			unionsAggregate = unionsAggregate.Union (rects[i]);

		return unionsAggregate;
	}

	public static int ColorDifferenceSquared (ColorBgra a, ColorBgra b)
	{
		int r_diff = a.R - b.R;
		int g_diff = a.G - b.G;
		int b_diff = a.B - b.B;

		int diffSq =
			  (r_diff * r_diff)
			+ (g_diff * g_diff)
			+ (b_diff * b_diff);

		return diffSq / 3;
	}

	public static string GetStaticName (Type type)
	{
		var pi = type.GetProperty ("StaticName", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty);

		if (pi != null)
			return (pi.GetValue (null, null) as string) ?? type.Name;

		return type.Name;
	}

	public static byte FastScaleByteByByte (byte a, byte b)
	{
		int r1 = a * b + 0x80;
		int r2 = ((r1 >> 8) + r1) >> 8;
		return (byte) r2;
	}

	public static IReadOnlyList<PointD> GetRgssOffsets (int sampleCount, int quality)
	{
		if (sampleCount < 1)
			throw new ArgumentOutOfRangeException (nameof (sampleCount), $"{nameof (sampleCount)} must be [0, int.MaxValue]");

		if (sampleCount != quality * quality)
			throw new ArgumentException ($"{nameof (sampleCount)} != ({nameof (quality)} * {nameof (quality)})");

		if (sampleCount == 1)
			return [PointD.Zero];

		var result = new PointD[sampleCount];
		for (int i = 0; i < sampleCount; ++i) {
			double y = (i + 1d) / (sampleCount + 1d);
			double baseX = y * quality;
			double x = baseX - Math.Truncate (baseX);
			result[i] = new PointD (x - 0.5d, y - 0.5d);
		}
		return result;
	}

	public static int FastDivideShortByByte (ushort n, byte d)
	{
		int i = d * 3;
		uint m = mas_table[i];
		uint a = mas_table[i + 1];
		uint s = mas_table[i + 2];

		uint nTimesMPlusA = unchecked((n * m) + a);
		uint shifted = nTimesMPlusA >> (int) s;
		int r = (int) shifted;

		return r;
	}

	// i = z * 3;
	// (x / z) = ((x * masTable[i]) + masTable[i + 1]) >> masTable[i + 2)
	private static readonly uint[] mas_table =
	[
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
		0x80808081, 0x00000000, 39, // 255
	];

	/// <summary>Gets the nearest step angle in radians.</summary>
	/// 
	/// <returns>The nearest step angle in radians.</returns>
	/// 
	/// <param name="angle">Angle in radians.</param>
	/// <param name="steps">Number of steps to divide the circle.</param>
	/// 
	public static RadiansAngle GetNearestStepAngle (RadiansAngle angle, int steps)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan (steps, 1);

		const double FULL_TURN = RadiansAngle.MAX_RADIANS;
		double stepAngle = FULL_TURN / steps;
		double sector = Math.Round (angle.Radians / stepAngle) * stepAngle;

		return new (sector);
	}

	/// <summary>
	/// Checks if all of the pixels in the row match the specified color.
	/// </summary>
	private static bool IsConstantRow (ImageSurface surf, Cairo.Color color, RectangleI rect, int y)
	{
		for (int x = rect.Left; x < rect.Right; ++x)
			if (!color.Equals (surf.GetColorBgra (new (x, y)).ToCairoColor ()))
				return false;

		return true;
	}

	/// <summary>
	/// Checks if all of the pixels in the column (within the bounds of the rectangle) match the specified color.
	/// </summary>
	private static bool IsConstantColumn (ImageSurface surf, Cairo.Color color, RectangleI rect, int x)
	{
		for (int y = rect.Top; y < rect.Bottom; ++y)
			if (!color.Equals (surf.GetColorBgra (new (x, y)).ToCairoColor ()))
				return false;

		return true;
	}

	public static RectangleI GetObjectBounds (ImageSurface image, RectangleI? searchArea = null)
	{
		// Use the entire image bounds by default, or restrict to the provided search area.
		RectangleI rect = searchArea ?? image.GetBounds ();
		// Get the background color from the top-left pixel of the rectangle
		Color borderColor = image.GetColorBgra (new PointI (rect.Left, rect.Top)).ToCairoColor ();

		// Top down.
		for (int y = rect.Top; y < rect.Bottom; ++y) {
			if (!IsConstantRow (image, borderColor, rect, y))
				break;

			rect = rect with { Y = rect.Y + 1, Height = rect.Height - 1 };
		}

		// Bottom up.
		for (int y = rect.Bottom - 1; y >= rect.Top; --y) {
			if (!IsConstantRow (image, borderColor, rect, y))
				break;

			rect = rect with { Height = rect.Height - 1 };
		}

		// Left side.
		for (int x = rect.Left; x < rect.Right; ++x) {
			if (!IsConstantColumn (image, borderColor, rect, x))
				break;

			rect = rect with { X = rect.X + 1, Width = rect.Width - 1 };
		}

		// Right side.
		for (int x = rect.Right - 1; x >= rect.Left; --x) {
			if (!IsConstantColumn (image, borderColor, rect, x))
				break;

			rect = rect with { Width = rect.Width - 1 };
		}

		return rect;
	}
}
