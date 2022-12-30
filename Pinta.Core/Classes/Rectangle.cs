//
// Rectangle.cs
//
// Author:
//       Cameron White <cameronwhite91@gmail.com>
//
// Copyright (c) 2022 
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

/// Replacements for Cairo / GDK rectangles that GtkSharp provided in the GTK3 build.
namespace Pinta.Core
{
	public record struct RectangleD
	{
		public double X;
		public double Y;
		public double Width;
		public double Height;

		public RectangleD (double x, double y, double width, double height)
		{
			this.X = x;
			this.Y = y;
			this.Width = width;
			this.Height = height;
		}

		public RectangleD (in PointD point, double width, double height)
			: this (point.X, point.Y, width, height)
		{
		}

		public double Left => X;
		public double Top => Y;
		public double Right => X + Width - 1; // TODO-GTK4 - Cairo.Rectangle.GetRight() was X + Width, same for GetBottom()
		public double Bottom => Y + Height - 1;

		public override string ToString () => $"x:{X} y:{Y} w:{Width} h:{Height}";
	}

	public record struct RectangleI
	{
		public int X;
		public int Y;
		public int Width;
		public int Height;

		public RectangleI (int x, int y, int width, int height)
		{
			this.X = x;
			this.Y = y;
			this.Width = width;
			this.Height = height;
		}

		public RectangleI (in PointI point, int width, int height)
			: this (point.X, point.Y, width, height)
		{
		}

		public RectangleI (in PointI point, in Size size)
			: this (point.X, point.Y, size.Width, size.Height)
		{
		}

		public static RectangleI Zero;

		public static RectangleI FromLTRB (int left, int top, int right, int bottom)
			=> new RectangleI (left, top, right - left + 1, bottom - top + 1);

		public RectangleD ToDouble() => new RectangleD(X, Y, Width, Height);

		public int Left => X;
		public int Top => Y;
		public int Right => X + Width - 1;
		public int Bottom => Y + Height - 1;

		public PointI Location => new PointI (X, Y);
		public Size Size => new Size (Width, Height);

		public override string ToString () => $"x:{X} y:{Y} w:{Width} h:{Height}";

		public bool Contains (int x, int y)
		{
			return x >= Left && x <= Right && y >= Top && y <= Bottom;
		}

		public void Intersect (RectangleI r) => this = Intersect (this, r);

		public static RectangleI Intersect (in RectangleI a, in RectangleI b)
		{
			int left = Math.Max (a.Left, b.Left);
			int right = Math.Min (a.Right, b.Right);
			int top = Math.Max (a.Top, b.Top);
			int bottom = Math.Min (a.Bottom, b.Bottom);

			if (left > right || top > bottom)
				return Zero;

			return FromLTRB (left, top, right, bottom);
		}
	}
}

