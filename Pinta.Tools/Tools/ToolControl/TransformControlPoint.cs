// 
// TransformControlPoint.cs
//  
// Author:
//       Volodymyr <${AuthorEmail}>
// 
// Copyright (c) 2012 Volodymyr
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

namespace Pinta.Tools
{
	[Flags]
	public enum TransformEdge
	{
			None = 0,
			Top = 1,
			Right = 2,
			Bottom = 4,
			Left = 8,
			TopLeft = Top | Left,
			TopRight = Top | Right,
			BottomRight = Bottom | Right,
			BottomLeft = Bottom | Left
		}

	public class TransformControlPoint
	{
		#region Constants
		private const double Size = 6;
		private readonly static Cairo.Color StrokeColor = new Cairo.Color (0, 0, 1, 0.7);
		private readonly static Cairo.Color FillColor = new Cairo.Color (0, 0, 1, 0.3);
		#endregion

		#region Properties
		public PointD Position { get; set; }
		public TransformEdge Edge { get; private set; }
		#endregion

		#region Constructor
		public TransformControlPoint(TransformEdge edge)
		{
			this.Edge = edge;
		}
		#endregion

		#region Implementation

		public bool IsInside(Matrix transform, PointD point)
		{
			PointD pos = this.Position;
			transform.TransformPoint(ref pos);
			return Math.Abs(pos.X - point.X) < Size && Math.Abs(pos.Y - point.Y) < Size;
		}

		public void Draw(Context g, Matrix transform, double scale)
		{
			double x = this.Position.X;
			double y = this.Position.Y;

			transform.TransformPoint(ref x, ref y);

			DrawEllipse(g, x * scale, y * scale);
		}

		public static void DrawEllipse(Context g, double x, double y)
		{
			Rectangle rc = new Rectangle(x - Size / 2, y - Size / 2, Size, Size);
			g.FillStrokedEllipse(rc, FillColor, StrokeColor, 1);
		}

		public void SetFromRectangle(Rectangle rc)
		{
			PointD res = new PointD();

			switch (this.Edge)
			{
			case TransformEdge.Bottom:
				res = new PointD(rc.X + rc.Width / 2, rc.Y + rc.Height);
				break;

			case TransformEdge.BottomLeft:
				res = new PointD(rc.X, rc.Y + rc.Height);
				break;

			case TransformEdge.BottomRight:
				res = new PointD(rc.X + rc.Width, rc.Y + rc.Height);
				break;

			case TransformEdge.Left:
				res = new PointD(rc.X, rc.Y + rc.Height / 2);
				break;

			case TransformEdge.Right:
				res = new PointD(rc.X + rc.Width, rc.Y + rc.Height / 2);
				break;

			case TransformEdge.Top:
				res = new PointD(rc.X + rc.Width / 2, rc.Y);
				break;

			case TransformEdge.TopLeft:
				res = new PointD(rc.X, rc.Y);
				break;

			case TransformEdge.TopRight:
				res = new PointD(rc.X + rc.Width, rc.Y);
				break;
			}

			this.Position = res;
		}
		#endregion
	}
}

