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
	[Serializable]
	public unsafe abstract class PixelOp //: IPixelOp
	{
		public PixelOp ()
		{
		}

		/// <summary>
		/// Computes alpha for r OVER l operation.
		/// </summary>
		public static byte ComputeAlpha (byte la, byte ra)
		{
			return (byte)(((la * (256 - (ra + (ra >> 7)))) >> 8) + ra);
		}

		public void Apply (ImageSurface dst, ImageSurface src, Gdk.Rectangle[] rois, int startIndex, int length)
		{
			for (int i = startIndex; i < startIndex + length; ++i) {
				ApplyBase (dst, rois[i].Location, src, rois[i].Location, rois[i].Size);
			}
		}

		public void Apply (ImageSurface dst, Gdk.Point dstOffset, ImageSurface src, Gdk.Point srcOffset, Gdk.Size roiSize)
		{
			ApplyBase (dst, dstOffset, src, srcOffset, roiSize);
		}

		/// <summary>
		/// Provides a default implementation for performing dst = F(dst, src) or F(src) over some rectangle 
		/// of interest. May be slightly faster than calling the other multi-parameter Apply method, as less 
		/// variables are used in the implementation, thus inducing less register pressure.
		/// </summary>
		/// <param name="dst">The Surface to write pixels to, and from which pixels are read and used as the lhs parameter for calling the method <b>ColorBgra Apply(ColorBgra, ColorBgra)</b>.</param>
		/// <param name="dstOffset">The pixel offset that defines the upper-left of the rectangle-of-interest for the dst Surface.</param>
		/// <param name="src">The Surface to read pixels from for the rhs parameter given to the method <b>ColorBgra Apply(ColorBgra, ColorBgra)</b>.</param>
		/// <param name="srcOffset">The pixel offset that defines the upper-left of the rectangle-of-interest for the src Surface.</param>
		/// <param name="roiSize">The size of the rectangles-of-interest for all Surfaces.</param>
		public void ApplyBase (ImageSurface dst, Gdk.Point dstOffset, ImageSurface src, Gdk.Point srcOffset, Gdk.Size roiSize)
		{
			// Create bounding rectangles for each Surface
			Gdk.Rectangle dstRect = new Gdk.Rectangle (dstOffset, roiSize);

			if (dstRect.Width == 0 || dstRect.Height == 0)
				return;

			Gdk.Rectangle srcRect = new Gdk.Rectangle (srcOffset, roiSize);

			if (srcRect.Width == 0 || srcRect.Height == 0)
				return;

			// Clip those rectangles to those Surface's bounding rectangles
			Gdk.Rectangle dstClip = Gdk.Rectangle.Intersect (dstRect, dst.GetBounds ());
			Gdk.Rectangle srcClip = Gdk.Rectangle.Intersect (srcRect, src.GetBounds ());

			// If any of those Rectangles actually got clipped, then throw an exception
			if (dstRect != dstClip)
				throw new ArgumentOutOfRangeException
				(
				    "roiSize",
				    "Destination roi out of bounds" +
				    string.Format (", dst.Size=({0},{1}", dst.Width, dst.Height) +
				    ", dst.Bounds=" + dst.GetBounds ().ToString () +
				    ", dstOffset=" + dstOffset.ToString () +
				    string.Format (", src.Size=({0},{1}", src.Width, src.Height) +
				    ", srcOffset=" + srcOffset.ToString () +
				    ", roiSize=" + roiSize.ToString () +
				    ", dstRect=" + dstRect.ToString () +
				    ", dstClip=" + dstClip.ToString () +
				    ", srcRect=" + srcRect.ToString () +
				    ", srcClip=" + srcClip.ToString ()
				);

			if (srcRect != srcClip)
				throw new ArgumentOutOfRangeException ("roiSize", "Source roi out of bounds");

			// Cache the width and height properties
			int width = roiSize.Width;
			int height = roiSize.Height;

			// Do the work.
			unsafe {
				for (int row = 0; row < height; ++row) {
					ColorBgra* dstPtr = dst.GetPointAddress (dstOffset.X, dstOffset.Y + row);
					ColorBgra* srcPtr = src.GetPointAddress (srcOffset.X, srcOffset.Y + row);
					Apply (dstPtr, srcPtr, width);
				}
			}
		}

		public virtual void Apply (ImageSurface dst, Gdk.Point dstOffset, ImageSurface src, Gdk.Point srcOffset, int scanLength)
		{
			Apply (dst.GetPointAddress (dstOffset), src.GetPointAddress (srcOffset), scanLength);
		}

		public virtual void Apply (ColorBgra* dst, ColorBgra* src, int length)
		{
			throw new System.NotImplementedException ("Derived class must implement Apply(ColorBgra*,ColorBgra*,int)");
		}
	}
}
