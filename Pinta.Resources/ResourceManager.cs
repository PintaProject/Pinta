// 
// ResourceLoader.cs
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Gdk;

namespace Pinta.Resources
{
	public static class ResourceLoader
	{
		[MethodImpl (MethodImplOptions.NoInlining)]
		public static Pixbuf GetIcon (string name, int size)
		{
			// First see if it's a built-in gtk icon, like gtk-new.
			if (TryGetIconFromTheme (name, size, out var theme_result))
				return theme_result;

			// Otherwise, get it from our embedded resources.
			if (TryGetIconFromResources (name, out var resource_result))
				return resource_result;

			// We can't find this image, but we are going to return *some* image rather than null to prevent crashes
			// Try to return GTK's default "missing image" image
			if (name != Gtk.Stock.MissingImage)
				return GetIcon (Gtk.Stock.MissingImage, size);

			// If even the "missing image" is missing, make one on the fly
			return CreateMissingImage (size);
		}

		public static Stream? GetResourceIconStream (string name)
		{
			var asm = typeof (ResourceLoader).Assembly;

			return asm.GetManifestResourceStream (name);
		}

		private static bool TryGetIconFromTheme (string name, int size, [NotNullWhen (true)] out Pixbuf? image)
		{
			image = null;

			try {
				// This will also load any icons added by Gtk.IconFactory.AddDefault() . 
				using (var icon = Gtk.IconTheme.Default.LookupIcon (name, size, Gtk.IconLookupFlags.ForceSize)) {
					if (icon != null)
						image = icon.LoadIcon ();
				}

			} catch (Exception ex) {
				Console.Error.WriteLine (ex.Message);
			}

			return image != null;
		}

		private static bool TryGetIconFromResources (string name, [NotNullWhen (true)] out Pixbuf? image)
		{
			// Check 'Pinta.Resources' for our image
			if (TryGetIconFromAssembly (Assembly.GetExecutingAssembly (), name, out image))
				return true;

			// Maybe we can find the icon in the resource of a different assembly (e.g. Plugin)
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies ())
				if (TryGetIconFromAssembly (asm, name, out image))
					return true;

			return false;
		}

		private static bool TryGetIconFromAssembly (Assembly assembly, string name, [NotNullWhen (true)] out Pixbuf? image)
		{
			image = null;

			try {
				if (HasResource (assembly, name))
					image = new Pixbuf (assembly, name);
			} catch (Exception ex) {
				Console.Error.WriteLine (ex.Message);
			}

			return image != null;
		}

		private static bool HasResource (Assembly asm, string name)
		{
			return asm.GetManifestResourceNames ().Any (n => n == name);
		}

		// From MonoDevelop:
		// https://github.com/mono/monodevelop/blob/master/main/src/core/MonoDevelop.Ide/gtk-gui/generated.cs
		private static Pixbuf CreateMissingImage (int size)
		{
			using (var surf = new Cairo.ImageSurface (Cairo.Format.Argb32, size, size))
			using (var g = new Cairo.Context (surf)) {
				g.SetSourceColor (new Cairo.Color (1, 1, 1));
				g.Rectangle (0, 0, size, size);
				g.Fill ();

				g.SetSourceColor (new Cairo.Color (0, 0, 0));
				g.Rectangle (0, 0, size - 1, size - 1);
				g.Fill ();

				g.LineWidth = 3;
				g.LineCap = Cairo.LineCap.Round;
				g.LineJoin = Cairo.LineJoin.Round;
				g.SetSourceColor (new Cairo.Color (1, 0, 0));
				g.MoveTo (size / 4, size / 4);
				g.LineTo ((size - 1) - (size / 4), (size - 1) - (size / 4));
				g.MoveTo ((size - 1) - (size / 4), size / 4);
				g.LineTo (size / 4, (size - 1) - (size / 4));

				return new Pixbuf (surf, 0, 0, surf.Width, surf.Height);
			}
		}
	}
}
