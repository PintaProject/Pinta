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
using Gdk;

namespace Pinta.Core
{
	public static class GdkExtensions
	{
		// Invalidate the whole thing
		public static void Invalidate(this Window w)
		{
			w.InvalidateRect(new Rectangle(0, 0, w.Width, w.Height), true);
		}

		public static Rectangle GetBounds(this Window w)
		{
			return new Rectangle(0, 0, w.Width, w.Height);
		}

		public static Size GetSize(this Window w)
		{
			return new Size(w.Width, w.Height);
		}

		public static Cairo.Color ToCairoColor(this Gdk.Color color)
		{
			return new Cairo.Color((double)color.Red / ushort.MaxValue, (double)color.Green / ushort.MaxValue, (double)color.Blue / ushort.MaxValue);
		}

		public static Gdk.Point Center(this Gdk.Rectangle rect)
		{
			return new Gdk.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
		}

		public static ColorBgra ToBgraColor(this Gdk.Color color)
		{
			return ColorBgra.FromBgr((byte)(color.Blue * 255 / ushort.MaxValue), (byte)(color.Green * 255 / ushort.MaxValue), (byte)(color.Red * 255 / ushort.MaxValue));
		}

		public static bool IsNotSet(this Point p)
		{
			return p.X == int.MinValue && p.Y == int.MinValue;
		}

		public static bool IsShiftPressed(this ModifierType m)
		{
			return m.HasFlag (ModifierType.ShiftMask);
		}

		/// <summary>
		/// Returns whether a Ctrl modifier is pressed (or the Cmd key on macOS).
		/// </summary>
		public static bool IsControlPressed (this ModifierType m)
		{
			if (PintaCore.System.OperatingSystem == OS.Mac)
				return m.HasFlag (ModifierType.Mod2Mask);
			else
				return m.HasFlag (ModifierType.ControlMask);
		}

		public static bool IsAltPressed (this ModifierType m)
		{
			return m.HasFlag (ModifierType.Mod1Mask);
		}

		public static bool IsLeftMousePressed (this ModifierType m)
		{
			return m.HasFlag (ModifierType.Button1Mask);
		}

		public static bool IsRightMousePressed (this ModifierType m)
		{
			return m.HasFlag (ModifierType.Button3Mask);
		}

		public static bool IsShiftPressed(this EventButton ev)
		{
			return ev.State.IsShiftPressed();
		}

		public static bool IsControlPressed(this EventButton ev)
		{
			return ev.State.IsControlPressed();
		}

		public static bool IsAltPressed (this EventButton ev)
		{
			return ev.State.IsAltPressed ();
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
		/// Filters out all modifier keys except Ctrl/Shift/Alt. This prevents Caps Lock, Num Lock, etc
		/// from appearing as active modifier keys.
		/// </summary>
		public static ModifierType FilterModifierKeys (this ModifierType current_state)
        {
            var state = Gdk.ModifierType.None;

			state |= (current_state & Gdk.ModifierType.ControlMask);
			state |= (current_state & Gdk.ModifierType.ShiftMask);
			state |= (current_state & Gdk.ModifierType.Mod1Mask);
			state |= (current_state & Gdk.ModifierType.Mod2Mask); // Command key on macOS.

			return state;
		}

		public static Cairo.PointD GetPoint(this EventButton ev)
		{
			return new Cairo.PointD(ev.X, ev.Y);
		}

		/// <summary>
		/// The implementation of Rectangle.Bottom was changed in 2.12.11 to fix an off-by-one error,
		/// and this function provides the newer behaviour for backwards compatibility with older versions.
		/// </summary>
		public static int GetBottom(this Rectangle r)
		{
			return r.Y + r.Height - 1;
		}

		/// <summary>
		/// The implementation of Rectangle.Right was changed in 2.12.11 to fix an off-by-one error,
		/// and this function provides the newer behaviour for backwards compatibility with older versions.
		/// </summary>
		public static int GetRight(this Rectangle r)
		{
			return r.X + r.Width - 1;
		}

		public static Cairo.Surface ToSurface(this Pixbuf pixbuf)
		{
			var surface = CairoExtensions.CreateImageSurface (Cairo.Format.ARGB32, pixbuf.Width, pixbuf.Height);

			using (var g = new Cairo.Context(surface))
			{
				Gdk.CairoHelper.SetSourcePixbuf(g, pixbuf, 0, 0);
				g.Paint();
			}

			return surface;
		}

		public static Cairo.Color ToCairoColor(this Gdk.RGBA color)
        {
			return new Cairo.Color(color.Red, color.Green, color.Blue, color.Alpha);
        }

		public static Pixbuf CreateColorSwatch(int size, Color color)
		{
			using (var surf = CairoExtensions.CreateImageSurface (Cairo.Format.Argb32, size, size))
			using (var g = new Cairo.Context(surf))
			{
				g.FillRectangle(new Cairo.Rectangle(0, 0, size, size), color.ToCairoColor());
				g.DrawRectangle(new Cairo.Rectangle(0, 0, size, size), new Cairo.Color(0, 0, 0), 1);
				return surf.ToPixbuf();
			}
		}

		public static Pixbuf CreateTransparentColorSwatch (bool drawBorder)
		{
			var size = 16;

			using (var surface = CairoExtensions.CreateTransparentBackgroundSurface (size))
			using (var g = new Cairo.Context (surface)) {
				if (drawBorder)
					g.DrawRectangle (new Cairo.Rectangle (0, 0, size, size), new Cairo.Color (0, 0, 0), 1);

				return surface.ToPixbuf ();
			}
		}
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

		public static Key ToUpper (this Key k1)
		{
			if (Enum.TryParse (k1.ToString ().ToUpperInvariant (), out Key result))
				return result;

			return k1;
		}

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
	}
}
