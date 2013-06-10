//
// SplatterBrush.cs
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

namespace Pinta.Tools.Brushes
{
	public class SplatterBrush : BasePaintBrush
	{
		public override string Name {
			get { return Mono.Unix.Catalog.GetString ("Splatter"); }
		}

		public override double StrokeAlphaMultiplier {
			get { return 0.5; }
		}
		
		protected override Gdk.Rectangle OnMouseMove (Context g, Color strokeColor, ImageSurface surface,
		                                              int x, int y, int lastX, int lastY)
		{
			int line_width = (int)g.LineWidth;
			int size;

			// we want a minimum size of 2 for the splatter (except for when the brush width is 1), since a splatter of size 1 is very small
			if (line_width == 1)
			{
				size = 1;
			}
			else
			{
				size = Random.Next (2, line_width);
			}

			Rectangle r = new Rectangle (x - Random.Next (-15, 15), y - Random.Next (-15, 15), size, size);

			double rx = r.Width / 2;
			double ry = r.Height / 2;
			double cx = r.X + rx;
			double cy = r.Y + ry;
			double c1 = 0.552285;

			g.Save ();

			g.MoveTo (cx + rx, cy);

			g.CurveTo (cx + rx, cy - c1 * ry, cx + c1 * rx, cy - ry, cx, cy - ry);
			g.CurveTo (cx - c1 * rx, cy - ry, cx - rx, cy - c1 * ry, cx - rx, cy);
			g.CurveTo (cx - rx, cy + c1 * ry, cx - c1 * rx, cy + ry, cx, cy + ry);
			g.CurveTo (cx + c1 * rx, cy + ry, cx + rx, cy + c1 * ry, cx + rx, cy);

			g.ClosePath ();

			Rectangle dirty = g.FixedStrokeExtents ();

			g.Fill ();
			g.Restore ();

			return dirty.ToGdkRectangle ();
		}
	}
}
