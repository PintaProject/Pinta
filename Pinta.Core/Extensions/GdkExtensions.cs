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
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Gdk;

namespace Pinta.Core;

public static class GdkExtensions
{
	private const string GdkLibraryName = "Gdk";

	static GdkExtensions ()
	{
		NativeImportResolver.RegisterLibrary (GdkLibraryName,
			windowsLibraryName: "libgtk-4-1.dll",
			linuxLibraryName: "libgtk-4.so.1",
			osxLibraryName: "libgtk-4.1.dylib"
		);
	}
	public static bool IsShiftPressed (this ModifierType m)
		=> m.HasFlag (ModifierType.ShiftMask);

	/// <summary>
	/// Returns whether a Ctrl modifier is pressed (or the Cmd key on macOS).
	/// </summary>
	public static bool IsControlPressed (this ModifierType m)
	{
		if (PintaCore.System.OperatingSystem == OS.Mac) {
			return m.HasFlag (ModifierType.MetaMask);
		} else
			return m.HasFlag (ModifierType.ControlMask);
	}

	public static bool IsAltPressed (this ModifierType m)
		=> m.HasFlag (ModifierType.AltMask);

	public static bool IsLeftMousePressed (this ModifierType m)
		=> m.HasFlag (ModifierType.Button1Mask);

	public static bool IsRightMousePressed (this ModifierType m)
		=> m.HasFlag (ModifierType.Button3Mask);

	/// <summary>
	/// Returns whether this key is a Ctrl key (or the Cmd key on macOS).
	/// </summary>
	public static bool IsControlKey (this Key key)
	{
		if (PintaCore.System.OperatingSystem == OS.Mac)
			return key == Key.Meta_L || key == Key.Meta_R;
		else
			return key == Key.Control_L || key == Key.Control_R;
	}

	/// <summary>
	/// Returns whether any of the Ctrl/Cmd/Shift/Alt modifiers are active.
	/// This prevents Caps Lock, Num Lock, etc from appearing as active modifier keys.
	/// </summary>
	public static bool HasModifierKey (this ModifierType current_state)
		=> current_state.IsControlPressed () || current_state.IsShiftPressed () || current_state.IsAltPressed ();

	/// <summary>
	/// Create a cursor icon with a shape that visually represents the tool's thickness.
	/// </summary>
	/// <param name="imgName">A string containing the name of the tool's icon image to use.</param>
	/// <param name="shape">The shape to draw.</param>
	/// <param name="shapeWidth">The width of the shape.</param>
	/// <param name="imgToShapeX">The horizontal distance between the image's top-left corner and the shape center.</param>
	/// <param name="imgToShapeY">The vertical distance between the image's top-left corner and the shape center.</param>
	/// <param name="shapeX">The X position in the returned Pixbuf that will be the center of the shape.</param>
	/// <param name="shapeY">The Y position in the returned Pixbuf that will be the center of the shape.</param>
	/// <returns>The new cursor icon with an shape that represents the tool's thickness.</returns>
	public static Gdk.Texture CreateIconWithShape (
		string imgName,
		CursorShape shape,
		int shapeWidth,
		int imgToShapeX,
		int imgToShapeY,
		out int shapeX,
		out int shapeY)
	{
		Gdk.Texture img = PintaCore.Resources.GetIcon (imgName);

		double zoom = 1d;
		if (PintaCore.Workspace.HasOpenDocuments) {
			zoom = Math.Min (30d, PintaCore.Workspace.ActiveDocument.Workspace.Scale);
		}

		shapeWidth = (int) Math.Min (800d, shapeWidth * zoom);
		int halfOfShapeWidth = shapeWidth / 2;

		// Calculate bounding boxes around the both image and shape
		// relative to the image top-left corner.
		var imgBBox = new RectangleI (0, 0, img.Width, img.Height);
		var shapeBBox = new RectangleI (
			imgToShapeX - halfOfShapeWidth,
			imgToShapeY - halfOfShapeWidth,
			shapeWidth,
			shapeWidth);

		// Inflate shape bounding box to allow for anti-aliasing
		shapeBBox = shapeBBox.Inflated (2, 2);

		// To determine required size of icon,
		// find union of the image and shape bounding boxes
		// (still relative to image top-left corner)
		RectangleI iconBBox = imgBBox.Union (shapeBBox);

		// Image top-left corner in icon coordinates
		int imgX = imgBBox.Left - iconBBox.Left;
		int imgY = imgBBox.Top - iconBBox.Top;

		// Shape center point in icon coordinates
		shapeX = imgToShapeX - iconBBox.Left;
		shapeY = imgToShapeY - iconBBox.Top;

		var i = CairoExtensions.CreateImageSurface (Cairo.Format.Argb32, iconBBox.Width, iconBBox.Height);
		var g = new Cairo.Context (i);
		// Don't show shape if shapeWidth less than 3,
		if (shapeWidth > 3) {
			int diam = Math.Max (1, shapeWidth - 2);
			var shapeRect = new RectangleD (
				shapeX - halfOfShapeWidth,
				shapeY - halfOfShapeWidth,
				diam,
				diam);

			Cairo.Color outerColor = new Cairo.Color (255, 255, 255, 0.75);
			Cairo.Color innerColor = new Cairo.Color (0, 0, 0);

			switch (shape) {
				case CursorShape.Ellipse:
					g.DrawEllipse (shapeRect, outerColor, 2);
					shapeRect = shapeRect.Inflated (-1, -1);
					g.DrawEllipse (shapeRect, innerColor, 1);
					break;
				case CursorShape.Rectangle:
					g.DrawRectangle (shapeRect, outerColor, 1);
					shapeRect = shapeRect.Inflated (-1, -1);
					g.DrawRectangle (shapeRect, innerColor, 1);
					break;
			}
		}

		// Draw the image
		var img_surf = img.ToSurface ();
		g.SetSourceSurface (img_surf, imgX, imgY);
		g.Paint ();

		return Texture.NewForPixbuf (i.ToPixbuf ());
	}

