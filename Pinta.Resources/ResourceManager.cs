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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Gdk;
using Gtk;

namespace Pinta.Resources;

public static class ResourceLoader
{
	[MethodImpl (MethodImplOptions.NoInlining)]
	public static Texture GetIcon (string name, int size)
	{
		// First see if it's a built-in gtk icon, like gtk-new.
		if (TryGetIconFromTheme (name, size, out var theme_result))
			return theme_result;

		// Otherwise, get it from our embedded resources.
		if (TryGetIconFromResources (name, out var resource_result))
			return resource_result;

		// We can't find this image, but we are going to return *some* image rather than null to prevent crashes
		// Try to return GTK's default "missing image" image
		if (name != StandardIcons.ImageMissing)
			return GetIcon (StandardIcons.ImageMissing, size);

		// If even the "missing image" is missing, make one on the fly
		return CreateMissingImage (size);
	}

	private static bool TryGetIconFromTheme (string name, int size, [NotNullWhen (true)] out Gdk.Texture? image)
	{
		image = null;

		try {
			// This will also load any icons added by Gtk.IconFactory.AddDefault() .
			var icon_theme = Gtk.IconTheme.GetForDisplay (Gdk.Display.GetDefault ()!);
			var icon_paintable = icon_theme.LookupIcon (name, Array.Empty<string> (), size, 1, TextDirection.None, Gtk.IconLookupFlags.Preload);
			if (icon_paintable == null || (name != StandardIcons.ImageMissing && icon_paintable.IconName!.StartsWith ("image-missing")))
				return false;

			var snapshot = Gtk.Snapshot.New ();
			icon_paintable.Snapshot (snapshot, size, size);

			// Render the icon to a texture.
			var node = snapshot.ToNode ();
			if (node == null)
				return false;

			var renderer = Gsk.CairoRenderer.New ();
			renderer.Realize (null);
			image = renderer.RenderTexture (node, null);
			renderer.Unrealize ();
		} catch (Exception ex) {
			Console.Error.WriteLine (ex.Message);
		}

		return image != null;
	}

	private static bool TryGetIconFromResources (string name, [NotNullWhen (true)] out Texture? image)
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

	private static bool TryGetIconFromAssembly (Assembly assembly, string name, [NotNullWhen (true)] out Texture? image)
	{
		image = null;

		try {
			if (HasResource (assembly, name)) {
				using var stream = assembly.GetManifestResourceStream (name)!;
				var buffer = new byte[stream.Length];
				stream.Read (buffer, 0, buffer.Length);

				var bytes = GLib.Bytes.New (buffer);
				image = Gdk.Texture.NewFromBytes (bytes);
			}
		} catch (Exception ex) {
			Console.Error.WriteLine (ex.Message);
			image = null;
		}

		return image != null;
	}

	private static bool HasResource (Assembly asm, string name)
	{
		return asm.GetManifestResourceNames ().Any (n => n == name);
	}

	// From MonoDevelop:
	// https://github.com/mono/monodevelop/blob/master/main/src/core/MonoDevelop.Ide/gtk-gui/generated.cs
	private static Texture CreateMissingImage (int size)
	{
		var surf = new Cairo.ImageSurface (Cairo.Format.Argb32, size, size);
		var g = new Cairo.Context (surf);
		g.SetSourceRgb (1, 1, 1);
		g.Rectangle (0, 0, size, size);
		g.Fill ();

		g.SetSourceRgb (0, 0, 0);
		g.Rectangle (0, 0, size - 1, size - 1);
		g.Fill ();

		g.LineWidth = 3;
		g.LineCap = Cairo.LineCap.Round;
		g.LineJoin = Cairo.LineJoin.Round;
		g.SetSourceRgb (1, 0, 0);
		g.MoveTo (size / 4, size / 4);
		g.LineTo ((size - 1) - (size / 4), (size - 1) - (size / 4));
		g.MoveTo ((size - 1) - (size / 4), size / 4);
		g.LineTo (size / 4, (size - 1) - (size / 4));

		return Texture.NewForPixbuf (Gdk.Functions.PixbufGetFromSurface (surf, 0, 0, surf.Width, surf.Height)!);
	}
}
