/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Gdk;

namespace Pinta.Core
{
	/// <summary>
	/// Defines a way to operate on a pixel, or a region of pixels, in a binary fashion.
	/// That is, it is a simple function F that takes two parameters and returns a
	/// result of the form: c = F(a, b)
	/// </summary>
	[Serializable]
	public unsafe abstract class BinaryPixelOp : PixelOp
	{
		public abstract ColorBgra Apply (ColorBgra lhs, ColorBgra rhs);

		public unsafe virtual void Apply (ColorBgra* dst, ColorBgra* lhs, ColorBgra* rhs, int length)
		{
			unsafe {
				while (length > 0) {
					*dst = Apply (*lhs, *rhs);
					++dst;
					++lhs;
					++rhs;
					--length;
				}
			}
		}

		/// <summary>
		/// Provides a default implementation for performing dst = F(lhs, rhs) over some rectangle of interest.
		/// </summary>
		/// <param name="dst">The Surface to write pixels to.</param>
		/// <param name="dstOffset">The pixel offset that defines the upper-left of the rectangle-of-interest for the dst Surface.</param>
		/// <param name="lhs">The Surface to read pixels from for the lhs parameter given to the method <b>ColorBgra Apply(ColorBgra, ColorBgra)</b>.</param>
		/// <param name="lhsOffset">The pixel offset that defines the upper-left of the rectangle-of-interest for the lhs Surface.</param>
		/// <param name="rhs">The Surface to read pixels from for the rhs parameter given to the method <b>ColorBgra Apply(ColorBgra, ColorBgra)</b></param>
		/// <param name="rhsOffset">The pixel offset that defines the upper-left of the rectangle-of-interest for the rhs Surface.</param>
		/// <param name="roiSize">The size of the rectangles-of-interest for all Surfaces.</param>
		public void Apply (Cairo.ImageSurface dst, Point dstOffset,
				  Cairo.ImageSurface lhs, Point lhsOffset,
				  Cairo.ImageSurface rhs, Point rhsOffset,
				  Size roiSize)
		{
			// Bounds checking only enabled in Debug builds.
#if DEBUG
			// Create bounding rectangles for each Surface
			Rectangle dstRect = new Rectangle (dstOffset, roiSize);
			Rectangle lhsRect = new Rectangle (lhsOffset, roiSize);
			Rectangle rhsRect = new Rectangle (rhsOffset, roiSize);

			// Clip those rectangles to those Surface's bounding rectangles
			Rectangle dstClip = Rectangle.Intersect (dstRect, dst.GetBounds ());
			Rectangle lhsClip = Rectangle.Intersect (lhsRect, lhs.GetBounds ());
			Rectangle rhsClip = Rectangle.Intersect (rhsRect, rhs.GetBounds ());

			// If any of those Rectangles actually got clipped, then throw an exception
			if (dstRect != dstClip) {
				throw new ArgumentOutOfRangeException ("roiSize", "Destination roi out of bounds");
			}

			if (lhsRect != lhsClip) {
				throw new ArgumentOutOfRangeException ("roiSize", "lhs roi out of bounds");
			}

			if (rhsRect != rhsClip) {
				throw new ArgumentOutOfRangeException ("roiSize", "rhs roi out of bounds");
			}
#endif

			// Cache the width and height properties
			int width = roiSize.Width;
			int height = roiSize.Height;

			// Do the work.
			unsafe {
				for (int row = 0; row < height; ++row) {
					ColorBgra* dstPtr = dst.GetPointAddress (dstOffset.X, dstOffset.Y + row);
					ColorBgra* lhsPtr = lhs.GetPointAddress (lhsOffset.X, lhsOffset.Y + row);
					ColorBgra* rhsPtr = rhs.GetPointAddress (rhsOffset.X, rhsOffset.Y + row);

					Apply (dstPtr, lhsPtr, rhsPtr, width);
				}
			}
		}

		public unsafe override void Apply (ColorBgra* dst, ColorBgra* src, int length)
		{
			unsafe {
				while (length > 0) {
					*dst = Apply (*dst, *src);
					++dst;
					++src;
					--length;
				}
			}
		}

		public override void Apply (Cairo.ImageSurface dst, Point dstOffset, Cairo.ImageSurface src, Point srcOffset, int roiLength)
		{
			Apply (dst.GetPointAddress (dstOffset), src.GetPointAddress (srcOffset), roiLength);
		}

		public void Apply (Cairo.ImageSurface dst, Cairo.ImageSurface src)
		{
			if (dst.GetSize () != src.GetSize ()) {
				throw new ArgumentException ("dst.Size != src.Size");
			}

			unsafe {
				for (int y = 0; y < dst.Height; ++y) {
					ColorBgra* dstPtr = dst.GetRowAddressUnchecked (y);
					ColorBgra* srcPtr = src.GetRowAddressUnchecked (y);
					Apply (dstPtr, srcPtr, dst.Width);
				}
			}
		}

		public void Apply (Cairo.ImageSurface dst, Cairo.ImageSurface lhs, Cairo.ImageSurface rhs)
		{
			if (dst.GetSize () != lhs.GetSize ()) {
				throw new ArgumentException ("dst.Size != lhs.Size");
			}

			if (lhs.GetSize () != rhs.GetSize ()) {
				throw new ArgumentException ("lhs.Size != rhs.Size");
			}

			unsafe {
				for (int y = 0; y < dst.Height; ++y) {
					ColorBgra* dstPtr = dst.GetRowAddressUnchecked (y);
					ColorBgra* lhsPtr = lhs.GetRowAddressUnchecked (y);
					ColorBgra* rhsPtr = rhs.GetRowAddressUnchecked (y);

					Apply (dstPtr, lhsPtr, rhsPtr, dst.Width);
				}
			}
		}

		protected BinaryPixelOp ()
		{
		}
	}
}
