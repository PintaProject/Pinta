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
	[System.ComponentModel.Composition.Export (typeof (BasePaintBrush))]
	public class SplatterBrush : PaintBrush
	{
		public override string Name {
			get { return Mono.Unix.Catalog.GetString ("Splatter"); }
		}

		public override double StrokeAlphaMultiplier {
			get { return 0.5; }
		}
		
		protected override Gdk.Rectangle OnMouseMove (int x, int y, int lastX, int lastY)
		{
			int size = Random.Next (2, 20);
			Rectangle r = new Rectangle (x + Random.Next (-15, 15), y + Random.Next (-15, 15), size, size);

			double rx = r.Width / 2;
			double ry = r.Height / 2;
			double cx = r.X + rx;
			double cy = r.Y + ry;
			double c1 = 0.552285;

			G.Save ();

			G.MoveTo (cx + rx, cy);

			G.CurveTo (cx + rx, cy - c1 * ry, cx + c1 * rx, cy - ry, cx, cy - ry);
			G.CurveTo (cx - c1 * rx, cy - ry, cx - rx, cy - c1 * ry, cx - rx, cy);
			G.CurveTo (cx - rx, cy + c1 * ry, cx - c1 * rx, cy + ry, cx, cy + ry);
			G.CurveTo (cx + c1 * rx, cy + ry, cx + rx, cy + c1 * ry, cx + rx, cy);

			G.ClosePath ();

			Rectangle dirty = G.StrokeExtents ();

			G.Fill ();
			G.Restore ();

			return dirty.ToGdkRectangle ();
		}
	}
}
