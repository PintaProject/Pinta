// 
// Point.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc
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
using System.Collections;
using System.Globalization;

namespace Xwt {

	[Serializable]
	struct Point {

		public double X { get; set; }
		public double Y { get; set; }

		public static Point Zero = new Point ();

		public override string ToString ()
		{
			return String.Format ("{{X={0} Y={1}}}", X.ToString (CultureInfo.InvariantCulture), Y.ToString (CultureInfo.InvariantCulture));
		}
		
		public Point (double x, double y): this ()
		{
			this.X = x;
			this.Y = y;
		}
		
		public Point (Size sz): this ()
		{
			this.X = sz.Width;
			this.Y = sz.Height;
		}
		
		public override bool Equals (object o)
		{
			if (!(o is Point))
				return false;
		
			return (this == (Point) o);
		}
		
		public override int GetHashCode ()
		{
			unchecked {
				return (X.GetHashCode () * 397) ^ Y.GetHashCode ();
			}
		}

		public Point Offset (Point offset)
		{
			return Offset (offset.X, offset.Y);
		}
		
		public Point Offset (double dx, double dy)
		{
			Point p = this;
			p.X += dx;
			p.Y += dy;
			return p;
		}

		public Point Round ()
		{
			return new Point (
				Math.Round (X),
				Math.Round (Y)
			);
		}

		public bool IsEmpty {
			get {
				return ((X == 0) && (Y == 0));
			}
		}
		
		public static explicit operator Size (Point pt)
		{
			return new Size (pt.X, pt.Y);
		}
		
		public static Point operator + (Point pt, Size sz)
		{
			return new Point (pt.X + sz.Width, pt.Y + sz.Height);
		}
		
		public static Point operator - (Point pt, Size sz)
		{
			return new Point (pt.X - sz.Width, pt.Y - sz.Height);
		}
		
		public static bool operator == (Point pt_a, Point pt_b)
		{
			return ((pt_a.X == pt_b.X) && (pt_a.Y == pt_b.Y));
		}
		
		public static bool operator != (Point pt_a, Point pt_b)
		{
			return ((pt_a.X != pt_b.X) || (pt_a.Y != pt_b.Y));
		}
	}
}
