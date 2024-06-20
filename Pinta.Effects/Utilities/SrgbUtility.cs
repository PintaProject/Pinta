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

internal static class SrgbUtility
{
	// pre-calculated array of linear intensity for 8bit values
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

	public static double ToSrgb (double linearLevel)
	{
		System.Diagnostics.Debug.Assert ((linearLevel >= 0d && linearLevel <= 1d), "level is out of range 0-1");
		const double power = 1d / 2.4d;
		if (linearLevel <= 0.0031308d)
			return 12.92d * linearLevel;
		else
			return (1.055d * Math.Pow (linearLevel, power)) - 0.055d;
	}

	public static double ToSrgbClamped (double linearLevel)
		=> (linearLevel < 0d) ? 0d : (linearLevel > 1d) ? 1d : ToSrgb (linearLevel);

	public static double ToLinear (byte srgbLevel)
		=> linear_intensity[srgbLevel];

	public static double ToLinear (double srgbLevel)
	{
		const double factor1 = 1d / 12.92d;
		const double factor2 = 1d / 1.055d;
		return
			(srgbLevel <= 0.04045d)
			? srgbLevel * factor1
			: Math.Pow ((srgbLevel + 0.055d) * factor2, 2.4d);
	}
}
