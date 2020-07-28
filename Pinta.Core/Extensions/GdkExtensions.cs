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

        public static bool IsShiftPressed (this ModifierType m)
        {
            return (m & ModifierType.ShiftMask) == ModifierType.ShiftMask;
        }

		public static bool IsControlPressed (this ModifierType m)
		{
			return (m & ModifierType.ControlMask) == ModifierType.ControlMask;
		}

		public static bool IsAltPressed (this ModifierType m)
		{
			return (m & ModifierType.Mod1Mask) == ModifierType.Mod1Mask;
		}

		public static bool IsShiftPressed (this EventButton ev)
		{
            return ev.State.IsShiftPressed ();
		}

		public static bool IsControlPressed (this EventButton ev)
		{
            return ev.State.IsControlPressed ();
		}

		public static bool IsAltPressed (this EventButton ev)
		{
			return ev.State.IsAltPressed ();
		}

		/// <summary>
		/// Filters out all modifier keys except Ctrl/Shift/Alt. This prevents Caps Lock, Num Lock, etc
		/// from appearing as active modifier keys.
		/// </summary>
		public static ModifierType FilterModifierKeys (this ModifierType current_state)
        {
            var state = Gdk.ModifierType.None;

            state |= (current_state & Gdk.ModifierType.ControlMask);
            state |= (current_state & Gdk.ModifierType.ShiftMask);
            state |= (current_state & Gdk.ModifierType.Mod1Mask);

            return state;
        }

		public static Cairo.PointD GetPoint (this EventButton ev)
		{
			return new Cairo.PointD (ev.X, ev.Y);
		}

		/// <summary>
		/// The implementation of Rectangle.Bottom was changed in 2.12.11 to fix an off-by-one error,
		/// and this function provides the newer behaviour for backwards compatibility with older versions.
		/// </summary>
		public static int GetBottom(this Rectangle r)
		{
			return r.Y + r.Height - 1;
		}

		/// <summary>
		/// The implementation of Rectangle.Right was changed in 2.12.11 to fix an off-by-one error,
		/// and this function provides the newer behaviour for backwards compatibility with older versions.
		/// </summary>
		public static int GetRight(this Rectangle r)
		{
			return r.X + r.Width - 1;
		}

        public static Cairo.Surface ToSurface (this Pixbuf pixbuf)
        {
            var surface = new Cairo.ImageSurface (Cairo.Format.ARGB32, pixbuf.Width, pixbuf.Height);

            using (var g = new Cairo.Context (surface)) {
                Gdk.CairoHelper.SetSourcePixbuf (g, pixbuf, 0, 0);
                g.Paint ();
            }

            return surface;
        }

        public static Pixbuf CreateColorSwatch (int size, Color color)
        {
            using (var pmap = new Pixmap (Screen.Default.RootWindow, size, size))
            using (var gc = new Gdk.GC (pmap)) {
                gc.RgbFgColor = color;
                pmap.DrawRectangle (gc, true, 0, 0, size, size);

                gc.RgbFgColor = new Color (0, 0, 0);
                pmap.DrawRectangle (gc, false, 0, 0, (size - 1), (size - 1));

                return Pixbuf.FromDrawable (pmap, pmap.Colormap, 0, 0, 0, 0, size, size);
            }
        }

        public static Pixbuf CreateTransparentColorSwatch (bool drawBorder)
        {
            var size = 16;

            using (var pmap = new Pixmap (Screen.Default.RootWindow, size, size))
            using (var gc = new Gdk.GC (pmap)) {
                gc.RgbFgColor = new Color (255, 255, 255);
                pmap.DrawRectangle (gc, true, 0, 0, size, size);

                gc.RgbFgColor = new Color (200, 200, 200);
                pmap.DrawRectangle (gc, true, 0, 0, (size / 2), (size / 2));
                pmap.DrawRectangle (gc, true, size / 2, size / 2, (size / 2), (size / 2));

                if (drawBorder) {
                    gc.RgbFgColor = new Color (0, 0, 0);
                    pmap.DrawRectangle (gc, false, 0, 0, (size - 1), (size - 1));
                }

                return Pixbuf.FromDrawable (pmap, pmap.Colormap, 0, 0, 0, 0, size, size);
            }
        }
    }
}
