/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace Pinta.Core;

/// <summary>
/// This is our pixel format that we will work with. It is always 32-bits / 4-bytes and is
/// always laid out in BGRA order.
/// Generally used with the Surface class.
/// </summary>
[Serializable]
[StructLayout (LayoutKind.Explicit)]
public struct ColorBgra
{
	[FieldOffset (0)]
	public byte B;

	[FieldOffset (1)]
	public byte G;

	[FieldOffset (2)]
	public byte R;

	[FieldOffset (3)]
	public byte A;

	/// <summary>
	/// Lets you change B, G, R, and A at the same time.
	/// </summary>
	[NonSerialized]
	[FieldOffset (0)]
	public uint Bgra;

	public const int BlueChannel = 0;
	public const int GreenChannel = 1;
	public const int RedChannel = 2;
	public const int AlphaChannel = 3;

	public const int SizeOf = 4;

	public static bool TryParseHexString (string hexString, out ColorBgra color)
	{
		try {
			var value = Convert.ToUInt32 (hexString, 16);
			color = FromUInt32 (value);
			return true;
		} catch (Exception) {
			color = Zero;
			return false;
		}
	}

	public readonly string ToHexString ()
	{
		int rgbNumber = (R << 16) | (G << 8) | B;
		string colorString = Convert.ToString (rgbNumber, 16);

		while (colorString.Length < 6) {
			colorString = "0" + colorString;
		}

		string alphaString = Convert.ToString (A, 16);

		while (alphaString.Length < 2) {
			alphaString = "0" + alphaString;
		}

		colorString = alphaString + colorString;

		return colorString.ToUpper ();
	}

	/// <summary>
	/// Gets or sets the byte value of the specified color channel.
	/// </summary>
	public unsafe byte this[int channel] {
		get {
			if (channel < 0 || channel > 3) {
				throw new ArgumentOutOfRangeException (nameof (channel), channel, "valid range is [0,3]");
			}

			fixed (byte* p = &B) {
				return p[channel];
			}
		}

		set {
			if (channel < 0 || channel > 3) {
				throw new ArgumentOutOfRangeException (nameof (channel), channel, "valid range is [0,3]");
			}

			fixed (byte* p = &B) {
				p[channel] = value;
			}
		}
	}

	/// <summary>
	/// Gets the luminance intensity of the pixel based on the values of the red, green, and blue components. Alpha is ignored.
	/// </summary>
	/// <returns>A value in the range 0 to 1 inclusive.</returns>
	public readonly double GetIntensity () => ((0.114 * B) + (0.587 * G) + (0.299 * R)) / 255.0;

	/// <summary>
	/// Gets the luminance intensity of the pixel based on the values of the red, green, and blue components. Alpha is ignored.
	/// </summary>
	/// <returns>A value in the range 0 to 255 inclusive.</returns>
	public readonly byte GetIntensityByte () => (byte) ((7471 * B + 38470 * G + 19595 * R) >> 16);

	/// <summary>
	/// Compares two ColorBgra instance to determine if they are equal.
	/// </summary>
	public static bool operator == (ColorBgra lhs, ColorBgra rhs)
	{
		return lhs.Bgra == rhs.Bgra;
	}

	/// <summary>
	/// Compares two ColorBgra instance to determine if they are not equal.
	/// </summary>
	public static bool operator != (ColorBgra lhs, ColorBgra rhs)
	{
		return lhs.Bgra != rhs.Bgra;
	}

	/// <summary>
	/// Compares two ColorBgra instance to determine if they are equal.
	/// </summary>
	public override readonly bool Equals (object? obj) => obj is ColorBgra bgra && bgra.Bgra == Bgra;

	/// <summary>
	/// Returns a hash code for this color value.
	/// </summary>
	/// <returns></returns>
	public override readonly int GetHashCode () { unchecked { return (int) Bgra; } }

	/// <summary>
	/// Returns a new ColorBgra with the same color values but with a new alpha component value.
	/// </summary>
	public readonly ColorBgra NewAlpha (byte newA) => FromBgra (B, G, R, newA);

	/// <summary>
	/// Creates a new ColorBgra instance with the given color and alpha values.
	/// </summary>
	public static ColorBgra FromBgra (byte b, byte g, byte r, byte a) => new () { Bgra = BgraToUInt32 (b, g, r, a) };

	/// <summary>
	/// Creates a new ColorBgra instance with the given color and alpha values.
	/// </summary>
	public static ColorBgra FromBgraClamped (int b, int g, int r, int a) => FromBgra (
			Utility.ClampToByte (b),
			Utility.ClampToByte (g),
			Utility.ClampToByte (r),
			Utility.ClampToByte (a)
		);

