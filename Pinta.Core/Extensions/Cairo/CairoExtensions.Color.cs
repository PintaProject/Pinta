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

using Cairo;

namespace Pinta.Core;

partial class CairoExtensions
{
	public static void SetSourceColor (
		this Context context,
		Color color
	)
		=> context.SetSourceRgba (
			color.R,
			color.G,
			color.B,
			color.A);

	/// <summary>
	/// Convert from Cairo.Color to ColorBgra.
	/// </summary>
	/// <remarks>This converts from straight to premultiplied alpha.</remarks>
	public static ColorBgra ToColorBgra (this Cairo.Color color)
		=> ColorBgra.FromBgra (
			b: (byte) (color.B * 255),
			g: (byte) (color.G * 255),
			r: (byte) (color.R * 255),
			a: (byte) (color.A * 255)).ToPremultipliedAlpha ();

	/// <summary>
	/// Convert from ColorBgra to Cairo.Color
	/// </summary>
	/// <remarks>This converts from premultiplied to straight alpha.</remarks>
	public static Cairo.Color ToCairoColor (this ColorBgra color)
	{
		ColorBgra sc = color.ToStraightAlpha ();
		return new (
			R: sc.R / 255d,
			G: sc.G / 255d,
			B: sc.B / 255d,
			A: sc.A / 255d);
	}

	public static void AddColorStop (
		this Gradient gradient,
		double offset,
		Color color
	)
		=> gradient.AddColorStopRgba (
			offset,
			color.R,
			color.G,
			color.B,
			color.A);

	public static ImageSurface CreateTransparentColorSwatch (int size, bool drawBorder)
	{
		ImageSurface surface = CreateTransparentBackgroundSurface (size);
		using Context g = new (surface);

		if (drawBorder)
			g.DrawRectangle (new RectangleD (0, 0, size, size), new Color (0, 0, 0), 1);

		return surface;
	}

	public static ImageSurface CreateColorSwatch (
		int size,
		Color color)
	{
		ImageSurface result = CreateImageSurface (Format.Argb32, size, size);
		using Context g = new (result);

		g.FillRectangle (new RectangleD (0, 0, size, size), color);
		g.DrawRectangle (new RectangleD (0, 0, size, size), new Color (0, 0, 0), 1);

		return result;
	}
}
