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
namespace Pinta.Core;

public readonly record struct RectangleD
{
	public readonly double X;
	public readonly double Y;
	public readonly double Width;
	public readonly double Height;

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

	/// <summary>
	/// Create a rectangle from the provided points.
	/// </summary>
	/// <param name="invert_if_negative">
	/// Flips the start and end points if necessary to produce a rectangle with positive width and height.
	/// Otherwise, a negative width or height is clamped to zero.
	/// </param>
	public static RectangleD FromPoints (in PointD start, in PointD end, bool invert_if_negative = true)
	{
		if (invert_if_negative) {
			double y1 = Math.Min (start.Y, end.Y);
			double y2 = Math.Max (start.Y, end.Y);
			double x1 = Math.Min (start.X, end.X);
			double x2 = Math.Max (start.X, end.X);
			return new RectangleD (x1, y1, x2 - x1, y2 - y1);
		} else {
			return new RectangleD (start.X, start.Y, end.X - start.X, end.Y - start.Y);
		}
	}

	public static readonly RectangleD Zero;

	public readonly RectangleI ToInt () => new ((int) Math.Floor (X), (int) Math.Floor (Y),
						      (int) Math.Ceiling (Width), (int) Math.Ceiling (Height));

	public readonly double Left => X;
	public readonly double Top => Y;
	public readonly double Right => X + Width - 1;
	public readonly double Bottom => Y + Height - 1;

	public override readonly string ToString () => $"x:{X} y:{Y} w:{Width} h:{Height}";

	public readonly bool ContainsPoint (double x, double y)
	{
		if (x < this.X || x >= this.X + this.Width)
			return false;

		if (y < this.Y || y >= this.Y + this.Height)
			return false;

		return true;
	}

	public readonly bool ContainsPoint (in PointD point) => ContainsPoint (point.X, point.Y);

	public readonly PointD Location () => new (X, Y);
	public readonly PointD EndLocation () => new (X + Width, Y + Height);
	public readonly PointD GetCenter () => new (X + 0.5 * Width, Y + 0.5 * Height);

	public readonly RectangleD Inflated (double width, double height)
	{
		var newX = X - width;
		var newY = Y - height;
		var newWidth = Width + (width * 2);
		var newHeight = Height + (height * 2);
		return new (newX, newY, newWidth, newHeight);
	}

	public readonly RectangleD ClampLocation ()
	{
		double x = this.X;
		double y = this.Y;
		double w = this.Width;
		double h = this.Height;

		if (x < 0) {
			w -= x;
			x = 0;
		}

		if (y < 0) {
			h -= y;
			y = 0;
		}

		return new RectangleD (x, y, w, h);
	}
}

public readonly record struct RectangleI (int X, int Y, int Width, int Height)
{
	public RectangleI (in PointI point, int width, int height)
		: this (point.X, point.Y, width, height)
	{
	}

	public RectangleI (in PointI point, in Size size)
		: this (point.X, point.Y, size.Width, size.Height)
	{
	}

	public static readonly RectangleI Zero;

	public static RectangleI FromLTRB (int left, int top, int right, int bottom)
		=> new (left, top, right - left + 1, bottom - top + 1);

	public readonly RectangleD ToDouble () => new (X, Y, Width, Height);

	public readonly int Left => X;
	public readonly int Top => Y;
	public readonly int Right => X + Width - 1;
	public readonly int Bottom => Y + Height - 1;

	public readonly bool IsEmpty => (Width == 0) || (Height == 0);

	public readonly PointI Location => new (X, Y);
	public readonly Size Size => new (Width, Height);

	public override readonly string ToString () => $"x:{X} y:{Y} w:{Width} h:{Height}";

	public readonly bool Contains (int x, int y)
	{
		return x >= Left && x <= Right && y >= Top && y <= Bottom;
	}

	public readonly bool Contains (in PointI pt) => Contains (pt.X, pt.Y);

	public readonly RectangleI Intersect (RectangleI r) => Intersect (this, r);

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

	public readonly RectangleI Union (RectangleI r) => Union (this, r);

	public static RectangleI Union (in RectangleI a, in RectangleI b)
	{
		int left = Math.Min (a.Left, b.Left);
		int right = Math.Max (a.Right, b.Right);
		int top = Math.Min (a.Top, b.Top);
		int bottom = Math.Max (a.Bottom, b.Bottom);
		return FromLTRB (left, top, right, bottom);
	}

	public readonly RectangleI Inflated (int width, int height)
	{
		var newX = X - width;
		var newY = Y - height;
		var newWidth = Width + (width * 2);
		var newHeight = Height + (height * 2);
		return new (newX, newY, newWidth, newHeight);
	}
}

