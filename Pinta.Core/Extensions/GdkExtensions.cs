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
using Gdk;

namespace Pinta.Core
{
	public static class GdkExtensions
	{
		// Invalidate the whole thing
		public static void Invalidate (this Window w)
		{
			int width;
			int height;
			
			w.GetSize (out width, out height);
			
			w.InvalidateRect (new Rectangle (0, 0, width, height), true);
		}
		
		public static Rectangle GetBounds (this Window w)
		{
			int width;
			int height;
			
			w.GetSize (out width, out height);
			
			return new Rectangle (0, 0, width, height);
		}

		public static Size GetSize (this Window w)
		{
			int width;
			int height;

			w.GetSize (out width, out height);

			return new Size (width, height);
		}
		
		public static Cairo.Color ToCairoColor (this Gdk.Color color)
		{
			return new Cairo.Color ((double)color.Red / ushort.MaxValue, (double)color.Green / ushort.MaxValue, (double)color.Blue / ushort.MaxValue);
		}
		
		public static Cairo.Color GetCairoColor (this Gtk.ColorSelection selection) 
		{
			Cairo.Color cairo_color = selection.CurrentColor.ToCairoColor ();
			return new Cairo.Color (cairo_color.R, cairo_color.G, cairo_color.B, (double)selection.CurrentAlpha / ushort.MaxValue);
		}
		
		public static Gdk.Point Center (this Gdk.Rectangle rect)
		{
			return new Gdk.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
		}

		public static ColorBgra ToBgraColor (this Gdk.Color color)
		{
			return ColorBgra.FromBgr ((byte)(color.Blue * 255 / ushort.MaxValue),  (byte)(color.Green * 255 / ushort.MaxValue), (byte)(color.Red * 255 / ushort.MaxValue));
		}
		
		public static bool IsNotSet (this Point p)
		{
			return p.X == int.MinValue && p.Y == int.MinValue;
		}
	}
}
