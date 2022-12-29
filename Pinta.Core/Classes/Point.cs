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
namespace Pinta.Core
{
	public record struct Point
	{
		public Point (int x, int y)
		{
			this.X = x;
			this.Y = y;
		}

		public int X;
		public int Y;

		public override string ToString () => $"{X}, {Y}";
	}

	public record struct PointD
	{
		public PointD (double x, double y)
		{
			this.X = x;
			this.Y = y;
		}

		public double X;
		public double Y;

		public override string ToString () => $"{X}, {Y}";

		public double Distance (in PointD e)
		{
			return new PointD (X - e.X, Y - e.Y).Magnitude ();
		}

		public double Magnitude ()
		{
			return Math.Sqrt (X * X + Y * Y);
		}
	}

	public record struct Size
	{
		public Size (int width, int height)
		{
			Width = width;
			Height = height;
		}

		public int Width;
		public int Height;

		public override string ToString () => $"{Width}, {Height}";
	}
}

