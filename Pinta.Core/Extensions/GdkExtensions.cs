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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cairo;

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
	public static bool IsShiftPressed (this Gdk.ModifierType m)
		=> m.HasFlag (Gdk.ModifierType.ShiftMask);

	/// <summary>
	/// Returns whether a Ctrl modifier is pressed (or the Cmd key on macOS).
	/// </summary>
	public static bool IsControlPressed (this Gdk.ModifierType m)
	{
		if (PintaCore.System.OperatingSystem == OS.Mac)
			return m.HasFlag (Gdk.ModifierType.MetaMask);
		else
			return m.HasFlag (Gdk.ModifierType.ControlMask);
	}

	public static bool IsAltPressed (this Gdk.ModifierType m)
		=> m.HasFlag (Gdk.ModifierType.AltMask);

	public static bool IsLeftMousePressed (this Gdk.ModifierType m)
		=> m.HasFlag (Gdk.ModifierType.Button1Mask);

	public static bool IsRightMousePressed (this Gdk.ModifierType m)
		=> m.HasFlag (Gdk.ModifierType.Button3Mask);

	/// <summary>
	/// Returns whether any of the Ctrl/Cmd/Shift/Alt modifiers are active.
	/// This prevents Caps Lock, Num Lock, etc from appearing as active modifier keys.
	/// </summary>
	public static bool HasModifierKey (this Gdk.ModifierType current_state)
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
		int shape_x;
		int shape_y;
		Gdk.Texture result = CreateIconWithShape (imgName, shape, shapeWidth, shapeWidth, 0, imgToShapeX, imgToShapeY, out shape_x, out shape_y);
		shapeX = shape_x;
		shapeY = shape_y;
		return result;
	}

	public static Gdk.Texture CreateIconWithShape (
		string imgName,
		CursorShape shape,
		int shapeWidth,
		int shapeHeight,
		int shapeAngle,
		int imgToShapeX,
		int imgToShapeY,
		out int shapeX,
		out int shapeY)
	{
		Gdk.Texture img = PintaCore.Resources.GetIcon (imgName);

		double zoom =
			(PintaCore.Workspace.HasOpenDocuments)
			? Math.Min (30d, PintaCore.Workspace.ActiveDocument.Workspace.Scale)
			: 1d;

		int clampedWidth = (int) Math.Min (800d, shapeWidth * zoom);
		int halfOfShapeWidth = clampedWidth / 2;
		int clampedHeight = (int) Math.Min (800d, shapeHeight * zoom);
		int halfOfShapeHeight = clampedHeight / 2;

		// Calculate bounding boxes around the both image and shape
		// relative to the image top-left corner.

		RectangleI imgBBox = new (0, 0, img.Width, img.Height);

		RectangleI initialShapeBBox = new (
			imgToShapeX - halfOfShapeWidth,
			imgToShapeY - halfOfShapeHeight,
			clampedWidth,
			clampedHeight);

		// Inflate shape bounding box to allow for anti-aliasing
		RectangleI inflatedBBox = initialShapeBBox.Inflated (2, 2);

		// To determine required size of icon,
		// find union of the image and shape bounding boxes
		// (still relative to image top-left corner)
		RectangleI iconBBox = imgBBox.Union (inflatedBBox);

		// Image top-left corner in icon coordinates
		int imgX = imgBBox.Left - iconBBox.Left;
		int imgY = imgBBox.Top - iconBBox.Top;

		// Shape center point in icon coordinates
		shapeX = imgToShapeX - iconBBox.Left;
		shapeY = imgToShapeY - iconBBox.Top;

		ImageSurface i = CairoExtensions.CreateImageSurface (
			Format.Argb32,
			iconBBox.Width,
			iconBBox.Height);

		using Context g = new (i);

		// Don't show shape if shapeWidth less than 3,
		if (clampedHeight > 3) {

			int diam = Math.Max (1, clampedWidth - 2);

			RectangleD shapeRect = new (
				shapeX - halfOfShapeWidth,
				shapeY - halfOfShapeHeight,
				diam,
				clampedHeight);

			Color outerColor = new (255, 255, 255, 0.75);
			Color innerColor = new (0, 0, 0);

			switch (shape) {
				case CursorShape.Ellipse:
					g.DrawEllipse (shapeRect, outerColor, 2);
					shapeRect = shapeRect.Inflated (-1, -1);
					g.DrawEllipse (shapeRect, innerColor, 1);
					break;
				case CursorShape.Rectangle:
					if (shapeAngle == 0) {
						g.DrawRectangle (shapeRect, outerColor, 1);
						shapeRect = shapeRect.Inflated (-1, -1);
						g.DrawRectangle (shapeRect, innerColor, 1);
					} else {
						PointD[] pointsOfRotatedRectangle = RotateRectangle (shapeRect, shapeAngle);
						shapeRect = shapeRect.Inflated (-1, -1);
						PointD[] pointsOfInflatedRotatedRectangle = RotateRectangle (shapeRect, shapeAngle);
						g.DrawPolygonal (new ReadOnlySpan<PointD> (pointsOfRotatedRectangle), outerColor, LineCap.Butt);
						g.DrawPolygonal (new ReadOnlySpan<PointD> (pointsOfInflatedRotatedRectangle), innerColor, LineCap.Butt);
					}
					break;
			}
		}

		// Draw the image
		ImageSurface img_surf = img.ToSurface ();

		g.SetSourceSurface (img_surf, imgX, imgY);
		g.Paint ();

		return Gdk.Texture.NewForPixbuf (i.ToPixbuf ());
	}

	// TODO-GTK4 (bindings, unsubmitted) - need gir.core async bindings for Gdk.Clipboard
	public static Task<Gdk.Texture?> ReadTextureAsync (this Gdk.Clipboard clipboard)
	{
		TaskCompletionSource<Gdk.Texture?> tcs = new ();

		Gdk.Internal.Clipboard.ReadTextureAsync (
			clipboard.Handle.DangerousGetHandle (),
			IntPtr.Zero,
			new Gio.Internal.AsyncReadyCallbackAsyncHandler ((_, args, _) => {

				IntPtr result = Gdk.Internal.Clipboard.ReadTextureFinish (
					clipboard.Handle.DangerousGetHandle (),
					args.Handle.DangerousGetHandle (),
					out var error);

				Gdk.Texture? texture = (Gdk.Texture?) GObject.Internal.InstanceWrapper.WrapNullableHandle<Gdk.Texture> (result, ownedRef: true);

				if (!error.IsInvalid)
					texture = null;

				tcs.SetResult (texture);

			}).NativeCallback,
			IntPtr.Zero);

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
	public static unsafe ImageSurface ToSurface (this Gdk.Texture texture)
	{
		ImageSurface surf = CairoExtensions.CreateImageSurface (
			Format.Argb32,
			texture.Width,
			texture.Height);

		Span<byte> surf_data = surf.GetData ();

		// TODO-GTK4 (bindings, unsubmitted) - needs support for primitive value arrays
		var buffer = new byte[surf_data.Length];
		fixed (byte* buffer_data = buffer)
			Gdk.Internal.Texture.Download (
				texture.Handle.DangerousGetHandle (),
				ref *buffer_data,
				(uint) surf.Stride);

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
		GLib.SList slist = new (FileListGetFiles (file_list.Handle));

		uint n = GLib.SList.Length (slist);

		var result = new Gio.File[n];
		for (uint i = 0; i < n; ++i) {
			IntPtr file_ptr = GLib.SList.NthData (slist, i);
			result[i] = (Gio.FileHelper) GObject.Internal.InstanceWrapper.WrapHandle<Gio.FileHelper> (file_ptr, ownedRef: false);
		}

		return result;
	}

	/// <summary>
	/// Wrapper for Gdk.Cursor.NewFromName which handles errors instead of returning null.
	/// </summary>
	public static Gdk.Cursor CursorFromName (string name)
		=> Gdk.Cursor.NewFromName (name, null) ?? throw new ArgumentException ("Cursor does not exist", nameof (name));

	/// <summary>
	/// Convert from Gdk.RGBA to Cairo.Color.
	/// </summary>
	public static Cairo.Color ToCairoColor (this Gdk.RGBA color)
		=> new (color.Red, color.Green, color.Blue, color.Alpha);

	/// <summary>
	/// Convert from Cairo.Color to Gdk.RGBA.
	/// </summary>
	public static Gdk.RGBA ToGdkRGBA (this Cairo.Color color)
	{
		return new Gdk.RGBA {
			Red = (float) color.R,
			Blue = (float) color.B,
			Green = (float) color.G,
			Alpha = (float) color.A
		};
	}

	private static PointD[] RotateRectangle (RectangleD rectangle, int angle_in_degrees)
	{
		float angle_in_radians = Single.DegreesToRadians (-angle_in_degrees);
		RectangleD translatedToOrigin = new RectangleD (
			rectangle.X - rectangle.GetCenter ().X,
			rectangle.Y - rectangle.GetCenter ().Y,
			rectangle.Width,
			rectangle.Height
		);
		Matrix3x2D rotation = Matrix3x2D.CreateRotation (new RadiansAngle (angle_in_radians));
		List<PointD> pointsOfRotatedRectangle = [
			translatedToOrigin.Location().Transformed(rotation),
			new PointD(translatedToOrigin.Location().X, translatedToOrigin.Location().Y + translatedToOrigin.Height).Transformed(rotation),
			translatedToOrigin.EndLocation().Transformed(rotation),
			new PointD(translatedToOrigin.Location().X + translatedToOrigin.Width, translatedToOrigin.Location().Y).Transformed(rotation)
		];
		PointD[] pointsOfFinalRectangle = pointsOfRotatedRectangle.Select (p => new PointD (p.X + rectangle.GetCenter ().X, p.Y + rectangle.GetCenter ().Y)).ToArray ();
		return pointsOfFinalRectangle;
	}
}
