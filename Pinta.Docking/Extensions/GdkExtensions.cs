//
// GtkExtensions.cs
//
// Authors: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (C) 2011 Xamarin Inc.
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
//

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection.Emit;
using System.Reflection;
using Gdk;

namespace Pinta.Docking
{
    static class GdkExtensions
	{
        public static Gdk.Pixbuf WithAlpha (this Gdk.Pixbuf image, double opacity)
        {
            using (var surf = new Cairo.ImageSurface (Cairo.Format.Argb32, image.Width, image.Height)) {
                using (var g = new Cairo.Context (surf)) {
                    CairoHelper.SetSourcePixbuf (g, image, 0, 0);
                    g.PaintWithAlpha (opacity);
                }

                return new Gdk.Pixbuf (surf.Data, true, 8, surf.Width, surf.Height, surf.Stride);
            }
        }

        public static Gdk.Pixbuf WithBoxSize (this Gdk.Pixbuf image, int size)
        {
            return image.ScaleSimple (size, size, InterpType.Bilinear);
        }

        public static Gdk.Pixbuf FromResource (string name)
        {
            using (var s = Assembly.GetExecutingAssembly ().GetManifestResourceStream (name)) {
                if (s == null)
                    throw new InvalidOperationException ("Resource not found: " + name);

                return LoadFromStream (s);
            }
        }

        public static Gdk.Pixbuf LoadFromStream (System.IO.Stream stream)
        {
            using (var loader = new Gdk.PixbufLoader (stream))
                return loader.Pixbuf;
        }

    }
}
