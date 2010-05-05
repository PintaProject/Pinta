/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Runtime.Serialization;
using Gdk;
using System.Collections.Generic;

namespace Pinta.Core
{
    /// <summary>
    /// Defines a surface that is irregularly shaped, defined by a Region.
    /// Works by containing an array of PlacedSurface instances.
    /// Similar to IrregularImage, but works with Surface objects instead.
    /// Instances of this class are immutable once created.
    /// </summary>
    [Serializable]
    public sealed class IrregularSurface
        : ISurfaceDraw,
          IDisposable,
          ICloneable,
          IDeserializationCallback
    {
        private List<PlacedSurface> placedSurfaces;

        [NonSerialized]
        private Region region;

        /// <summary>
        /// The Region that the irregular image fills.
        /// </summary>
        public Region Region
        {
            get
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException("IrregularSurface");
                }

                return this.region;
            }
        }

        /// <summary>
        /// Constructs an IrregularSurface by copying the given region-of-interest from an Image.
        /// </summary>
        /// <param name="source">The Surface to copy pixels from.</param>
        /// <param name="roi">Defines the Region from which to copy pixels from the Image.</param>
        public IrregularSurface (Cairo.ImageSurface source, Region roi)
        {   
            Region roiClipped = (Region)roi.Copy();
            roiClipped.Intersect(Region.Rectangle(source.GetBounds()));

            Rectangle[] rects = roiClipped.GetRectangles();
            this.placedSurfaces = new List<PlacedSurface>(rects.Length);

            foreach (Rectangle rect in rects)
            {
                this.placedSurfaces.Add(new PlacedSurface(source, rect));
            }

            this.region = roiClipped;
        }

        public IrregularSurface(Cairo.ImageSurface source, Rectangle[] roi)
        {
            this.placedSurfaces = new List<PlacedSurface>(roi.Length);

            foreach (Rectangle rect in roi)
            {
                Rectangle ri = Rectangle.Intersect(source.GetBounds(), rect);

                if (!ri.IsEmpty)
                {
                    this.placedSurfaces.Add(new PlacedSurface(source, ri));
                }
            }

            this.region = Utility.RectanglesToRegion(roi);
            this.region.Intersect(Region.Rectangle(source.GetBounds()));
        }

        /// <summary>
        /// Constructs an IrregularSurface by copying the given rectangle-of-interest from an Image.
        /// </summary>
        /// <param name="source">The Surface to copy pixels from.</param>
        /// <param name="roi">Defines the Rectangle from which to copy pixels from the Image.</param>
        public IrregularSurface (Cairo.ImageSurface source, Rectangle roi)
        {
            this.placedSurfaces = new List<PlacedSurface>();
            this.placedSurfaces.Add(new PlacedSurface(source, roi));
            this.region = Region.Rectangle(roi);
        }

        private IrregularSurface (IrregularSurface cloneMe)
        {
            this.placedSurfaces = new List<PlacedSurface>(cloneMe.placedSurfaces.Count);

            foreach (PlacedSurface ps in cloneMe.placedSurfaces)
            {
                this.placedSurfaces.Add((PlacedSurface)ps.Clone());
            }

            this.region = cloneMe.Region.Copy();
        }

        ~IrregularSurface()
        {
            Dispose(false);
        }

        /// <summary>
        /// Draws the IrregularSurface on to the given Surface.
        /// </summary>
        /// <param name="dst">The Surface to draw to.</param>
        public void Draw(Cairo.ImageSurface dst)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("IrregularSurface");
            }

            foreach (PlacedSurface ps in placedSurfaces)
            {
                ps.Draw(dst);
            }
        }

        public void Draw(Cairo.ImageSurface dst, PixelOp pixelOp)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("IrregularSurface");
            }

            foreach (PlacedSurface ps in this.placedSurfaces)
            {
                ps.Draw(dst, pixelOp);
            }
        }

        /// <summary>
        /// Draws the IrregularSurface on to the given Surface starting at the given (x,y) offset.
        /// </summary>
        /// <param name="g">The Surface to draw to.</param>
        /// <param name="transformX">The value to be added to every X coordinate that is used for drawing.</param>
        /// <param name="transformY">The value to be added to every Y coordinate that is used for drawing.</param>
        public void Draw(Cairo.ImageSurface dst, int tX, int tY)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("IrregularSurface");
            }

            foreach (PlacedSurface ps in this.placedSurfaces)
            {
                ps.Draw(dst, tX, tY);
            }
        }

        public void Draw(Cairo.ImageSurface dst, int tX, int tY, PixelOp pixelOp)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("IrregularSurface");
            }

            foreach (PlacedSurface ps in this.placedSurfaces)
            {
                ps.Draw(dst, tX, tY, pixelOp);
            }
        }

        #region IDisposable Members
        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                // TODO: FXCOP: call Dispose() on this.region

                this.disposed = true;

                if (disposing)
                {
                    this.placedSurfaces.Clear();
                    this.placedSurfaces = null;
                }
            }
        }
        #endregion

        #region ICloneable Members

        /// <summary>
        /// Clones the IrregularSurface.
        /// </summary>
        /// <returns>A copy of the current state of this PlacedSurface.</returns>
        public object Clone()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("IrregularSurface");
            }

            return new IrregularSurface(this);
        }
        #endregion

        #region IDeserializationCallback Members

        public void OnDeserialization(object sender)
        {
            region = Region.Rectangle(Rectangle.Zero);

            Rectangle[] rects = new Rectangle[placedSurfaces.Count];

            for (int i = 0; i < placedSurfaces.Count; ++i)
            {
                rects[i] = placedSurfaces[i].Bounds;
            }

            region = Utility.RectanglesToRegion(rects);
        }

        #endregion
    }
}
