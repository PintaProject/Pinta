// 
// PencilTool.cs
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
using Cairo;
using Pinta.Core;

namespace Pinta.Tools
{
	public class PencilTool : BaseTool
	{
		private readonly IPaletteService palette;

		private Point last_point = point_empty;

		private ImageSurface? undo_surface;
		private bool surface_modified;
		private MouseButton mouse_button;

		public PencilTool (IServiceManager services) : base (services)
		{
			palette = services.GetService<IPaletteService> ();
		}

		public override string Name => Translations.GetString ("Pencil");
		public override string Icon => Pinta.Resources.Icons.ToolPencil;
		public override string StatusBarText => Translations.GetString ("Left click to draw freeform one-pixel wide lines with the primary color. Right click to use the secondary color.");
		public override Gdk.Cursor DefaultCursor => new Gdk.Cursor (Gdk.Display.Default, Resources.GetIcon ("Cursor.Pencil.png"), 7, 24);
		public override Gdk.Key ShortcutKey => Gdk.Key.P;
		public override int Priority => 25;
		protected override bool ShowAlphaBlendingButton => true;

		protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
		{
			// If we are already drawing, ignore any additional mouse down events
			if (mouse_button != MouseButton.None)
				return;

			surface_modified = false;
			undo_surface = document.Layers.CurrentUserLayer.Surface.Clone ();
			mouse_button = e.MouseButton;

			Color tool_color;

			if (e.MouseButton == MouseButton.Left) {
				tool_color = palette.PrimaryColor;
			} else if (e.MouseButton == MouseButton.Right) {
				tool_color = palette.SecondaryColor;
			} else {
				last_point = point_empty;
				return;
			}

			Draw (document, e, tool_color, true);
		}

		protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
		{
			Color tool_color;

			if (mouse_button == MouseButton.Left) {
				tool_color = palette.PrimaryColor;
			} else if (mouse_button == MouseButton.Right) {
				tool_color = palette.SecondaryColor;
			} else {
				last_point = point_empty;
				return;
			}

			Draw (document, e, tool_color, false);
		}

		protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
		{
			if (undo_surface != null) {
				if (surface_modified)
					document.History.PushNewItem (new SimpleHistoryItem (Icon, Name, undo_surface, document.Layers.CurrentUserLayerIndex));
				else
					undo_surface.Dispose ();
			}

			surface_modified = false;
			undo_surface = null;
			mouse_button = MouseButton.None;
		}

		private void Draw (Document document, ToolMouseEventArgs e, Color tool_color, bool first_pixel)
		{
			var x = e.Point.X;
			var y = e.Point.Y;

			if (last_point.Equals (point_empty)) {
				last_point = e.Point;

				if (!first_pixel)
					return;
			}

			if (document.Workspace.PointInCanvas (e.PointDouble))
				surface_modified = true;

			using (var g = document.CreateClippedContext ()) {

				g.Antialias = Antialias.None;

				g.SetSourceColor (tool_color);

				if (UseAlphaBlending)
					g.SetBlendMode (BlendMode.Normal);
				else
					g.Operator = Operator.Source;

				g.LineWidth = 1;
				g.LineCap = LineCap.Square;

				if (first_pixel) {
					// Cairo does not support a single-pixel-long single-pixel-wide line
					g.Rectangle (x, y, 1.0, 1.0);
					g.Fill ();
				} else {
					// Adding 0.5 forces cairo into the correct square:
					// See https://bugs.launchpad.net/bugs/672232
					g.MoveTo (last_point.X + 0.5, last_point.Y + 0.5);
					g.LineTo (x + 0.5, y + 0.5);
					g.Stroke ();
				}
			}

			var dirty = CairoExtensions.GetRectangleFromPoints (last_point, new Point (x, y), 4);

			document.Workspace.Invalidate (document.ClampToImageSize (dirty));

			last_point = new Point (x, y);
		}
	}
}
