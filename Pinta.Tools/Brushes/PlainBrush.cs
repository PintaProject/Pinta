//
// PlainBrush.cs
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
	public class PlainBrush : BasePaintBrush
	{
		public override string Name {
			get { return Mono.Unix.Catalog.GetString ("Normal"); }
		}

		public override int Priority {
			get { return -100; }
		}

		protected override Gdk.Rectangle OnMouseMove (Context g, Color strokeColor, ImageSurface surface,
		                                              int x, int y, int lastX, int lastY)
		{
			// Cairo does not support a single-pixel-long single-pixel-wide line
			if (x == lastX && y == lastY && g.LineWidth == 1 &&
			    PintaCore.Workspace.ActiveWorkspace.PointInCanvas (new PointD(x,y))) {
				g.Rectangle (x, y, 1.0, 1.0);
				g.Fill ();
			}
            else {
				g.MoveTo (lastX + 0.5, lastY + 0.5);
				g.LineTo (x + 0.5, y + 0.5);
				g.StrokePreserve ();
			}

			Gdk.Rectangle dirty = g.FixedStrokeExtents ().ToGdkRectangle ();

			// For some reason (?!) we need to inflate the dirty
			// rectangle for small brush widths in zoomed images
			dirty.Inflate (1, 1);

			return dirty;
		}
	}
}
