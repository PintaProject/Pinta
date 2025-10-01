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
using System.Runtime.InteropServices;
using Cairo;

namespace Pinta.Core;

partial class CairoExtensions
{
	public static ColorBgra GetBilinearSample (
		this ImageSurface src,
		float x,
		float y
	)
		=> GetBilinearSample (
			src,
			src.GetReadOnlyPixelData (),
			src.Width,
			src.Height,
			x,
			y);

	public static ColorBgra GetBilinearSample (
		this ImageSurface src,
		ReadOnlySpan<ColorBgra> src_data,
		int srcWidth,
		int srcHeight,
		float x,
		float y)
	{
		if (!Utility.IsNumber (x) || !Utility.IsNumber (y))
			return ColorBgra.Transparent;

		float u = x;
		float v = y;

		if (u < 0 || v < 0 || u >= srcWidth || v >= srcHeight)
			return ColorBgra.FromUInt32 (0);

		unchecked {
			int iu = (int) Math.Floor (u);
			uint sxfrac = (uint) (256 * (u - iu));
			uint sxfracinv = 256 - sxfrac;

			int iv = (int) Math.Floor (v);
			uint syfrac = (uint) (256 * (v - iv));
			uint syfracinv = 256 - syfrac;

			uint wul = sxfracinv * syfracinv;
			uint wur = sxfrac * syfracinv;
			uint wll = sxfracinv * syfrac;
			uint wlr = sxfrac * syfrac;

			int sx = iu;
			int sy = iv;
			int sleft = sx;
			int sright =
				sleft == (srcWidth - 1)
				? sleft
				: sleft + 1;

			int stop = sy;
			int sbottom =
				stop == (srcHeight - 1)
				? stop
				: stop + 1;

			ColorBgra cul = src.GetColorBgra (src_data, srcWidth, new (sleft, stop));
			ColorBgra cur = src.GetColorBgra (src_data, srcWidth, new (sright, stop));
			ColorBgra cll = src.GetColorBgra (src_data, srcWidth, new (sleft, sbottom));
			ColorBgra clr = src.GetColorBgra (src_data, srcWidth, new (sright, sbottom));

			return ColorBgra.BlendColors4W16IP (cul, wul, cur, wur, cll, wll, clr, wlr);
		}
	}

	public static ColorBgra GetBilinearSampleClamped (
		this ImageSurface src,
		float x,
		float y
	)
		=> GetBilinearSampleClamped (
			src,
			src.GetReadOnlyPixelData (),
			src.Width,
			src.Height,
			x,
			y);

	public static ColorBgra GetBilinearSampleClamped (
		this ImageSurface src,
		ReadOnlySpan<ColorBgra> src_data,
		int srcWidth,
		int srcHeight,
		float x,
		float y)
	{
		if (!Utility.IsNumber (x) || !Utility.IsNumber (y))
			return ColorBgra.Transparent;

		float u = Math.Clamp (x, 0, srcWidth - 1);
		float v = Math.Clamp (y, 0, srcHeight - 1);

		unchecked {
			int iu = (int) Math.Floor (u);
			uint sxfrac = (uint) (256 * (u - iu));
			uint sxfracinv = 256 - sxfrac;

			int iv = (int) Math.Floor (v);
			uint syfrac = (uint) (256 * (v - iv));
			uint syfracinv = 256 - syfrac;

			uint wul = sxfracinv * syfracinv;
			uint wur = sxfrac * syfracinv;
			uint wll = sxfracinv * syfrac;
			uint wlr = sxfrac * syfrac;

			int sx = iu;
			int sy = iv;
			int sleft = sx;
			int sright =
				sleft == (srcWidth - 1)
				? sleft
				: sleft + 1;

			int stop = sy;
			int sbottom =
				stop == (srcHeight - 1)
				? stop
				: stop + 1;

			ColorBgra cul = src.GetColorBgra (src_data, srcWidth, new (sleft, stop));
			ColorBgra cur = src.GetColorBgra (src_data, srcWidth, new (sright, stop));
			ColorBgra cll = src.GetColorBgra (src_data, srcWidth, new (sleft, sbottom));
			ColorBgra clr = src.GetColorBgra (src_data, srcWidth, new (sright, sbottom));

			return ColorBgra.BlendColors4W16IP (cul, wul, cur, wur, cll, wll, clr, wlr);
		}
	}

	public static ColorBgra GetBilinearSampleWrapped (
		this ImageSurface src,
		float x,
		float y
	)
		=> GetBilinearSampleWrapped (
			src,
			src.GetReadOnlyPixelData (),
			src.Width,
			src.Height,
			x,
			y);

