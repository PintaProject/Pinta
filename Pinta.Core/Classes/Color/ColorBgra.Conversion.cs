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
	/// Creates a new ColorBgra instance with the given color and alpha values.
	/// </summary>
	public static ColorBgra FromBgra (byte b, byte g, byte r, byte a)
		=> new (b, g, r, a);

	/// <summary>
	/// Creates a new ColorBgra instance with the given color and alpha values.
	/// </summary>
	public static ColorBgra FromBgraClamped (int b, int g, int r, int a)
		=> FromBgra (
			Utility.ClampToByte (b),
			Utility.ClampToByte (g),
			Utility.ClampToByte (r),
			Utility.ClampToByte (a));

	/// <summary>
	/// Creates a new ColorBgra instance with the given color and alpha values.
	/// </summary>
	public static ColorBgra FromBgraClamped (float b, float g, float r, float a)
		=> FromBgra (
			Utility.ClampToByte (b),
			Utility.ClampToByte (g),
			Utility.ClampToByte (r),
			Utility.ClampToByte (a));

	/// <summary>
	/// Constructs a new ColorBgra instance with the given 32-bit value.
	/// </summary>
	public static ColorBgra FromUInt32 (uint bgra)
		=> new (bgra);

	/// <summary>
	/// Packs color and alpha values into a 32-bit integer.
	/// </summary>
	public static uint BgraToUInt32 (byte b, byte g, byte r, byte a)
		=> b + ((uint) g << 8) + ((uint) r << 16) + ((uint) a << 24);

	/// <summary>
	/// Packs color and alpha values into a 32-bit integer.
	/// </summary>
	public static uint BgraToUInt32 (int b, int g, int r, int a)
		=> (uint) b + ((uint) g << 8) + ((uint) r << 16) + ((uint) a << 24);

	/// <summary>
	/// Creates a new ColorBgra instance with the given color values, and 255 for alpha.
	/// </summary>
	public static ColorBgra FromBgr (byte b, byte g, byte r)
		=> FromBgra (b, g, r, 255);

	/// <summary>
	/// Gets the luminance intensity of the pixel based on the values of the red, green, and blue components. Alpha is ignored.
	/// </summary>
	/// <returns>A value in the range 0 to 1 inclusive.</returns>
	public readonly double GetIntensity ()
		=> ((0.114 * B) + (0.587 * G) + (0.299 * R)) / 255.0;

	/// <summary>
	/// Gets the luminance intensity of the pixel based on the values of the red, green, and blue components. Alpha is ignored.
	/// </summary>
	/// <returns>A value in the range 0 to 255 inclusive.</returns>
	public readonly byte GetIntensityByte ()
		=> (byte) ((7471 * B + 38470 * G + 19595 * R) >> 16);
}
