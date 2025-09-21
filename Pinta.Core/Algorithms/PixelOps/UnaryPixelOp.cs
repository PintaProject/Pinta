/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;

namespace Pinta.Core;

/// <summary>
/// Defines a way to operate on a pixel, or a region of pixels, in a unary fashion.
/// That is, it is a simple function F that takes one parameter and returns a
/// result of the form: d = F(c)
/// </summary>
[Serializable]
public abstract class UnaryPixelOp : PixelOp
{
	public UnaryPixelOp ()
	{
	}

	public abstract ColorBgra Apply (in ColorBgra color);

	public override void Apply (Span<ColorBgra> dst, ReadOnlySpan<ColorBgra> src)
	{
		//Debug.Assert (dst.Length == src.Length);
		for (int i = 0; i < src.Length; ++i)
			dst[i] = Apply (src[i]);
	}

	public virtual void Apply (Span<ColorBgra> dst)
	{
		for (int i = 0; i < dst.Length; ++i)
			dst[i] = Apply (dst[i]);
	}

	private void ApplyRectangle (ImageSurface surface, RectangleI rect)
	{
		var data = surface.GetPixelData ();
		int width = surface.Width;
		for (int y = rect.Top; y <= rect.Bottom; ++y) {
			Apply (data.Slice (y * width + rect.Left, rect.Width));
		}
	}

	public void Apply (ImageSurface surface, ReadOnlySpan<RectangleI> roi, int startIndex, int length)
	{
		RectangleI regionBounds = Utility.GetRegionBounds (roi, startIndex, length);

		if (regionBounds != RectangleI.Intersect (surface.GetBounds (), regionBounds))
			throw new ArgumentOutOfRangeException (nameof (roi), "Region is out of bounds");

		for (int x = startIndex; x < startIndex + length; ++x)
			ApplyRectangle (surface, roi[x]);
	}

	public void Apply (ImageSurface surface, ReadOnlySpan<RectangleI> roi)
	{
		Apply (surface, roi, 0, roi.Length);
	}

	public void Apply (ImageSurface surface, RectangleI roi)
	{
		ApplyRectangle (surface, roi);
	}

	public void Apply (ImageSurface dst, ImageSurface src, RectangleI roi)
	{
		var src_data = src.GetReadOnlyPixelData ();
		var dst_data = dst.GetPixelData ();
		int src_width = src.Width;
		int dst_width = dst.Width;

		for (int y = roi.Y; y <= roi.Bottom; ++y) {
			Apply (dst_data.Slice (y * dst_width + roi.X, roi.Width),
			      src_data.Slice (y * src_width + roi.X, roi.Width));
		}
	}

	public void Apply (ImageSurface dst, ImageSurface src, ReadOnlySpan<RectangleI> rois)
	{
		foreach (RectangleI roi in rois)
			Apply (dst, src, roi);
	}
}
