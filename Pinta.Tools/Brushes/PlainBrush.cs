//
// PlainBrush.cs
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

using Cairo;

using Pinta.Core;

namespace Pinta.Tools.Brushes;

public sealed class PlainBrush : BasePaintBrush
{
	public override string Name => Translations.GetString ("Normal");

	public override int Priority => -100;

	private Path? path;

	protected override RectangleI OnMouseMove (
		Context g,
		ImageSurface surface,
		BrushStrokeArgs strokeArgs)
	{
		surface.Clear ();

		g.LineCap = g.LineWidth == 1 ? LineCap.Butt : LineCap.Round;

		if (path is null)
			g.MoveTo (strokeArgs.LastPosition.X, strokeArgs.LastPosition.Y);
		else
			g.AppendPath (path);

		DrawNonSinglePixelLine (g, strokeArgs);

		path = g.CopyPath ();

		RectangleI dirty = g.StrokeExtents ().ToInt ();

		// For some reason (?!) we need to inflate the dirty
		// rectangle for small brush widths in zoomed images
		var inflated = dirty.Inflated (1, 1);

		return inflated;
	}

	private static void DrawNonSinglePixelLine (Context g, BrushStrokeArgs strokeArgs)
	{
		g.LineTo (strokeArgs.CurrentPosition.X + 0.5, strokeArgs.CurrentPosition.Y + 0.5);
		g.StrokePreserve ();
	}

	protected override void OnMouseUp ()
	{
		path = null;
	}
}
