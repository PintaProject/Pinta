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
using System.IO;
using Gdk;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Pinta.Resources
{
	public static class ResourceLoader
	{

		private static bool HasResource(Assembly asm, string name)
		{
			string[] resources = asm.GetManifestResourceNames ();

			if (Array.IndexOf (resources, name) > -1)
				return true;
			else
				return false;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Pixbuf GetIcon (string name, int size)
		{
			Gdk.Pixbuf result = null;
			try {
                // First see if it's a built-in gtk icon, like gtk-new.
                // This will also load any icons added by Gtk.IconFactory.AddDefault() . 
                using (var icon = Gtk.IconTheme.Default.LookupIcon(name, size, Gtk.IconLookupFlags.ForceSize))
                {
					if (icon != null)
                        result = icon.LoadIcon();
                }
                // Otherwise, get it from our embedded resources.
                if (result == null) {

					if (HasResource(Assembly.GetExecutingAssembly(), name)) //Assembly.GetCallingAssembly() is wrong here!
						result = Gdk.Pixbuf.LoadFromResource (name);
				}

				//Maybe we can find the icon in the resource of a different assembly (e.g. Plugin)
				if (result == null) {
					foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
					{
						if (HasResource(asm, name))
							result = new Pixbuf(asm, name);
					}
				}
			}
			catch (Exception ex) {
				System.Console.Error.WriteLine (ex.Message);
			}

			// Ensure that we don't crash if an icon is missing for some reason.
			if (result == null) {
				try
				{
					// Try to return gtk's default missing image
					if (name != Gtk.Stock.MissingImage) {
						result = GetIcon (Gtk.Stock.MissingImage, size);
					} else {
						// If gtk is missing it's "missing image", we'll create one on the fly
						result = CreateMissingImage (size);
					}
				}
				catch (Exception ex) {
					System.Console.Error.WriteLine (ex.Message);
				}
			}

			return result;
		}

		public static Stream GetResourceIconStream (string name)
		{
			var ass = typeof (Pinta.Resources.ResourceLoader).Assembly;

			return ass.GetManifestResourceStream (name);
		}

		// From MonoDevelop:
		// https://github.com/mono/monodevelop/blob/master/main/src/core/MonoDevelop.Ide/gtk-gui/generated.cs
		private static Pixbuf CreateMissingImage (int size)
		{
			using (var surf = new Cairo.ImageSurface(Cairo.Format.Argb32, size, size))
			using (var g = new Cairo.Context(surf))
            {
				g.SetSourceColor(new Cairo.Color(1, 1, 1));
				g.Rectangle(0, 0, size, size);
				g.Fill();

				g.SetSourceColor(new Cairo.Color(0, 0, 0));
				g.Rectangle(0, 0, size - 1, size - 1);
				g.Fill();

				g.LineWidth = 3;
				g.LineCap = Cairo.LineCap.Round;
				g.LineJoin = Cairo.LineJoin.Round;
				g.SetSourceColor(new Cairo.Color(1, 0, 0));
				g.MoveTo(size / 4, size / 4);
				g.LineTo((size - 1) - (size / 4), (size - 1) - (size / 4));
				g.MoveTo((size - 1) - (size / 4), size / 4);
				g.LineTo(size / 4, (size - 1) - (size / 4));

				return new Gdk.Pixbuf(surf, 0, 0, surf.Width, surf.Height);
			}
		}

		private static Gtk.IconSize GetIconSize(int size)
		{
			switch (size) {
				case 16:
					return Gtk.IconSize.SmallToolbar;
				case 32:
					return Gtk.IconSize.Dnd;
				default:
					return Gtk.IconSize.Invalid;
			}
		}
	}
}
