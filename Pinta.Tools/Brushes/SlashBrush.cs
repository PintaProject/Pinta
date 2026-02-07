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

	private int line_width;
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
		angleOption.OnValueChanged += an => {
			angle = an;
			SetSlashCursor ();
		};
		angleOption.LoadValueFromSettings (settingsService);
		Options = [angleOption];
	}

	protected override RectangleI OnMouseMove (
		Context g,
		ImageSurface surface,
		BrushStrokeArgs strokeArgs)
	{
		line_width = (int) g.LineWidth;

		PointD last_pos = strokeArgs.LastPosition.ToDouble ();
		PointD current_pos = strokeArgs.CurrentPosition.ToDouble ();

		PointD old_top = OffsetPoint (last_pos, -1, line_width / 2, angle);
		PointD old_bottom = OffsetPoint (last_pos, 1, line_width / 2, angle);
		PointD new_top = OffsetPoint (current_pos, -1, line_width / 2, angle);
		PointD new_bottom = OffsetPoint (current_pos, 1, line_width / 2, angle);

		/* 
			We want to avoid situations where no area is drawn because (for
			example) the user selected an angle of 0 and 180 and moved the
			mouse straight up or down. This code covers most such cases, but it
			is currently still possible to create such cases by moving the
			mouse very fast, so there is still some room for refinement of the
			present logic...
		*/

		double area = Math.Abs (0.5 * (old_top.X * new_top.Y - old_top.Y * new_top.X +
			new_top.X * new_bottom.Y - new_top.Y * new_bottom.X +
			new_bottom.X * old_bottom.Y - new_bottom.Y * old_bottom.X +
			old_bottom.X * old_top.Y - old_bottom.Y * old_top.X));

		if (area < 2) {
			old_top = OffsetPoint (old_top, -1, 1, angle + 90);
			new_top = OffsetPoint (new_top, -1, 1, angle + 90);
			old_bottom = OffsetPoint (old_bottom, 1, 1, angle + 90);
			new_bottom = OffsetPoint (new_bottom, 1, 1, angle + 90);
		}

		g.MoveTo (old_top.X, old_top.Y);
		g.LineTo (new_top.X, new_top.Y);
		g.LineTo (new_bottom.X, new_bottom.Y);
		g.LineTo (old_bottom.X, old_bottom.Y);

		g.Fill ();

		/*
			Anti-aliasing creates ugly effects because it anti-aliases at all
			edges of each rectangle, even the ones that are adjacent to the next
			one in the path, creating gaps in what should be a continuous line.
			
			Workaround: If anti-aliasing is enabled, create a non-antialiased
			stroke at the end to draw over the gap.
		*/

		if (g.Antialias != Antialias.None) {
			Antialias previous_antialias = g.Antialias;
			g.Antialias = Antialias.None;
			PointD antialias_correction_top = OffsetPoint (new_top, 1, 2, angle);
			PointD antialias_correction_bottom = OffsetPoint (new_bottom, -1, 2, angle);
			g.MoveTo (antialias_correction_top.X, antialias_correction_top.Y);
			g.LineTo (antialias_correction_bottom.X, antialias_correction_bottom.Y);
			g.LineWidth = 2;
			g.Stroke ();
			g.Antialias = previous_antialias;
			g.LineWidth = line_width;
		}

		RectangleI dirty = g.StrokeExtents ().ToInt ();

		return dirty;
	}

	public override void LoadCursor (int lineWidth)
	{
		line_width = lineWidth;
		SetSlashCursor ();
	}

	private PointD OffsetPoint (PointD point, double multiplier, double offset, float angle_in_degrees)
	{
		double offsetX = offset * Math.Sin (Single.DegreesToRadians (angle_in_degrees));
		double offsetY = offset * Math.Cos (Single.DegreesToRadians (angle_in_degrees));
		return new (Math.Round (point.X + multiplier * offsetX), Math.Round (point.Y + multiplier * offsetY));
	}

	private void SetSlashCursor ()
	{
		SetCursor (CursorShape.Rectangle, 2, line_width, angle);
	}

}
