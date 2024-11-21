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

using System.Runtime.InteropServices;
using Cairo;

namespace Pinta.Core;

partial class CairoExtensions
{
	// TODO-GTK4 (bindings) - remove once gir.core has improved bindings for Cairo.Region (https://github.com/gircore/gir.core/pull/621)
	[StructLayout (LayoutKind.Sequential)]
	private struct CairoRectangleInt
	{
		public int X;
		public int Y;
		public int Width;
		public int Height;

		public readonly RectangleI ToRectangleI () => new (X, Y, Width, Height);
	}

	[DllImport (CAIRO_LIBRARY_NAME, EntryPoint = "cairo_region_create_rectangle")]
	private static extern Cairo.Internal.RegionOwnedHandle RegionCreateRectangle (ref CairoRectangleInt rect);

	[LibraryImport (CAIRO_LIBRARY_NAME, EntryPoint = "cairo_region_contains_point")]
	[return: MarshalAs (UnmanagedType.Bool)]
	private static partial bool RegionContainsPoint (Cairo.Internal.RegionHandle handle, int x, int y);

	[LibraryImport (CAIRO_LIBRARY_NAME, EntryPoint = "cairo_region_xor")]
	private static partial Status RegionXor (Cairo.Internal.RegionHandle handle, Cairo.Internal.RegionHandle other);

	[LibraryImport (CAIRO_LIBRARY_NAME, EntryPoint = "cairo_region_num_rectangles")]
	private static partial int RegionNumRectangles (Cairo.Internal.RegionHandle handle);

	[LibraryImport (CAIRO_LIBRARY_NAME, EntryPoint = "cairo_region_get_rectangle")]
	private static partial int RegionGetRectangle (Cairo.Internal.RegionHandle handle, int i, out CairoRectangleInt rect);
}
