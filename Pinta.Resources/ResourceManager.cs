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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cairo;
using GLib;

namespace Pinta.Resources;

public static class ResourceLoader
{
	[MethodImpl (MethodImplOptions.NoInlining)]
	public static Gdk.Texture GetIcon (string name, int size)
	{
		// First see if it's a built-in gtk icon, like gtk-new.
		try {
			Gdk.Texture? theme_result = GetIconFromTheme (name, size);
			if (theme_result is not null) return theme_result;
		} catch (Exception ex) {
			Console.Error.WriteLine (ex.Message);
		}

		// Otherwise, get it from our embedded resources.
		Gdk.Texture? resource_result = GetIconFromResources (name);
		if (resource_result is not null)
			return resource_result;

		// We can't find this image, but we are going to return *some* image rather than null to prevent crashes
		// Try to return GTK's default "missing image" image
		if (name != StandardIcons.ImageMissing)
			return GetIcon (StandardIcons.ImageMissing, size);

		// If even the "missing image" is missing, make one on the fly
		return CreateMissingImage (size);
	}

	public static void LoadCssStyles ()
	{
		try {
			Bytes? bytes = GetBytesFromAssembly (Assembly.GetExecutingAssembly (), "style.css");
			if (bytes is null) return;
			Gtk.CssProvider cssProvider = Gtk.CssProvider.New ();
			cssProvider.LoadFromBytes (bytes);
			Gdk.Display? display = Gdk.Display.GetDefault ();
			if (display is null) return;
			Gtk.StyleContext.AddProviderForDisplay (display, cssProvider, Gtk.Constants.STYLE_PROVIDER_PRIORITY_APPLICATION);
		} catch (Exception ex) {
			Console.Error.WriteLine (ex.Message);
		}
	}

	private static Gdk.Texture? GetIconFromTheme (string name, int size)
	{
		// This will also load any icons added by Gtk.IconFactory.AddDefault() .
		Gtk.IconTheme iconTheme = Gtk.IconTheme.GetForDisplay (Gdk.Display.GetDefault ()!);
		Gtk.IconPaintable iconPaintable = iconTheme.LookupIcon (name, [], size, 1, Gtk.TextDirection.None, Gtk.IconLookupFlags.Preload);

		if (iconPaintable is null) return null;

		if (name != StandardIcons.ImageMissing && iconPaintable.IconName!.StartsWith ("image-missing", StringComparison.InvariantCulture))
			return null;

		Gtk.Snapshot snapshot = Gtk.Snapshot.New ();
		iconPaintable.Snapshot (snapshot, size, size);

		// Render the icon to a texture.

		Gsk.RenderNode? node = snapshot.ToNode ();
		if (node is null) return null;
		Gsk.CairoRenderer renderer = Gsk.CairoRenderer.New ();
		try {
			renderer.Realize (null);
			return renderer.RenderTexture (node, null);
		} finally {
			renderer.Unrealize ();
		}
	}

	private static Gdk.Texture? GetIconFromResources (string name)
	{
		// Check 'Pinta.Resources' for our image
		Gdk.Texture? fromExecutingAssembly = GetIconFromAssembly (Assembly.GetExecutingAssembly (), name);
		if (fromExecutingAssembly is not null)
			return fromExecutingAssembly;

		// Maybe we can find the icon in the resource of a different assembly (e.g. Plugin)
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies ()) {
			Gdk.Texture? fromTargetedAssembly = GetIconFromAssembly (assembly, name);
			if (fromTargetedAssembly is not null)
				return fromTargetedAssembly;
		}

		return null;
	}

	private static Gdk.Texture? GetIconFromAssembly (Assembly assembly, string name)
	{
		try {
			Bytes? bytes = GetBytesFromAssembly (assembly, name);
			if (bytes is null) return null;
			return Gdk.Texture.NewFromBytes (bytes);
		} catch (Exception ex) {
			Console.Error.WriteLine (ex.Message);
			return null;
		}
	}

	private static Bytes? GetBytesFromAssembly (Assembly assembly, string name)
	{
		if (!HasResource (assembly, name))
			return null;

		using Stream stream = assembly.GetManifestResourceStream (name)!;
		byte[] buffer = new byte[stream.Length];
		stream.ReadExactly (buffer);

		return Bytes.New (buffer);
	}

	private static bool HasResource (Assembly assembly, string name)
	{
		return
			assembly
			.GetManifestResourceNames ()
			.Any (n => n == name);
	}

	// From MonoDevelop:
	// https://github.com/mono/monodevelop/blob/master/main/src/core/MonoDevelop.Ide/gtk-gui/generated.cs
	private static Gdk.Texture CreateMissingImage (int size)
	{
		using ImageSurface surf = new (Format.Argb32, size, size);
		using Context g = new (surf);

		g.SetSourceRgb (1, 1, 1);
		g.Rectangle (0, 0, size, size);
		g.Fill ();

		g.SetSourceRgb (0, 0, 0);
		g.Rectangle (0, 0, size - 1, size - 1);
		g.Fill ();

		g.LineWidth = 3;
		g.LineCap = LineCap.Round;
		g.LineJoin = LineJoin.Round;
		g.SetSourceRgb (1, 0, 0);
		g.MoveTo (size / 4, size / 4);
		g.LineTo ((size - 1) - (size / 4), (size - 1) - (size / 4));
		g.MoveTo ((size - 1) - (size / 4), size / 4);
		g.LineTo (size / 4, (size - 1) - (size / 4));

		return Gdk.Texture.NewForPixbuf (Gdk.Functions.PixbufGetFromSurface (surf, 0, 0, surf.Width, surf.Height)!);
	}
}
