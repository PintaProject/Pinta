// 
// GridBrush.cs
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

namespace Pinta.Tools.Brushes;

public sealed class GridBrush : BasePaintBrush
{
	public override string Name => Translations.GetString ("Grid");

	public override double StrokeAlphaMultiplier => 0.05;

	protected override RectangleI OnMouseMove (
		Context g,
		StrokeContext strokeContext,
		ImageSurface surface,
		PointI current,
		PointI last)
	{
		PointD c = new (
			X: Math.Round (current.X / 100.0) * 100.0,
			Y: Math.Round (current.Y / 100.0) * 100.0
		);

		PointD d = new (
			X: (c.X - current.X) * 10.0,
			Y: (c.Y - current.Y) * 10.0
		);

		for (int i = 0; i < 50; i++) {
			g.MoveTo (c.X, c.Y);
			g.QuadraticCurveTo (
				current.X + Random.NextDouble () * d.X,
				current.Y + Random.NextDouble () * d.Y,
				c.X,
				c.Y);
			g.Stroke ();
		}

		return RectangleI.Zero;
	}
}
