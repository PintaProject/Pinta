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

public readonly record struct PointF (float X, float Y)
{
	public static PointF Zero { get; } = new (0, 0);
	public override readonly string ToString () => $"{X}, {Y}";
	public readonly PointD ToDouble () => new (X, Y);
	public readonly PointI ToInt () => new ((int) X, (int) Y);

	/// <summary>
	/// Returns a new point, rounded to the nearest integer coordinates.
	/// </summary>
	public readonly PointF Rounded () => new (MathF.Round (X), MathF.Round (Y));

	public readonly PointF Scaled (float factor) => new (X * factor, Y * factor);
	public static explicit operator PointF (PointI p) => new (p.X, p.Y);
	public static PointF operator + (PointF left, PointF right)
		=> new (
			X: left.X + right.X,
			Y: left.Y + right.Y
		);

	public static PointF operator - (PointF left, PointF right)
		=> new (
			X: left.X - right.X,
			Y: left.Y - right.Y
		);
}

public readonly record struct PointI (int X, int Y)
{
	public static PointI Zero { get; } = new (0, 0);
	public override readonly string ToString () => $"{X}, {Y}";

	public readonly PointD ToDouble () => new (X, Y);
	public readonly PointF ToFloat () => new (X, Y);

	public PointI Rotated90CCW () // Counterclockwise
		=> new (-Y, X);

	public static PointI operator + (PointI left, PointI right)
		=> new (
			X: left.X + right.X,
			Y: left.Y + right.Y
		);

	public static PointI operator - (PointI left, PointI right)
		=> new (
			X: left.X - right.X,
			Y: left.Y - right.Y
		);
}

public readonly record struct PointD (double X, double Y)
{
	public static PointD Zero { get; } = new (0, 0);

	public override readonly string ToString () => $"{X}, {Y}";

	public readonly PointI ToInt () => new ((int) X, (int) Y);
	public readonly PointF ToFloat () => new ((float) X, (float) Y);

	/// <summary>
	/// Returns a new point, rounded to the nearest integer coordinates.
	/// </summary>
	public readonly PointD Rounded () => new (Math.Round (X), Math.Round (Y));

	public readonly PointD Scaled (double factor) => new (X * factor, Y * factor);

	public static explicit operator PointD (PointI p) => new (p.X, p.Y);

	public static PointD operator + (PointD left, PointD right)
		=> new (
			X: left.X + right.X,
			Y: left.Y + right.Y
		);

	public static PointD operator - (PointD left, PointD right)
		=> new (
			X: left.X - right.X,
			Y: left.Y - right.Y
		);
}

public readonly record struct Size (int Width, int Height)
{
	public static Size Empty { get; } = new (0, 0);

	public override readonly string ToString () => $"{Width}, {Height}";

	public readonly bool IsEmpty => (Width == 0 && Height == 0);
}

