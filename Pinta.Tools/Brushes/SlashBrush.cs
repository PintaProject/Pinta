//
// SlashBrush.cs
//
// Author:
//       Paul Korecky <https://github.com/spaghetti22>
// 
// Copyright (c) 2026 Paul Korecky
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

internal sealed class SlashBrush : BasePaintBrush
{
	public override string Name
		=> Translations.GetString ("Slash");

	private int angle;

	private const string AngleSettingName = "slash-brush-angle";

	public SlashBrush (ISettingsService settingsService)
	{
		IntegerOption angleOption = new IntegerOption (
			AngleSettingName,
			0,
			180,
			45,
			Translations.GetString ("Angle")
		);
		angleOption.OnValueChanged += an => angle = an;
		angleOption.LoadValueFromSettings (settingsService);
		Options = [angleOption];
	}

	protected override RectangleI OnMouseMove (
		Context g,
		ImageSurface surface,
		BrushStrokeArgs strokeArgs)
	{
		int line_width = (int) g.LineWidth;

		PointI last_pos = strokeArgs.LastPosition;
		PointI current_pos = strokeArgs.CurrentPosition;

		double offsetX = (line_width / 2) * Math.Sin (Single.DegreesToRadians (angle));
		double offsetY = (line_width / 2) * Math.Cos (Single.DegreesToRadians (angle));

		PointD old_top = new (last_pos.X + offsetX, last_pos.Y + offsetY);
		PointD old_bottom = new (last_pos.X - offsetX, last_pos.Y - offsetY);
		PointD new_top = new (current_pos.X + offsetX, current_pos.Y + offsetY);
		PointD new_bottom = new (current_pos.X - offsetX, current_pos.Y - offsetY);

		// ensure the result has nonzero width even if e.g. angle is 0 or 180 and user is moving the mouse exactly up/down
		if (new_top.X == old_top.X) {
			old_top = new PointD (old_top.X + 1, old_top.Y);
			new_top = new PointD (new_top.X - 1, new_top.Y);
		}
		if (new_top.Y == old_top.Y) {
			old_top = new PointD (old_top.X, old_top.Y + 1);
			new_top = new PointD (new_top.X, new_top.Y - 1);
		}
		if (new_bottom.X == old_bottom.X) {
			old_bottom = new PointD (old_bottom.X + 1, old_bottom.Y);
			new_bottom = new PointD (new_bottom.X - 1, new_bottom.Y);
		}
		if (new_bottom.Y == old_bottom.Y) {
			old_bottom = new PointD (old_bottom.X, old_bottom.Y + 1);
			new_bottom = new PointD (new_bottom.X, new_bottom.Y - 1);
		}

		g.MoveTo (old_top.X, old_top.Y);
		g.LineTo (old_bottom.X, old_bottom.Y);
		g.LineTo (new_bottom.X, new_bottom.Y);
		g.LineTo (new_top.X, new_top.Y);
		g.ClosePath ();

		RectangleI dirty = g.StrokeExtents ().ToInt ();
		g.Fill ();

		return dirty;
	}


}
