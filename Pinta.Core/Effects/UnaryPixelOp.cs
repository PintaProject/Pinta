/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;
using Cairo;

namespace Pinta.Core
{
	/// <summary>
	/// Defines a way to operate on a pixel, or a region of pixels, in a unary fashion.
	/// That is, it is a simple function F that takes one parameter and returns a
	/// result of the form: d = F(c)
	/// </summary>
	[Serializable]
	public unsafe abstract class UnaryPixelOp : PixelOp
	{
		public UnaryPixelOp ()
		{
		}

		public abstract ColorBgra Apply (ColorBgra color);

		public unsafe override void Apply (ColorBgra* dst, ColorBgra* src, int length)
		{
			unsafe {
				while (length > 0) {
					*dst = Apply (*src);
					++dst;
					++src;
					--length;
				}
			}
		}

		public unsafe virtual void Apply (ColorBgra* ptr, int length)
		{
			unsafe {
				while (length > 0) {
					*ptr = Apply (*ptr);
					++ptr;
					--length;
				}
			}
		}

		private unsafe void ApplyRectangle (ImageSurface surface, Gdk.Rectangle rect)
		{
			for (int y = rect.Left; y <= rect.GetBottom (); ++y) {
				ColorBgra* ptr = surface.GetPointAddress (rect.Left, y);
				Apply (ptr, rect.Width);
			}
		}

		public void Apply (ImageSurface surface, Gdk.Rectangle[] roi, int startIndex, int length)
		{
			Gdk.Rectangle regionBounds = Utility.GetRegionBounds (roi, startIndex, length);

			if (regionBounds != Gdk.Rectangle.Intersect (surface.GetBounds (), regionBounds))
				throw new ArgumentOutOfRangeException ("roi", "Region is out of bounds");

			unsafe {
				for (int x = startIndex; x < startIndex + length; ++x)
					ApplyRectangle (surface, roi[x]);
			}
		}

		public void Apply (ImageSurface surface, Gdk.Rectangle[] roi)
		{
			Apply (surface, roi, 0, roi.Length);
		}

		public unsafe void Apply (ImageSurface surface, Gdk.Rectangle roi)
		{
			ApplyRectangle (surface, roi);
		}

		public override void Apply (ImageSurface dst, Gdk.Point dstOffset, ImageSurface src, Gdk.Point srcOffset, int scanLength)
		{
			Apply (dst.GetPointAddress (dstOffset), src.GetPointAddress (srcOffset), scanLength);
		}
		
		public void Apply (ImageSurface dst, ImageSurface src, Gdk.Rectangle roi)
		{
			ColorBgra* src_data_ptr = (ColorBgra*)src.DataPtr;
			int src_width = src.Width;
			ColorBgra* dst_data_ptr = (ColorBgra*)dst.DataPtr;
			int dst_width = dst.Width;

			for (int y = roi.Y; y <= roi.GetBottom(); ++y) {
				ColorBgra* dstPtr = dst.GetPointAddressUnchecked (dst_data_ptr, dst_width, roi.X, y);
				ColorBgra* srcPtr = src.GetPointAddressUnchecked (src_data_ptr, src_width, roi.X, y);
				Apply (dstPtr, srcPtr, roi.Width);
			}
		}

		public void Apply (ImageSurface dst, ImageSurface src, Gdk.Rectangle[] rois)
		{
			foreach (Gdk.Rectangle roi in rois)
				Apply (dst, src, roi);
		}
	}
}
