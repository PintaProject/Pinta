/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;

namespace Pinta.Core
{
    /// <summary>
    /// Defines a way to operate on a pixel, or a region of pixels, in a unary fashion.
    /// That is, it is a simple function F that takes one parameter and returns a
    /// result of the form: d = F(c)
    /// </summary>
    [Serializable]
    public unsafe abstract class UnaryPixelOp
        : PixelOp
    {
        public abstract ColorBgra Apply(ColorBgra color);

        public unsafe override void Apply(ColorBgra *dst, ColorBgra *src, int length)
        {
            unsafe
            {
                while (length > 0)
                {
                    *dst = Apply(*src);
                    ++dst;
                    ++src;
                    --length;
                }
            }
        }

        public unsafe virtual void Apply(ColorBgra* ptr, int length)
        {
            unsafe
            {
                while (length > 0)
                {
                    *ptr = Apply(*ptr);
                    ++ptr;
                    --length;
                }
            }
        }

//        private unsafe void ApplyRectangle(Surface surface, Rectangle rect)
//        {
//            for (int y = rect.Top; y < rect.Bottom; ++y)
//            {
//                ColorBgra *ptr = surface.GetPointAddress(rect.Left, y);
//                Apply(ptr, rect.Width);
//            }
//        }
//
//        public void Apply(Surface surface, Rectangle[] roi, int startIndex, int length)
//        {
//            Rectangle regionBounds = Utility.GetRegionBounds(roi, startIndex, length);
//
//            if (regionBounds != Rectangle.Intersect(surface.Bounds, regionBounds))
//            {
//                throw new ArgumentOutOfRangeException("roi", "Region is out of bounds");
//            }
//
//            unsafe
//            {
//                for (int x = startIndex; x < startIndex + length; ++x)
//                {
//                    ApplyRectangle(surface, roi[x]);
//                }
//            }
//        }
//
//        public void Apply(Surface surface, Rectangle[] roi)
//        {
//            Apply(surface, roi, 0, roi.Length);
//        }
//
//        public void Apply(Surface surface, RectangleF[] roiF, int startIndex, int length)
//        {
//            Rectangle regionBounds = Rectangle.Truncate(Utility.GetRegionBounds(roiF, startIndex, length));
//
//            if (regionBounds != Rectangle.Intersect(surface.Bounds, regionBounds))
//            {
//                throw new ArgumentOutOfRangeException("roiF", "Region is out of bounds");
//            }
//
//            unsafe
//            {
//                for (int x = startIndex; x < startIndex + length; ++x)
//                {
//                    ApplyRectangle(surface, Rectangle.Truncate(roiF[x]));
//                }
//            }
//        }
//
//        public void Apply(Surface surface, RectangleF[] roiF)
//        {
//            Apply(surface, roiF, 0, roiF.Length);
//        }
//
//        public unsafe void Apply(Surface surface, Rectangle roi)
//        {
//            ApplyRectangle(surface, roi);
//        }
//
//        public void Apply(Surface surface, Scanline scan)
//        {
//            Apply(surface.GetPointAddress(scan.X, scan.Y), scan.Length);
//        }
//
//        public void Apply(Surface surface, Scanline[] scans)
//        {
//            foreach (Scanline scan in scans)
//            {
//                Apply(surface, scan);
//            }
//        }
//
//        public override void Apply(Surface dst, Point dstOffset, Surface src, Point srcOffset, int scanLength)
//        {
//            Apply(dst.GetPointAddress(dstOffset), src.GetPointAddress(srcOffset), scanLength);
//        }
//
//        public void Apply(Surface dst, Surface src, Rectangle roi)
//        {
//            for (int y = roi.Top; y < roi.Bottom; ++y)
//            {
//                ColorBgra *dstPtr = dst.GetPointAddress(roi.Left, y);
//                ColorBgra *srcPtr = src.GetPointAddress(roi.Left, y);
//                Apply(dstPtr, srcPtr, roi.Width);
//            }
//        }
//
//        public void Apply(Surface surface, PdnRegion roi)
//        {
//            Apply(surface, roi.GetRegionScansReadOnlyInt());
//        }

        public UnaryPixelOp()
        {
        }
    }
}
