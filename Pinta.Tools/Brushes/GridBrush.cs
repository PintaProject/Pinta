// 
// GridBrush.cs
//  
// Author:
//       Aaron Bockover <abockover@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
	public class GridBrush : BasePaintBrush
	{
		public override string Name {
			get { return Mono.Unix.Catalog.GetString ("Grid"); }
		}

		public override double StrokeAlphaMultiplier {
			get { return 0.05; }
		}

		protected override Gdk.Rectangle OnMouseMove (Context g, Color strokeColor, ImageSurface surface,
		                                              int x, int y, int lastX, int lastY)
		{
			double cx = Math.Round (x / 100.0) * 100.0;
			double cy = Math.Round (y / 100.0) * 100.0;
			double dx = (cx - x) * 10.0;
			double dy = (cy - y) * 10.0;

			for (int i = 0; i < 50; i++) {
				g.MoveTo (cx, cy);
				g.QuadraticCurveTo (
					x + Random.NextDouble () * dx,
					y + Random.NextDouble () * dy,
					cx,
					cy);
				g.Stroke ();
			}

			return Gdk.Rectangle.Zero;
		}
	}
}