	public static ColorBgra GetBilinearSampleWrapped (
		this ImageSurface src,
		ReadOnlySpan<ColorBgra> src_data,
		int srcWidth,
		int srcHeight,
		float x,
		float y)
	{
		if (!Utility.IsNumber (x) || !Utility.IsNumber (y))
			return ColorBgra.Transparent;

		float u = x;
		float v = y;

		unchecked {
			int iu = (int) Math.Floor (u);
			uint sxfrac = (uint) (256 * (u - iu));
			uint sxfracinv = 256 - sxfrac;

			int iv = (int) Math.Floor (v);
			uint syfrac = (uint) (256 * (v - iv));
			uint syfracinv = 256 - syfrac;

			uint wul = sxfracinv * syfracinv;
			uint wur = sxfrac * syfracinv;
			uint wll = sxfracinv * syfrac;
			uint wlr = sxfrac * syfrac;

			int sx;
			if (iu < 0)
				sx = srcWidth - 1 + ((iu + 1) % srcWidth);
			else if (iu > (srcWidth - 1))
				sx = iu % srcWidth;
			else
				sx = iu;


			int sy;
			if (iv < 0)
				sy = srcHeight - 1 + ((iv + 1) % srcHeight);
			else if (iv > (srcHeight - 1))
				sy = iv % srcHeight;
			else
				sy = iv;


			int sleft = sx;
			int sright =
				sleft == (srcWidth - 1)
				? 0
				: sleft + 1;
			int stop = sy;
			int sbottom =
				stop == (srcHeight - 1)
				? 0
				: stop + 1;

			ColorBgra cul = src.GetColorBgra (src_data, srcWidth, new (sleft, stop));
			ColorBgra cur = src.GetColorBgra (src_data, srcWidth, new (sright, stop));
			ColorBgra cll = src.GetColorBgra (src_data, srcWidth, new (sleft, sbottom));
			ColorBgra clr = src.GetColorBgra (src_data, srcWidth, new (sright, sbottom));

			return ColorBgra.BlendColors4W16IP (cul, wul, cur, wur, cll, wll, clr, wlr);
		}
	}

	public static ColorBgra GetBilinearSampleReflected (
		this ImageSurface src,
		ReadOnlySpan<ColorBgra> src_data,
		int srcWidth,
		int srcHeight,
		float x,
		float y
	) => src.GetBilinearSampleClamped (
		src_data,
		srcWidth,
		srcHeight,
		ReflectCoord (x, srcWidth),
		ReflectCoord (y, srcHeight));

	public static ColorBgra GetBilinearSampleReflected (
		this ImageSurface src,
		float x,
		float y
	) => GetBilinearSampleClamped (
		src,
		ReflectCoord (x, src.Width),
		ReflectCoord (y, src.Height));

	private static float ReflectCoord (float value, int max)
	{
		bool reflection = false;

		while (value < 0) {
			value += max;
			reflection = !reflection;
		}

		while (value > max) {
			value -= max;
			reflection = !reflection;
		}

		if (reflection)
			value = max - value;

		return value;
	}

	/// <summary>
	/// Prefer using the variant which takes the surface data and width, for improved performance
	/// if there are repeated calls in a loop.
	/// </summary>
	public static ref readonly ColorBgra GetColorBgra (
		this ImageSurface surf,
		PointI position)
	{
		return ref surf.GetColorBgra (
			surf.GetReadOnlyPixelData (),
			surf.Width,
			position);
	}

	// This isn't really an extension method, since it doesn't use
	// the passed in argument, but it's nice to have the same calling
	// convention as the uncached version.  If you can use this one
	// over the other, it is much faster in tight loops (like effects).
	public static ref readonly ColorBgra GetColorBgra (
		this ImageSurface surf,
		ReadOnlySpan<ColorBgra> data,
		int width,
		PointI position)
	{
		return ref data[width * position.Y + position.X];
	}

	/// <summary>
	/// Access the image surface's data as a read-only span of ColorBgra pixels.
	/// </summary>
	public static ReadOnlySpan<ColorBgra> GetReadOnlyPixelData (this ImageSurface surface)
		=> surface.GetPixelData ();

	/// <summary>
	/// Access the image surface's data as a span of ColorBgra pixels.
	/// </summary>
	public static Span<ColorBgra> GetPixelData (this ImageSurface surface)
		=> MemoryMarshal.Cast<byte, ColorBgra> (surface.GetData ());


	public static GdkPixbuf.Pixbuf ToPixbuf (this ImageSurface sourceSurface, bool includeAlpha = true)
	{
		int width = sourceSurface.Width;
		int height = sourceSurface.Height;

		if (includeAlpha) {
			return Gdk.Functions.PixbufGetFromSurface (sourceSurface, 0, 0, width, height)!;
		}

		// If we need a pixbuf without alpha, it's easiest to convert from a temporary RGB cairo surface.
		using Cairo.ImageSurface rgbSurface = new (Format.Rgb24, width, height);
		using Cairo.Context context = new (rgbSurface);
		context.SetSourceSurface (sourceSurface, 0.0, 0.0);
		context.Paint ();

		return Gdk.Functions.PixbufGetFromSurface (rgbSurface, 0, 0, width, height)!;
	}
}
