//
// SplatterBrush.cs
//
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
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

internal sealed class SplatterBrush : BasePaintBrush
{
	public override string Name
		=> Translations.GetString ("Splatter");

	public override double StrokeAlphaMultiplier
		=> 0.5;

	private readonly Random random = new ();

	protected override RectangleI OnMouseMove (
		Context g,
		ImageSurface surface,
		BrushStrokeArgs strokeArgs)
	{
		int line_width = (int) g.LineWidth;

		// we want a minimum size of 2 for the splatter (except for when the brush width is 1), since a splatter of size 1 is very small
		int size = (line_width == 1) ? 1 : random.Next (2, line_width);

		PointI current = strokeArgs.CurrentPosition;

		RectangleD rect = new (
			X: current.X - random.Next (-15, 15),
			Y: current.Y - random.Next (-15, 15),
			Width: size,
			Height: size);

		PointD r = new (
			X: rect.Width / 2,
			Y: rect.Height / 2);

		PointD c = new (
			X: rect.X + r.X,
			Y: rect.Y + r.Y);

		const double c_1 = 0.552285;

		g.Save ();

		g.MoveTo (c.X + r.X, c.Y);

		g.CurveTo (
			c.X + r.X,
			c.Y - c_1 * r.Y,
			c.X + c_1 * r.X,
			c.Y - r.Y,
			c.X,
			c.Y - r.Y);

		g.CurveTo (
			c.X - c_1 * r.X,
			c.Y - r.Y,
			c.X - r.X,
			c.Y - c_1 * r.Y,
			c.X - r.X,
			c.Y);

		g.CurveTo (
			c.X - r.X,
			c.Y + c_1 * r.Y,
			c.X - c_1 * r.X,
			c.Y + r.Y,
			c.X,
			c.Y + r.Y);

		g.CurveTo (
			c.X + c_1 * r.X,
			c.Y + r.Y,
			c.X + r.X,
			c.Y + c_1 * r.Y,
			c.X + r.X,
			c.Y);

		g.ClosePath ();

		RectangleI dirty = g.StrokeExtents ().ToInt ();

		g.Fill ();
		g.Restore ();

		return dirty;
	}
}
