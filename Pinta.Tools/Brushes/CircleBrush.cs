//
// CircleBrush.cs
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

internal sealed class CircleBrush : BasePaintBrush
{
	public override string Name => Translations.GetString ("Circles");

	public override double StrokeAlphaMultiplier => 0.05;

	private readonly Random random = new ();

	protected override RectangleI OnMouseMove (
		Context g,
		ImageSurface surface,
		BrushStrokeArgs strokeArgs)
	{
		PointI mouseDelta = strokeArgs.CurrentPosition - strokeArgs.LastPosition;

		PointD center = new (
			X: Math.Floor (strokeArgs.CurrentPosition.X / 100.0) * 100 + 50,
			Y: Math.Floor (strokeArgs.CurrentPosition.Y / 100.0) * 100 + 50);

		double d = mouseDelta.Magnitude () * 2.0;
		int steps = random.Next (1, 10);
		double step_delta = d / steps;

		for (int i = 0; i < steps; i++) {
			double radius = (steps - i) * step_delta;
			g.Arc (
				center.X,
				center.Y,
				radius,
				0,
				Math.PI * 2);
			g.Stroke ();
		}

		return RectangleI.Zero;
	}
}
