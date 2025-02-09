/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace Pinta.Core;

partial struct ColorBgra
{
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
	public static ColorBgra Lerp (ColorBgra from, ColorBgra to, float frac)
		=> FromBgra (
			b: Utility.ClampToByte (Mathematics.Lerp (from.B, to.B, frac)),
			g: Utility.ClampToByte (Mathematics.Lerp (from.G, to.G, frac)),
			r: Utility.ClampToByte (Mathematics.Lerp (from.R, to.R, frac)),
			a: Utility.ClampToByte (Mathematics.Lerp (from.A, to.A, frac)));

	/// <summary>
	/// Linearly interpolates between two color values with premultiplied alpha.
	/// </summary>
	/// <param name="from">The color value that represents 0 on the lerp number line.</param>
	/// <param name="to">The color value that represents 1 on the lerp number line.</param>
	/// <param name="frac">A value in the range [0, 1].</param>
	public static ColorBgra Lerp (ColorBgra from, ColorBgra to, double frac)
		=> FromBgra (
			b: Utility.ClampToByte (Mathematics.Lerp (from.B, to.B, frac)),
			g: Utility.ClampToByte (Mathematics.Lerp (from.G, to.G, frac)),
			r: Utility.ClampToByte (Mathematics.Lerp (from.R, to.R, frac)),
			a: Utility.ClampToByte (Mathematics.Lerp (from.A, to.A, frac)));

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

	/// <summary>
	/// Returns a new ColorBgra with the same color values but with a new alpha component value.
	/// </summary>
	public readonly ColorBgra NewAlpha (byte newA) => FromBgra (B, G, R, newA);

	/// <summary>
	/// Brings the color channels from straight alpha in premultiplied alpha form.
	/// This is required for direct memory manipulation when writing on Cairo surfaces
	/// as it internally uses the premultiplied alpha form.
	/// See:
	/// https://en.wikipedia.org/wiki/Alpha_compositing
	/// http://cairographics.org/manual/cairo-Image-Surfaces.html#cairo-format-t
	/// </summary>
	/// <returns>A ColorBgra value in premultiplied alpha form</returns>
	public readonly ColorBgra ToPremultipliedAlpha ()
		=> FromBgra ((byte) (B * A / 255), (byte) (G * A / 255), (byte) (R * A / 255), A);

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
}
