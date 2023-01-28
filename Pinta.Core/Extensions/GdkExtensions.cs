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
using System.Threading.Tasks;
using Gdk;
using GdkPixbuf;

namespace Pinta.Core
{
	public static class GdkExtensions
	{
#if false // TODO-GTK4
		// Invalidate the whole thing
		public static void Invalidate (this Window w)
		{
			w.InvalidateRect (new Rectangle (0, 0, w.Width, w.Height), true);
		}

		public static Rectangle GetBounds (this Window w)
		{
			return new Rectangle (0, 0, w.Width, w.Height);
		}

		public static Size GetSize (this Window w)
		{
			return new Size (w.Width, w.Height);
		}

		public static Cairo.Color ToCairoColor (this Gdk.Color color)
		{
			return new Cairo.Color ((double) color.Red / ushort.MaxValue, (double) color.Green / ushort.MaxValue, (double) color.Blue / ushort.MaxValue);
		}

		public static Gdk.Point Center (this Gdk.Rectangle rect)
		{
			return new Gdk.Point (rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
		}

		public static ColorBgra ToBgraColor (this Gdk.Color color)
		{
			return ColorBgra.FromBgr ((byte) (color.Blue * 255 / ushort.MaxValue), (byte) (color.Green * 255 / ushort.MaxValue), (byte) (color.Red * 255 / ushort.MaxValue));
		}
#endif

		public static bool IsShiftPressed (this ModifierType m)
		{
			return m.HasFlag (ModifierType.ShiftMask);
		}

		/// <summary>
		/// Returns whether a Ctrl modifier is pressed (or the Cmd key on macOS).
		/// </summary>
		public static bool IsControlPressed (this ModifierType m)
		{
			// The Cmd key is GDK_MOD2_MASK, which is no longer a public constant as of GTK4
			if (PintaCore.System.OperatingSystem == OS.Mac) {
				var modifier_val = (uint) m;
				return (modifier_val & 16) != 0;
			} else
				return m.HasFlag (ModifierType.ControlMask);
		}

		public static bool IsAltPressed (this ModifierType m)
		{
			return m.HasFlag (ModifierType.AltMask);
		}

		public static bool IsLeftMousePressed (this ModifierType m)
		{
			return m.HasFlag (ModifierType.Button1Mask);
		}

		public static bool IsRightMousePressed (this ModifierType m)
		{
			return m.HasFlag (ModifierType.Button3Mask);
		}

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

#if false // TODO-GTK4
		/// <summary>
		/// Create a cursor icon with a shape that visually represents the tool's thickness.
		/// </summary>
		/// <param name="imgName">A string containing the name of the tool's icon image to use.</param>
		/// <param name="shape">The shape to draw.</param>
		/// <param name="shapeWidth">The width of the shape.</param>
		/// <param name="imgToShapeX">The horizontal distance between the image's top-left corner and the shape center.</param>
		/// <param name="imgToShapeY">The verical distance between the image's top-left corner and the shape center.</param>
		/// <param name="shapeX">The X position in the returned Pixbuf that will be the center of the shape.</param>
		/// <param name="shapeY">The Y position in the returned Pixbuf that will be the center of the shape.</param>
		/// <returns>The new cursor icon with an shape that represents the tool's thickness.</returns>
		public static Gdk.Pixbuf CreateIconWithShape (string imgName, CursorShape shape, int shapeWidth,
							  int imgToShapeX, int imgToShapeY,
							  out int shapeX, out int shapeY)
		{
			Gdk.Pixbuf img = PintaCore.Resources.GetIcon (imgName);

			double zoom = 1d;
			if (PintaCore.Workspace.HasOpenDocuments) {
				zoom = Math.Min (30d, PintaCore.Workspace.ActiveDocument.Workspace.Scale);
			}

			shapeWidth = (int) Math.Min (800d, ((double) shapeWidth) * zoom);
			int halfOfShapeWidth = shapeWidth / 2;

			// Calculate bounding boxes around the both image and shape
			// relative to the image top-left corner.
			Gdk.Rectangle imgBBox = new Gdk.Rectangle (0, 0, img.Width, img.Height);
			Gdk.Rectangle shapeBBox = new Gdk.Rectangle (
				imgToShapeX - halfOfShapeWidth,
				imgToShapeY - halfOfShapeWidth,
				shapeWidth,
				shapeWidth);

			// Inflate shape bounding box to allow for anti-aliasing
			shapeBBox.Inflate (2, 2);

			// To determine required size of icon,
			// find union of the image and shape bounding boxes
			// (still relative to image top-left corner)
			Gdk.Rectangle iconBBox = imgBBox.Union (shapeBBox);

			// Image top-left corner in icon co-ordinates
			int imgX = imgBBox.Left - iconBBox.Left;
			int imgY = imgBBox.Top - iconBBox.Top;

			// Shape center point in icon co-ordinates
			shapeX = imgToShapeX - iconBBox.Left;
			shapeY = imgToShapeY - iconBBox.Top;

			using (var i = CairoExtensions.CreateImageSurface (Cairo.Format.ARGB32, iconBBox.Width, iconBBox.Height)) {
				using (var g = new Cairo.Context (i)) {
					// Don't show shape if shapeWidth less than 3,
					if (shapeWidth > 3) {
						int diam = Math.Max (1, shapeWidth - 2);
						Cairo.Rectangle shapeRect = new Cairo.Rectangle (shapeX - halfOfShapeWidth,
												 shapeY - halfOfShapeWidth,
												 diam,
												 diam);

						Cairo.Color outerColor = new Cairo.Color (255, 255, 255, 0.75);
						Cairo.Color innerColor = new Cairo.Color (0, 0, 0);

						switch (shape) {
							case CursorShape.Ellipse:
								g.DrawEllipse (shapeRect, outerColor, 2);
								shapeRect = shapeRect.Inflate (-1, -1);
								g.DrawEllipse (shapeRect, innerColor, 1);
								break;
							case CursorShape.Rectangle:
								g.DrawRectangle (shapeRect, outerColor, 1);
								shapeRect = shapeRect.Inflate (-1, -1);
								g.DrawRectangle (shapeRect, innerColor, 1);
								break;
						}
					}

					// Draw the image
					g.DrawPixbuf (img, new Cairo.Point (imgX, imgY));
				}

				return CairoExtensions.ToPixbuf (i);
			}
		}
#endif

		public static Key ToUpper (this Key k1)
		{
			if (Enum.TryParse (k1.ToString ().ToUpperInvariant (), out Key result))
				return result;

			return k1;
		}

#if false // TODO-GTK4
		public static void GetWidgetPointer (Gtk.Widget widget, out int x, out int y, out Gdk.ModifierType mask)
		{
			var pointer = widget.Display.DefaultSeat.Pointer;
			widget.Window.GetDevicePosition (pointer, out x, out y, out mask);
		}

		public static void GetWindowPointer (Gdk.Window window, out int x, out int y, out Gdk.ModifierType mask)
		{
			var pointer = window.Display.DefaultSeat.Pointer;
			window.GetDevicePosition (pointer, out x, out y, out mask);
		}
#endif

		// TODO-GTK4 - need gir.core async bindings for Gdk.Clipboard
		public static Task<string?> ReadTextAsync (this Gdk.Clipboard clipboard)
		{
			var tcs = new TaskCompletionSource<string?> ();

			Gdk.Internal.Clipboard.ReadTextAsync (clipboard.Handle, IntPtr.Zero, (_, args, _) => {
				GLib.Internal.ErrorOwnedHandle error;
				string? result = Gdk.Internal.Clipboard.ReadTextFinish (clipboard.Handle, args, out error);
				GLib.Error.ThrowOnError (error);

				tcs.SetResult (result);
			}, IntPtr.Zero);

			return tcs.Task;
		}

		/// <summary>
		/// Helper function to return the clipboard for the default display.
		/// </summary>
		public static Gdk.Clipboard GetDefaultClipboard () => Gdk.Display.GetDefault ()!.GetClipboard ();
	}
}
