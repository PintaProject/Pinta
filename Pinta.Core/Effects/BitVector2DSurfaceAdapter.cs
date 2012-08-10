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
	/// Adapts a Surface class so it can be used as a two dimensional boolean array.
	/// Elements are stored compactly, such that each pixel stores 32 boolean values.
	/// However, the usable width is the same as that of the adapted surface.
	/// (in other words, a surface that is 100 pixels wide can still only store 100
	/// booleans per row)
	/// </summary>
	public unsafe sealed class BitVector2DSurfaceAdapter : IBitVector2D
	{
		private ImageSurface surface;
		private int src_width;
		private int src_height;
		private ColorBgra* src_data_ptr;

		public BitVector2DSurfaceAdapter (ImageSurface surface)
		{
			if (surface == null)
				throw new ArgumentNullException ("surface");

			this.surface = surface;
			src_width = surface.Width;
			src_height = surface.Height;
			src_data_ptr = (ColorBgra*)surface.DataPtr;
		}

		#region Public Properties
		public int Width { get { return src_width; } }
		public int Height { get { return src_height; } }
		public bool IsEmpty { get { return (src_width == 0) || (src_height == 0); } }

		public bool this[Point pt] {
			get { return this[pt.X, pt.Y]; }
			set { this[pt.X, pt.Y] = value; }
		}

		public bool this[int x, int y] {
			get { return Get (x, y); }
			set { Set (x, y, value); }
		}
		#endregion

		#region Public Methods
		public void Clear (bool newValue)
		{
			unsafe {
				uint val = newValue ? 0xffffffff : 0;

				for (int y = 0; y < Height; ++y) {
					ColorBgra* row = surface.GetRowAddressUnchecked (src_data_ptr, src_width, y);

					int w = (this.Width + 31) / 32;

					while (w > 0) {
						row->Bgra = val;
						++row;
						--w;
					}
				}
			}
		}

		public bool Get (int x, int y)
		{
			if (x < 0 || x >= this.Width) {
				throw new ArgumentOutOfRangeException ("x");
			}

			if (y < 0 || y >= this.Height) {
				throw new ArgumentOutOfRangeException ("y");
			}

			return GetUnchecked (x, y);
		}

		public unsafe bool GetUnchecked (int x, int y)
		{
			int cx = x / 32;
			int sx = x % 32;
			uint mask = surface.GetPointAddressUnchecked (src_data_ptr, src_width, cx, y)->Bgra;
			return 0 != (mask & (1 << sx));
		}

		public void Set (int x, int y, bool newValue)
		{
			if (x < 0 || x >= this.Width) {
				throw new ArgumentOutOfRangeException ("x");
			}

			if (y < 0 || y >= this.Height) {
				throw new ArgumentOutOfRangeException ("y");
			}

			SetUnchecked (x, y, newValue);
		}

		public void Set (Point pt, bool newValue)
		{
			Set (pt.X, pt.Y, newValue);
		}

		public void Set (Gdk.Rectangle rect, bool newValue)
		{
			for (int y = rect.Y; y <= rect.GetBottom (); ++y) {
				for (int x = rect.X; x <= rect.GetRight (); ++x) {
					Set (x, y, newValue);
				}
			}
		}

		public void Set (Scanline scan, bool newValue)
		{
			int x = scan.X;

			while (x < scan.X + scan.Length) {
				Set (x, scan.Y, newValue);
				++x;
			}
		}

		//public void Set(PdnRegion region, bool newValue)
		//{
		//    foreach (Rectangle rect in region.GetRegionScansReadOnlyInt())
		//    {
		//        Set(rect, newValue);
		//    }
		//}

		public unsafe void SetUnchecked (int x, int y, bool newValue)
		{
			int cx = x / 32;
			int sx = x % 32;
			ColorBgra* ptr = surface.GetPointAddressUnchecked (src_data_ptr, src_width, cx, y);
			uint mask = ptr->Bgra;
			uint slice = ((uint)1 << sx);
			uint newMask;

			if (newValue) {
				newMask = mask | slice;
			} else {
				newMask = mask & ~slice;
			}

			ptr->Bgra = newMask;
		}

		public void Invert (int x, int y)
		{
			Set (x, y, !Get (x, y));
		}

		public void Invert (Point pt)
		{
			Invert (pt.X, pt.Y);
		}

		public void Invert (Gdk.Rectangle rect)
		{
			for (int y = rect.Y; y <= rect.GetBottom (); ++y) {
				for (int x = rect.X; x <= rect.GetRight (); ++x) {
					Invert (x, y);
				}
			}
		}

		public void Invert (Scanline scan)
		{
			int x = scan.X;

			while (x < scan.X + scan.Length) {
				Invert (x, scan.Y);
				++x;
			}
		}

		//public void Invert(PdnRegion region)
		//{
		//    foreach (Rectangle rect in region.GetRegionScansReadOnlyInt())
		//    {
		//        Invert(rect);
		//    }        
		//}

		public BitVector2DSurfaceAdapter Clone ()
		{
			ImageSurface clonedSurface = this.surface.Clone ();
			return new BitVector2DSurfaceAdapter (clonedSurface);
		}

		object ICloneable.Clone ()
		{
			return Clone ();
		}
		#endregion
	}
}
