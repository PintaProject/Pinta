// 
// GdkExtensions.cs
//  
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

using System;
using System.Runtime.InteropServices;
using Pango;

namespace Pinta.Core
{
	// TODO-GTK4 - these need a proper binding in gir.core
	public static class PangoExtensions
	{
		private const string PangoDllName = "libpango-1.0";

		public static FontDescription CreateFontDescription ()
			=> new FontDescription (Pango.Internal.FontDescription.New ());

		public static FontDescription Copy (this FontDescription desc)
			=> new FontDescription (Pango.Internal.FontDescription.Copy (desc.Handle));

		public static void SetWeight (this FontDescription desc, Weight weight)
			=> Pango.Internal.FontDescription.SetWeight (desc.Handle, weight);

		public static void SetStyle (this FontDescription desc, Style style)
			=> Pango.Internal.FontDescription.SetStyle (desc.Handle, style);

		public static void GetCursorPos (this Layout layout, int index, out RectangleI strong_pos, out RectangleI weak_pos)
		{
			InternalGetCursorPos (layout.Handle, index, out var strong_pos_pango, out var weak_pos_pango);
			strong_pos = strong_pos_pango.ToRectangleI ();
			weak_pos = weak_pos_pango.ToRectangleI ();
		}

		[DllImport (PangoDllName, EntryPoint = "pango_layout_get_cursor_pos")]
		private static extern void InternalGetCursorPos (IntPtr layout, int index, out PangoRectangle strong_pos, out PangoRectangle weak_pos);

		public static void GetPixelExtents (this Layout layout, out RectangleI ink_rect, out RectangleI logical_rect)
		{
			InternalGetPixelExtents (layout.Handle, out var ink_rect_pango, out var logical_rect_pango);
			ink_rect = ink_rect_pango.ToRectangleI ();
			logical_rect = logical_rect_pango.ToRectangleI ();
		}

		[DllImport (PangoDllName, EntryPoint = "pango_layout_get_pixel_extents")]
		private static extern void InternalGetPixelExtents (IntPtr layout, out PangoRectangle ink_rect, out PangoRectangle logical_rect);

		public static void IndexToPos (this Layout layout, int index, out RectangleI pos)
		{
			InternalIndexToPos (layout.Handle, index, out var pos_pango);
			pos = pos_pango.ToRectangleI ();
		}

		[DllImport (PangoDllName, EntryPoint = "pango_layout_index_to_pos")]
		private static extern void InternalIndexToPos (IntPtr layout, int index, out PangoRectangle pos);

		public static void XyToIndex (this Layout layout, int x, int y, out int index, out int trailing)
		{
			InternalXyToIndex (layout.Handle, x, y, out index, out trailing);
		}

		[DllImport (PangoDllName, EntryPoint = "pango_layout_xy_to_index")]
		private static extern void InternalXyToIndex (IntPtr layout, int x, int y, out int index, out int trailing);

		[StructLayout (LayoutKind.Sequential)]
		private struct PangoRectangle
		{
			public int X;
			public int Y;
			public int Width;
			public int Height;

			public RectangleI ToRectangleI () => new RectangleI (X, Y, Width, Height);
		}

		public static int UnitsToPixels (int units)
			=> (int) Math.Round (Pango.Functions.UnitsToDouble (units));

		public static int UnitsFromPixels (int pixels)
			=> Pango.Functions.UnitsFromDouble (pixels);
	}
}