	/// <summary>
	/// Creates a new ColorBgra instance with the given color and alpha values.
	/// </summary>
	public static ColorBgra FromBgraClamped (float b, float g, float r, float a) => FromBgra (
			Utility.ClampToByte (b),
			Utility.ClampToByte (g),
			Utility.ClampToByte (r),
			Utility.ClampToByte (a)
		);

	/// <summary>
	/// Packs color and alpha values into a 32-bit integer.
	/// </summary>
	public static uint BgraToUInt32 (byte b, byte g, byte r, byte a) => b + ((uint) g << 8) + ((uint) r << 16) + ((uint) a << 24);

	/// <summary>
	/// Packs color and alpha values into a 32-bit integer.
	/// </summary>
	public static uint BgraToUInt32 (int b, int g, int r, int a) => (uint) b + ((uint) g << 8) + ((uint) r << 16) + ((uint) a << 24);

	/// <summary>
	/// Creates a new ColorBgra instance with the given color values, and 255 for alpha.
	/// </summary>
	public static ColorBgra FromBgr (byte b, byte g, byte r) => FromBgra (b, g, r, 255);

	/// <summary>
	/// Constructs a new ColorBgra instance with the given 32-bit value.
	/// </summary>
	public static ColorBgra FromUInt32 (uint bgra)
	{
		ColorBgra color = new ColorBgra { Bgra = bgra };
		return color;
	}

	/// <summary>
	/// Smoothly blends between two colors.
	/// NOTE: this assumes unpremultipled alpha!
	/// </summary>
	public static ColorBgra Blend (ColorBgra ca, ColorBgra cb, byte cbAlpha)
	{
		uint caA = Utility.FastScaleByteByByte ((byte) (255 - cbAlpha), ca.A);
		uint cbA = Utility.FastScaleByteByByte (cbAlpha, cb.A);
		uint cbAT = caA + cbA;

		uint r;
		uint g;
		uint b;

		if (cbAT == 0) {
			r = 0;
			g = 0;
			b = 0;
		} else {
			r = ((ca.R * caA) + (cb.R * cbA)) / cbAT;
			g = ((ca.G * caA) + (cb.G * cbA)) / cbAT;
			b = ((ca.B * caA) + (cb.B * cbA)) / cbAT;
		}

		return FromBgra ((byte) b, (byte) g, (byte) r, (byte) cbAT);
	}

	/// <summary>
	/// Linearly interpolates between two color values with premultiplied alpha.
	/// </summary>
	/// <param name="from">The color value that represents 0 on the lerp number line.</param>
	/// <param name="to">The color value that represents 1 on the lerp number line.</param>
	/// <param name="frac">A value in the range [0, 1].</param>
	public static ColorBgra Lerp (ColorBgra from, ColorBgra to, float frac) => FromBgra (
			b: Utility.ClampToByte (Mathematics.Lerp (from.B, to.B, frac)),
			g: Utility.ClampToByte (Mathematics.Lerp (from.G, to.G, frac)),
			r: Utility.ClampToByte (Mathematics.Lerp (from.R, to.R, frac)),
			a: Utility.ClampToByte (Mathematics.Lerp (from.A, to.A, frac))
		);

	/// <summary>
	/// Linearly interpolates between two color values with premultiplied alpha.
	/// </summary>
	/// <param name="from">The color value that represents 0 on the lerp number line.</param>
	/// <param name="to">The color value that represents 1 on the lerp number line.</param>
	/// <param name="frac">A value in the range [0, 1].</param>
	public static ColorBgra Lerp (ColorBgra from, ColorBgra to, double frac) => FromBgra (
			b: Utility.ClampToByte (Mathematics.Lerp (from.B, to.B, frac)),
			g: Utility.ClampToByte (Mathematics.Lerp (from.G, to.G, frac)),
			r: Utility.ClampToByte (Mathematics.Lerp (from.R, to.R, frac)),
			a: Utility.ClampToByte (Mathematics.Lerp (from.A, to.A, frac))
		);

	/// <summary>
	/// Blends four premultiplied colors together based on the given weight values.
	/// </summary>
	/// <returns>The blended color.</returns>
	/// <remarks>
	/// The weights should be 16-bit fixed point numbers that add up to 65536 ("1.0").
	/// 4W16IP means "4 colors, weights, 16-bit integer precision"
	/// </remarks>
	public static ColorBgra BlendColors4W16IP (in ColorBgra c1, uint w1, in ColorBgra c2, uint w2, in ColorBgra c3, uint w3, in ColorBgra c4, uint w4)
	{
#if DEBUG
		if ((w1 + w2 + w3 + w4) != 65536)
			throw new ArgumentException ($"{nameof (w1)} + {nameof (w2)} + {nameof (w3)} + {nameof (w4)} must equal 65536!");
#endif

		const uint ww = 32768;
		uint r = (c1.R * w1 + c2.R * w2 + c3.R * w3 + c4.R * w4 + ww) >> 16;
		uint g = (c1.G * w1 + c2.G * w2 + c3.G * w3 + c4.G * w4 + ww) >> 16;
		uint b = (c1.B * w1 + c2.B * w2 + c3.B * w3 + c4.B * w4 + ww) >> 16;
		uint a = (c1.A * w1 + c2.A * w2 + c3.A * w3 + c4.A * w4 + ww) >> 16;

		return FromBgra ((byte) b, (byte) g, (byte) r, (byte) a);
	}

