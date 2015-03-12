// 
// CairoExtensions.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2015 Jonathan Pobst
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
using Cairo;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Pinta.Docking
{
	static class CairoExtensions
	{
        private const string CairoLib = "libcairo-2.dll";

        [DllImport (CairoLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void cairo_set_source (IntPtr cr, IntPtr pattern);

        public static void DrawPixbuf (this Context g, Gdk.Pixbuf pixbuf, double x, double y)
        {
            g.Save ();

            Gdk.CairoHelper.SetSourcePixbuf (g, pixbuf, x, y);
            g.Paint ();
            g.Restore ();
        }

        public static void DrawImage (this Cairo.Context s, Gtk.Widget widget, Gdk.Pixbuf image, double x, double y)
        {
            s.DrawPixbuf (image, x, y);
        }

        // The Color property is deprecated, so use this extension method until SetSourceColor is officially available everywhere.
        public static void SetSourceColor (this Context g, Color c)
        {
            g.SetSourceRGBA (c.R, c.G, c.B, c.A);
        }

        /// <summary>
        /// The Pattern property is now deprecated in favour of the SetSource (pattern) method,
        /// but that method doesn't exist in older versions of Mono.Cairo. This extension method
        /// provides an implementation of that functionality.
        ///
        /// This can be removed once we port to GTK3.
        /// </summary>
        public static void SetSource (this Context g, Pattern source)
        {
#pragma warning disable 612
            cairo_set_source (g.Handle, source.Pointer);
#pragma warning restore 612
        }

        public static Cairo.Color ToCairoColor (this Gdk.Color color)
        {
            return new Cairo.Color ((double)color.Red / ushort.MaxValue, (double)color.Green / ushort.MaxValue, (double)color.Blue / ushort.MaxValue);
        }

        public static Cairo.Color ParseColor (string s, double alpha = 1)
        {
            if (s.StartsWith ("#"))
                s = s.Substring (1);
            if (s.Length == 3)
                s = "" + s[0] + s[0] + s[1] + s[1] + s[2] + s[2];
            double r = ((double)int.Parse (s.Substring (0, 2), System.Globalization.NumberStyles.HexNumber)) / 255;
            double g = ((double)int.Parse (s.Substring (2, 2), System.Globalization.NumberStyles.HexNumber)) / 255;
            double b = ((double)int.Parse (s.Substring (4, 2), System.Globalization.NumberStyles.HexNumber)) / 255;
            return new Cairo.Color (r, g, b, alpha);
        }

        public static Gdk.Color ToGdkColor (this Cairo.Color color)
        {
            return new Gdk.Color ((byte)(color.R * 255d), (byte)(color.G * 255d), (byte)(color.B * 255d));
        }

        public static Cairo.Color ToCairoColor (this Xwt.Drawing.Color color)
        {
            return new Cairo.Color (color.Red, color.Green, color.Blue, color.Alpha);
        }

        public static Xwt.Drawing.Color ToXwtColor (this Cairo.Color color)
        {
            return new Xwt.Drawing.Color (color.R, color.G, color.B, color.A);
        }

        public static Xwt.Drawing.Color ToXwtColor (this Gdk.Color color)
        {
            return new Xwt.Drawing.Color ((double)color.Red / ushort.MaxValue,
                (double)color.Green / ushort.MaxValue,
                (double)color.Blue / ushort.MaxValue);
        }

        public static void Line (this Cairo.Context cr, double x1, double y1, double x2, double y2)
        {
            cr.MoveTo (x1, y1);
            cr.LineTo (x2, y2);
        }

        public static Cairo.Rectangle ToCairoRect (this Gdk.Rectangle rect)
        {
            return new Cairo.Rectangle (rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static Cairo.Color MultiplyAlpha (this Cairo.Color self, double alpha)
        {
            return new Cairo.Color (self.R, self.G, self.B, self.A * alpha);
        }

        public static void HsbFromColor (Cairo.Color color, out double hue,
            out double saturation, out double brightness)
        {
            double min, max, delta;
            double red = color.R;
            double green = color.G;
            double blue = color.B;

            hue = 0;
            saturation = 0;
            brightness = 0;

            if (red > green) {
                max = Math.Max (red, blue);
                min = Math.Min (green, blue);
            } else {
                max = Math.Max (green, blue);
                min = Math.Min (red, blue);
            }

            brightness = (max + min) / 2;

            if (Math.Abs (max - min) < 0.0001) {
                hue = 0;
                saturation = 0;
            } else {
                saturation = brightness <= 0.5
                    ? (max - min) / (max + min)
                    : (max - min) / (2 - max - min);

                delta = max - min;

                if (red == max) {
                    hue = (green - blue) / delta;
                } else if (green == max) {
                    hue = 2 + (blue - red) / delta;
                } else if (blue == max) {
                    hue = 4 + (red - green) / delta;
                }

                hue *= 60;
                if (hue < 0) {
                    hue += 360;
                }
            }
        }

        private static double Modula (double number, double divisor)
        {
            return ((int)number % divisor) + (number - (int)number);
        }

        public static Cairo.Color ColorFromHsb (double hue, double saturation, double brightness)
        {
            int i;
            double[] hue_shift = { 0, 0, 0 };
            double[] color_shift = { 0, 0, 0 };
            double m1, m2, m3;

            m2 = brightness <= 0.5
                ? brightness * (1 + saturation)
                : brightness + saturation - brightness * saturation;

            m1 = 2 * brightness - m2;

            hue_shift[0] = hue + 120;
            hue_shift[1] = hue;
            hue_shift[2] = hue - 120;

            color_shift[0] = color_shift[1] = color_shift[2] = brightness;

            i = saturation == 0 ? 3 : 0;

            for (; i < 3; i++) {
                m3 = hue_shift[i];

                if (m3 > 360) {
                    m3 = Modula (m3, 360);
                } else if (m3 < 0) {
                    m3 = 360 - Modula (Math.Abs (m3), 360);
                }

                if (m3 < 60) {
                    color_shift[i] = m1 + (m2 - m1) * m3 / 60;
                } else if (m3 < 180) {
                    color_shift[i] = m2;
                } else if (m3 < 240) {
                    color_shift[i] = m1 + (m2 - m1) * (240 - m3) / 60;
                } else {
                    color_shift[i] = m1;
                }
            }

            return new Cairo.Color (color_shift[0], color_shift[1], color_shift[2]);
        }

        public static Cairo.Color ColorShade (Cairo.Color @base, double ratio)
        {
            double h, s, b;

            HsbFromColor (@base, out h, out s, out b);

            b = Math.Max (Math.Min (b * ratio, 1), 0);
            s = Math.Max (Math.Min (s * ratio, 1), 0);

            Cairo.Color color = ColorFromHsb (h, s, b);
            color.A = @base.A;
            return color;
        }

	}
}