	public static Key ToUpper (this Key k1)
	{
		if (Enum.TryParse (k1.ToString ().ToUpperInvariant (), out Key result))
			return result;

		return k1;
	}

	// TODO-GTK4 (bindings, unsubmitted) - need gir.core async bindings for Gdk.Clipboard
	public static Task<Texture?> ReadTextureAsync (this Gdk.Clipboard clipboard)
	{
		var tcs = new TaskCompletionSource<Texture?> ();

		Gdk.Internal.Clipboard.ReadTextureAsync (clipboard.Handle, IntPtr.Zero, new Gio.Internal.AsyncReadyCallbackAsyncHandler ((_, args, _) => {
			IntPtr result = Gdk.Internal.Clipboard.ReadTextureFinish (clipboard.Handle, args.Handle, out var error);

			Texture? texture = texture = GObject.Internal.ObjectWrapper.WrapNullableHandle<Texture> (result, ownedRef: true);

			if (!error.IsInvalid)
				texture = null;

			tcs.SetResult (texture);
		}).NativeCallback, IntPtr.Zero);

		return tcs.Task;
	}

	/// <summary>
	/// Helper function to set the clipboard's contents to an image.
	/// </summary>
	public static void SetImage (this Gdk.Clipboard clipboard, Cairo.ImageSurface surf)
		=> clipboard.SetTexture (Gdk.Texture.NewForPixbuf (surf.ToPixbuf ()));

	/// <summary>
	/// Helper function to return the clipboard for the default display.
	/// </summary>
	public static Gdk.Clipboard GetDefaultClipboard ()
		=> Gdk.Display.GetDefault ()!.GetClipboard ();

	/// <summary>
	/// Convert a texture to a Cairo surface.
	/// </summary>
	public static unsafe Cairo.ImageSurface ToSurface (this Gdk.Texture texture)
	{
		var surf = CairoExtensions.CreateImageSurface (Cairo.Format.Argb32, texture.Width, texture.Height);
		Span<byte> surf_data = surf.GetData ();

		// TODO-GTK4 (bindings, unsubmitted) - needs support for primitive value arrays
		var buffer = new byte[surf_data.Length];
		fixed (byte* buffer_data = buffer)
			Gdk.Internal.Texture.Download (texture.Handle, ref *buffer_data, (uint) surf.Stride);

		buffer.CopyTo (surf_data);
		surf.MarkDirty ();

		return surf;
	}

	[DllImport (GdkLibraryName, EntryPoint = "gdk_file_list_get_files")]
	private static extern GLib.Internal.SListOwnedHandle FileListGetFiles (Gdk.Internal.FileListHandle fileList);

	// Wrapper around Gdk.FileList.GetFiles() to return a Gio.File array rather than GLib.SList.
	// TODO-GTK4 (bindings) - Gdk.FileList.GetFiles() is not generated
	public static Gio.File[] GetFilesHelper (this Gdk.FileList file_list)
	{
		var slist = new GLib.SList (FileListGetFiles (file_list.Handle));

		uint n = GLib.SList.Length (slist);
		var result = new Gio.File[n];
		for (uint i = 0; i < n; ++i) {
			result[i] = new Gio.FileHelper (GLib.SList.NthData (slist, i), ownedRef: false);
		}

		return result;
	}

	/// <summary>
	/// Wrapper for Gdk.Cursor.NewFromName which handles errors instead of returning null.
	/// </summary>
	public static Gdk.Cursor CursorFromName (string name)
		=> Gdk.Cursor.NewFromName (name, null) ?? throw new ArgumentException ("Cursor does not exist", nameof (name));
}
