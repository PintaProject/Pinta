/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;

namespace Pinta.Core
{
	/// <summary>
	/// Encapsulates a surface ("what") along with a pixel offset ("where") which
	/// defines where the surface would be drawn on to another surface.
	/// Instances of this object are immutable -- once you create it, you can not
	/// change it.
	/// </summary>
	[Serializable]
	public sealed class PlacedSurface : ISurfaceDraw, ICloneable
	{
		Gdk.Point where;
		ImageSurface what;

		#region Constructors
		private PlacedSurface (PlacedSurface ps)
		{
			where = ps.Where;
			what = ps.What.Clone ();
		}

		private PlacedSurface ()
		{
		}

		public PlacedSurface (ImageSurface source, Gdk.Rectangle roi)
		{
			where = roi.Location;
			what = new ImageSurface (Format.Argb32, roi.Width, roi.Height);

			using (Context g = new Context (what)) {
				g.SetSourceSurface (source, -roi.X, -roi.Y);
				g.Paint ();
			}
		}
		#endregion

		#region Public Properties
		public Gdk.Point Where {
			get {
				if (disposed)
					throw new ObjectDisposedException ("PlacedSurface");

				return where;
			}
		}

		public ImageSurface What {
			get {
				if (disposed)
					throw new ObjectDisposedException ("PlacedSurface");

				return what;
			}
		}

		public Gdk.Size Size {
			get {
				if (disposed)
					throw new ObjectDisposedException ("PlacedSurface");

				return new Gdk.Size (what.Width, what.Height);
			}
		}

		public Gdk.Rectangle Bounds {
			get {
				if (disposed)
					throw new ObjectDisposedException ("PlacedSurface");

				return new Gdk.Rectangle (Where, Size);
			}
		}
		#endregion

		#region Public Methods
		public void Draw (ImageSurface dst)
		{
			if (disposed)
				throw new ObjectDisposedException ("PlacedSurface");

			using (Cairo.Context g = new Cairo.Context (dst)) {
				g.Save ();

				Rectangle r = what.GetBounds ().ToCairoRectangle ();

				// We need to use the source operator to fully replace the old
				// data.  Or else we may paint transparent on top of it and 
				// it will still be visible.  [Bug #670411]
				using (Path p = g.CreateRectanglePath (new Rectangle (where.X, where.Y, r.Width, r.Height))) {
					g.AppendPath (p);
					g.Clip ();
					g.Operator = Operator.Source;
					g.DrawPixbuf (what.ToPixbuf (), new Cairo.Point (where.X, where.Y));
				}

				g.Restore ();
			}
		}

		public void Draw (ImageSurface dst, PixelOp pixelOp)
		{
			if (disposed)
				throw new ObjectDisposedException ("PlacedSurface");

			Gdk.Rectangle dstRect = Bounds;
			Gdk.Rectangle dstClip = Gdk.Rectangle.Intersect (dstRect, dst.GetBounds ());

			if (dstClip.Width > 0 && dstClip.Height > 0) {
				int dtX = dstClip.X - where.X;
				int dtY = dstClip.Y - where.Y;

				pixelOp.Apply (dst, dstClip.Location, what, new Gdk.Point (dtX, dtY), dstClip.Size);
			}
		}

		public void Draw (ImageSurface dst, int tX, int tY)
		{
			if (disposed)
				throw new ObjectDisposedException ("PlacedSurface");

			Gdk.Point oldWhere = where;

			try {
				where.X += tX;
				where.Y += tY;
				Draw (dst);
			} finally {
				where = oldWhere;
			}
		}

		public void Draw (ImageSurface dst, int tX, int tY, PixelOp pixelOp)
		{
			if (disposed)
				throw new ObjectDisposedException ("PlacedSurface");

			Gdk.Point oldWhere = where;

			try {
				where.X += tX;
				where.Y += tY;
				Draw (dst, pixelOp);
			} finally {
				where = oldWhere;
			}
		}
		#endregion

		#region IDisposable Members
		private bool disposed = false;
		#endregion

		#region ICloneable Members
		public object Clone ()
		{
			if (disposed)
				throw new ObjectDisposedException ("PlacedSurface");

			return new PlacedSurface (this);
		}
		#endregion
	}
}

