// Author:
//       Jonathan Pobst <monkey@jpobst.com>
//
// Copyright (c) 2010 Jonathan Pobst
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

// Some functions are from Paint.NET:

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;

namespace Pinta.Core;

partial class CairoExtensions
{
	public static void BlendSurface (
		this Context g,
		Surface src,
		BlendMode mode = BlendMode.Normal,
		double opacity = 1.0)
	{
		g.Save ();

		g.SetBlendMode (mode);
		g.SetSourceSurface (src, 0, 0);
		g.PaintWithAlpha (opacity);

		g.Restore ();
	}

	public static void BlendSurface (
		this Context g,
		Surface src,
		RectangleD roi,
		BlendMode mode = BlendMode.Normal,
		double opacity = 1.0)
	{
		g.Save ();

		g.Rectangle (roi);
		g.Clip ();
		g.SetBlendMode (mode);
		g.SetSourceSurface (src, 0, 0);
		g.PaintWithAlpha (opacity);

		g.Restore ();
	}
	public static void BlendSurface (
		this Context g,
		Surface src,
		PointD offset,
		BlendMode mode = BlendMode.Normal,
		double opacity = 1.0)
	{
		g.Save ();

		g.Translate (offset.X, offset.Y);
		g.SetBlendMode (mode);
		g.SetSourceSurface (src, 0, 0);
		g.PaintWithAlpha (opacity);

		g.Restore ();
	}

	public static void SetBlendMode (
		this Context g,
		BlendMode mode)
	{
		g.Operator = GetBlendModeOperator (mode);
	}

	private static Operator GetBlendModeOperator (BlendMode mode)
		=> mode switch {
			BlendMode.Normal => Operator.Over,
			BlendMode.Multiply => (Operator) ExtendedOperators.Multiply,
			BlendMode.ColorBurn => (Operator) ExtendedOperators.ColorBurn,
			BlendMode.ColorDodge => (Operator) ExtendedOperators.ColorDodge,
			BlendMode.HardLight => (Operator) ExtendedOperators.HardLight,
			BlendMode.SoftLight => (Operator) ExtendedOperators.SoftLight,
			BlendMode.Overlay => (Operator) ExtendedOperators.Overlay,
			BlendMode.Difference => (Operator) ExtendedOperators.Difference,
			BlendMode.Color => (Operator) ExtendedOperators.HslColor,
			BlendMode.Luminosity => (Operator) ExtendedOperators.HslLuminosity,
			BlendMode.Hue => (Operator) ExtendedOperators.HslHue,
			BlendMode.Saturation => (Operator) ExtendedOperators.HslSaturation,
			BlendMode.Lighten => (Operator) ExtendedOperators.Lighten,
			BlendMode.Darken => (Operator) ExtendedOperators.Darken,
			BlendMode.Screen => (Operator) ExtendedOperators.Screen,
			BlendMode.Xor => Operator.Xor,
			_ => throw new ArgumentOutOfRangeException (nameof (mode)),
		};

	private static Status Xor (this Region region, Region other)
		=> RegionXor (region.Handle, other.Handle);

	public enum ExtendedOperators
	{
		Clear = 0,

		Source = 1,
		SourceOver = 2,
		SourceIn = 3,
		SourceOut = 4,
		SourceAtop = 5,

		Destination = 6,
		DestinationOver = 7,
		DestinationIn = 8,
		DestinationOut = 9,
		DestinationAtop = 10,

		Xor = 11,
		Add = 12,
		Saturate = 13,

		Multiply = 14,
		Screen = 15,
		Overlay = 16,
		Darken = 17,
		Lighten = 18,
		ColorDodge = 19,
		ColorBurn = 20,
		HardLight = 21,
		SoftLight = 22,
		Difference = 23,
		Exclusion = 24,
		HslHue = 25,
		HslSaturation = 26,
		HslColor = 27,
		HslLuminosity = 28,
	}
}
