/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution                      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

// Copyright (c) 2007, 2008 Ed Harvey 
//
// MIT License: http://www.opensource.org/licenses/mit-license.php
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions: 
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software. 
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
// THE SOFTWARE. 
//

using System;
using System.Collections.Immutable;

namespace Pinta.Effects;

/// <summary>
/// A static utility class for converting color values between sRGB and linear color spaces.
/// </summary>
/// <remarks>
/// Human eyes don't perceive light linearly. They are more sensitive to changes in dark tones than in bright ones.
/// sRGB is a color space designed to mimic this non-linear perception.
/// </remarks>
internal static class SrgbUtility
{
	/// <summary>
	/// Pre-calculated lookup table that stores the linear intensity for each possible 8-bit sRGB value (0-255).
	/// Reading from this array is much faster than performing the complex `ToLinear` calculation every time.
	/// </summary>
	private static readonly ImmutableArray<double> linear_intensity = CalculateLinearIntensities ();
	private static ImmutableArray<double> CalculateLinearIntensities ()
	{
		var linearIntensity = ImmutableArray.CreateBuilder<double> ();
		linearIntensity.Count = 256;
		for (int i = 0; i <= 255; i++) {
			double x = i / 255d;
			linearIntensity[i] = ToLinear (x);
		}
		return linearIntensity.MoveToImmutable ();
	}

	/// <summary>
	/// Converts a color channel value from linear RGB to sRGB space.
	/// </summary>
	/// <param name="linearLevel">Value in linear space, between 0 and 1</param>
	/// <exception cref="ArgumentOutOfRangeException" />
	public static double ToSrgb (double linearLevel)
	{
		if (linearLevel < 0d || linearLevel > 1d)
			throw new ArgumentOutOfRangeException (nameof (linearLevel));

		const double POWER = 1d / 2.4d;
		return
			(linearLevel <= 0.0031308d)
			? 12.92d * linearLevel
			: (1.055d * Math.Pow (linearLevel, POWER)) - 0.055d;
	}

	/// <summary>
	/// A "safe" version of ToSrgb that clamps the input value to the valid 0.0-1.0 range
	/// instead of throwing an exception.
	/// </summary>
	public static double ToSrgbClamped (double linearLevel)
	{
		if (linearLevel < 0d) return 0d;
		if (linearLevel > 1d) return 1d;
		return ToSrgb (linearLevel);
	}

	/// <summary>
	/// Converts an 8-bit sRGB color channel value to its linear equivalent using the pre-calculated lookup table.
	/// </summary>
	public static double ToLinear (byte srgbLevel)
		=> linear_intensity[srgbLevel];

	/// <summary>
	/// Converts a floating-point sRGB color channel value to its linear equivalent.
	/// </summary>
	public static double ToLinear (double srgbLevel)
	{
		const double FACTOR_1 = 1d / 12.92d;
		const double FACTOR_2 = 1d / 1.055d;
		return
			(srgbLevel <= 0.04045d)
			? srgbLevel * FACTOR_1
			: Math.Pow ((srgbLevel + 0.055d) * FACTOR_2, 2.4d);
	}
}
