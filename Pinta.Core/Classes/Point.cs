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
using System.Numerics;

/// Replacements for Cairo / GDK points that GtkSharp provided in the GTK3 build.
namespace Pinta.Core;

public readonly record struct PointF<T> (T X, T Y) where T : IFloatingPoint<T>
{
	public static PointF<T> Zero { get; } = new (T.Zero, T.Zero);
	public override readonly string ToString () => $"{X}, {Y}";

	public readonly PointI<TOut> ToInteger<TOut> () where TOut : IBinaryInteger<TOut>
	{
		TOut newX = TOut.CreateSaturating (X);
		TOut newY = TOut.CreateSaturating (Y);
		return new (newX, newY);
	}

	public readonly PointF<TOut> Cast<TOut> () where TOut : IFloatingPoint<TOut>
	{
		TOut newX = TOut.CreateChecked (X);
		TOut newY = TOut.CreateChecked (Y);
		return new (newX, newY);
	}

	public readonly PointF<T> Rotated90CCW () // Counterclockwise
		=> new (-Y, X);

	public static PointF<T> operator + (in PointF<T> left, in PointF<T> right)
		=> new (
			X: left.X + right.X,
			Y: left.Y + right.Y);

	public static PointF<T> operator - (in PointF<T> left, in PointF<T> right)
		=> new (
			X: left.X - right.X,
			Y: left.Y - right.Y);

	public readonly PointF<T> Scaled (T factor)
		=> new (
			X * factor,
			Y * factor);

	public readonly PointF<T> Rounded ()
		=> new (
			T.Round (X),
			T.Round (Y));
}

public readonly record struct PointI<T> (T X, T Y) where T : IBinaryInteger<T>
{
	public static PointI<T> Zero { get; } = new (T.Zero, T.Zero);
	public override readonly string ToString () => $"{X}, {Y}";

	public readonly PointF<TOut> ToFloatingPoint<TOut> () where TOut : IFloatingPoint<TOut>
	{
		TOut newX = TOut.CreateSaturating (X);
		TOut newY = TOut.CreateSaturating (Y);
		return new (newX, newY);
	}

	public readonly PointI<TOut> Cast<TOut> () where TOut : IBinaryInteger<TOut>
	{
		TOut newX = TOut.CreateChecked (X);
		TOut newY = TOut.CreateChecked (Y);
		return new (newX, newY);
	}

	public static PointI<T> operator + (in PointI<T> left, in PointI<T> right)
		=> new (
			X: left.X + right.X,
			Y: left.Y + right.Y);

	public static PointI<T> operator - (in PointI<T> left, in PointI<T> right)
		=> new (
			X: left.X - right.X,
			Y: left.Y - right.Y);

	public readonly PointI<T> Scaled (T factor)
		=> new (
			X * factor,
			Y * factor);
}

//public readonly record struct PointI (int X, int Y)
//{
//	public static PointI Zero { get; } = new (0, 0);
//	public override readonly string ToString () => $"{X}, {Y}";

//	public readonly PointD ToDouble () => new (X, Y);

//	public PointI Rotated90CCW () // Counterclockwise
//		=> new (-Y, X);

//	public static PointI operator + (PointI left, PointI right)
//		=> new (
//			X: left.X + right.X,
//			Y: left.Y + right.Y);

//	public static PointI operator - (PointI left, PointI right)
//		=> new (
//			X: left.X - right.X,
//			Y: left.Y - right.Y);
//}

//public readonly record struct PointD (double X, double Y)
//{
//	public static PointD Zero { get; } = new (0, 0);

//	public override readonly string ToString () => $"{X}, {Y}";

//	public readonly PointI ToInt () => new ((int) X, (int) Y);

//	/// <summary>
//	/// Returns a new point, rounded to the nearest integer coordinates.
//	/// </summary>
//	public readonly PointD Rounded () => new (Math.Round (X), Math.Round (Y));

//	public readonly PointD Scaled (double factor)
//		=> new (
//			X * factor,
//			Y * factor);

//	public static PointD operator + (in PointD a, in PointD b)
//		=> new (
//			a.X + b.X,
//			a.Y + b.Y);

//	public static explicit operator PointD (PointI p) => new (p.X, p.Y);

//	public static PointD operator + (PointD left, PointD right)
//		=> new (
//			X: left.X + right.X,
//			Y: left.Y + right.Y);

//	public static PointD operator - (PointD left, PointD right)
//		=> new (
//			X: left.X - right.X,
//			Y: left.Y - right.Y);
//}

public readonly record struct Size (int Width, int Height)
{
	public static Size Empty { get; } = new (0, 0);

	public override readonly string ToString () => $"{Width}, {Height}";

	public readonly bool IsEmpty => (Width == 0 && Height == 0);
}

