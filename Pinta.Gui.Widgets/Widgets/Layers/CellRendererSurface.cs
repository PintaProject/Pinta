// 
// CellRendererSurface.cs
//  
// Author:
//       Greg Lowe <greg@vis.net.nz>
// 
// Copyright (c) 2010 Greg Lowe
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
using Gtk;
using Cairo;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class CellRendererSurface : CellRenderer
	{
		private static readonly Pattern transparent_pattern;

		[GLib.Property ("surface", "Get/Set Surface", "Set the cairo image surface to display a thumbnail of.")]
		public ImageSurface? Surface { get; set; }

		static CellRendererSurface ()
		{
			transparent_pattern = CairoExtensions.CreateTransparentBackgroundPattern (8);
		}

		public CellRendererSurface (int width, int height)
		{
			// TODO: Respect cell padding (Xpad and Ypad).
			SetFixedSize (width, height);
		}

		protected override void OnGetSize (Widget widget, ref Gdk.Rectangle cellArea, out int x, out int y, out int width, out int height)
		{
			// TODO: Respect cell padding (Xpad and Ypad).
			x = cellArea.Left;
			y = cellArea.Top;
			width = cellArea.Width;
			height = cellArea.Height;
		}

		protected override void OnRender (Context g, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, CellRendererState flags)
		{
			OnGetSize (widget, ref cell_area, out var x, out var y, out var width, out var height);

			g.Save ();
			g.Translate (x, y);
			RenderCell (g, width, height);
			g.Restore ();
		}

		private void RenderCell (Context g, int width, int height)
		{
			// Add some padding
			width -= 2;
			height -= 2;

			double scale;
			var draw_width = width;
			var draw_height = height;

			if (Surface is null)
				return;

			// The image is more constrained by height than width
			if ((double) width / (double) Surface.Width >= (double) height / (double) Surface.Height) {
				scale = (double) height / (double) (Surface.Height);
				draw_width = (int) (Surface.Width * height / Surface.Height);
			} else {
				scale = (double) width / (double) (Surface.Width);
				draw_height = (int) (Surface.Height * width / Surface.Width);
			}

			var offset_x = (int) ((width - draw_width) / 2f);
			var offset_y = (int) ((height - draw_height) / 2f);

			g.Save ();
			g.Rectangle (offset_x, offset_y, draw_width, draw_height);
			g.Clip ();

			g.SetSource (transparent_pattern);
			g.Paint ();

			g.Scale (scale, scale);
			g.SetSourceSurface (Surface, (int) (offset_x / scale), (int) (offset_y / scale));
			g.Paint ();

			g.Restore ();

			// TODO: scale this box correctly to match layer aspect ratio
			g.SetSourceColor (new Color (0.5, 0.5, 0.5));
			g.Rectangle (offset_x + 0.5, offset_y + 0.5, draw_width, draw_height);
			g.LineWidth = 1;
			g.Stroke ();
		}
	}
}