	/// <summary>
	/// Smoothly blends the given colors together, assuming equal weighting for each one.
	/// It is assumed that pre-multiplied alpha is used.
	/// </summary>
	public static ColorBgra Blend (ReadOnlySpan<ColorBgra> colors)
	{
		int count = colors.Length;
		if (count == 0)
			return Transparent;

		ulong a_sum = 0;
		ulong b_sum = 0;
		ulong g_sum = 0;
		ulong r_sum = 0;
		for (int i = 0; i < count; ++i) {
			a_sum += colors[i].A;
			b_sum += colors[i].B;
			g_sum += colors[i].G;
			r_sum += colors[i].R;
		}

		byte a = (byte) (a_sum / (ulong) count);
		byte b = (byte) (b_sum / (ulong) count);
		byte g = (byte) (g_sum / (ulong) count);
		byte r = (byte) (r_sum / (ulong) count);

		return FromBgra (b, g, r, a);
	}

	public override readonly string ToString () => $"B: {B}, G: {G}, R: {R}, A: {A}";

	/// <summary>
	/// Casts a ColorBgra to a UInt32.
	/// </summary>
	public static explicit operator uint (ColorBgra color) => color.Bgra;

	/// <summary>
	/// Casts a UInt32 to a ColorBgra.
	/// </summary>
	public static explicit operator ColorBgra (uint uint32) => FromUInt32 (uint32);

	/// <summary>
	/// Brings the color channels from straight alpha in premultiplied alpha form.
	/// This is required for direct memory manipulation when writing on Cairo surfaces
	/// as it internally uses the premultiplied alpha form.
	/// See:
	/// https://en.wikipedia.org/wiki/Alpha_compositing
	/// http://cairographics.org/manual/cairo-Image-Surfaces.html#cairo-format-t
	/// </summary>
	/// <returns>A ColorBgra value in premultiplied alpha form</returns>
	public readonly ColorBgra ToPremultipliedAlpha () => FromBgra ((byte) (B * A / 255), (byte) (G * A / 255), (byte) (R * A / 255), A);

	public static bool ColorsWithinTolerance (ColorBgra a, ColorBgra b, int tolerance)
	{
		int diffR = a.R - b.R;
		int diffG = a.G - b.G;
		int diffB = a.B - b.B;
		int diffA = a.A - b.A;

		int summandR = (1 + diffR * diffR) * a.A / 256;
		int summandG = (1 + diffG * diffG) * a.A / 256;
		int summandB = (1 + diffB * diffB) * a.A / 256;
		int summandA = diffA * diffA;

		int sum = summandR + summandG + summandB + summandA;

		return sum <= tolerance * tolerance * 4;
	}

	/// <summary>
	/// Brings the color channels from premultiplied alpha in straight alpha form.
	/// This is required for direct memory manipulation when reading from Cairo surfaces
	/// as it internally uses the premultiplied alpha form.
	/// Note: It is expected that the R,G,B-values are less or equal to the A-values (as it is always the case in premultiplied alpha form)
	/// See:
	/// https://en.wikipedia.org/wiki/Alpha_compositing
	/// http://cairographics.org/manual/cairo-Image-Surfaces.html#cairo-format-t
	/// </summary>
	/// <returns>A ColorBgra value in straight alpha form</returns>
	public readonly ColorBgra ToStraightAlpha ()
	{
		if (A > 0)
			return FromBgra ((byte) (B * 255 / A), (byte) (G * 255 / A), (byte) (R * 255 / A), A);
		else
			return Zero;
	}

	//// Colors: copied from System.Drawing.Color's list (don't worry I didn't type it in
	//// manually, I used a code generator w/ reflection ...)

	public static ColorBgra Transparent => Zero; // Note pre-multiplied alpha is used.
	public static ColorBgra Zero => (ColorBgra) 0;

	public static ColorBgra Black => FromBgra (0, 0, 0, 255);
	public static ColorBgra Blue => FromBgra (255, 0, 0, 255);
	public static ColorBgra Cyan => FromBgra (255, 255, 0, 255);
	public static ColorBgra Green => FromBgra (0, 128, 0, 255);
	public static ColorBgra Magenta => FromBgra (255, 0, 255, 255);
	public static ColorBgra Red => FromBgra (0, 0, 255, 255);
	public static ColorBgra White => FromBgra (255, 255, 255, 255);
	public static ColorBgra Yellow => FromBgra (0, 255, 255, 255);
}
