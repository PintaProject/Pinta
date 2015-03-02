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

namespace MonoDevelop.Components.Docking
{
	internal static class CairoExtensions
	{
        // The Color property is deprecated, so use this extension method until SetSourceColor is officially available everywhere.
        public static void SetSourceColor (this Context g, Color c)
        {
            g.SetSourceRGBA (c.R, c.G, c.B, c.A);
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

	}
}
