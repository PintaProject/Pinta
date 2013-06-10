//
// SquaresBrush.cs
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
	public class SquaresBrush : BasePaintBrush
	{
		private static double theta = Math.PI / 2;

		public override string Name {
			get { return Mono.Unix.Catalog.GetString ("Squares"); }
		}

		protected override Gdk.Rectangle OnMouseMove (Context g, Color strokeColor, ImageSurface surface,
		                                              int x, int y, int lastX, int lastY)
		{
			int dx = x - lastX;
			int dy = y - lastY;
			double px = Math.Cos (theta) * dx - Math.Sin (theta) * dy;
			double py = Math.Sin (theta) * dx + Math.Cos (theta) * dy;

			g.MoveTo (lastX - px, lastY - py);
			g.LineTo (lastX + px, lastY + py);
			g.LineTo (x + px, y + py);
			g.LineTo (x - px, y - py);
			g.LineTo (lastX - px, lastY - py);

			g.StrokePreserve ();

			return g.FixedStrokeExtents ().ToGdkRectangle ();
		}
	}
}
