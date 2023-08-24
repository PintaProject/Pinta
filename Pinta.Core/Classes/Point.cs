//
// Point.cs
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

/// Replacements for Cairo / GDK points that GtkSharp provided in the GTK3 build.
namespace Pinta.Core;

public record struct PointI
{
	public PointI (int x, int y)
	{
		this.X = x;
		this.Y = y;
	}

	public static readonly PointI Zero;

	public int X;
	public int Y;

	public override readonly string ToString () => $"{X}, {Y}";
}

public readonly record struct PointD (double X, double Y)
{
	public override readonly string ToString () => $"{X}, {Y}";

	public readonly PointI ToInt () => new ((int) X, (int) Y);

	public readonly double Distance (in PointD e) => new PointD (X - e.X, Y - e.Y).Magnitude ();

	public readonly double Magnitude () => Math.Sqrt (X * X + Y * Y);

	/// <summary>
	/// Returns a new point, rounded to the nearest integer coordinates.
	/// </summary>
	public readonly PointD Rounded () => new (Math.Round (X), Math.Round (Y));

	public static PointD operator + (in PointD a, in PointD b) => new (a.X + b.X, a.Y + b.Y);

	public static explicit operator PointD (PointI p) => new (p.X, p.Y);
}

public readonly record struct Size (int Width, int Height)
{
	public static readonly Size Empty;

	public override readonly string ToString () => $"{Width}, {Height}";

	public readonly bool IsEmpty => (Width == 0 && Height == 0);
}

