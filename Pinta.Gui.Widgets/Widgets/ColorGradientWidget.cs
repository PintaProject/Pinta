//
// ColorGradientWidget.cs
//
// Author:
//      Krzysztof Marecki <marecki.krzysztof@gmail.com>
//
// Copyright (c) 2010 Krzysztof Marecki
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
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cairo;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class ColorGradientWidget : Gtk.DrawingArea
{
	private const double X_pad = 0.15; // gradient horizontal padding
	private const double Y_pad = 0.03; // gradient vertical padding

	private double[] vals;
	private PointI last_mouse_pos = new (0, 0);

	public ColorGradientWidget (int count)
	{
		CanFocus = true;
		Count = count;

		ValueIndex = -1;

		SetDrawFunc ((_, context, _, _) => Draw (context));

		var motion_controller = Gtk.EventControllerMotion.New ();
		motion_controller.OnMotion += HandleMotionNotifyEvent;
		motion_controller.OnLeave += HandleLeaveNotifyEvent;
		AddController (motion_controller);

		ClickGesture = Gtk.GestureClick.New ();
		ClickGesture.SetButton (0); // Handle all buttons
		ClickGesture.OnPressed += HandleButtonPressEvent;
		ClickGesture.OnReleased += HandleButtonReleaseEvent;
		AddController (ClickGesture);
	}

	public Gtk.GestureClick ClickGesture { get; }

	private RectangleD GradientRectangle {
		get {
			RectangleD rect = GetAllocation ();
			return new (
				X: rect.X + X_pad * rect.Width,
				Y: rect.Y + Y_pad * rect.Height,
				Width: (1 - 2 * X_pad) * rect.Width,
				Height: (1 - 2 * Y_pad) * rect.Height);
		}
	}

	public int Count {

		get => vals.Length;

		[MemberNotNull (nameof (vals))]
		set {
			if (value < 2 || value > 3)
				throw new ArgumentOutOfRangeException (nameof (value), value, "Count must be 2 or 3");

			vals = new double[value];

			int step = 256 / (value - 1);

			for (int i = 0; i < value; i++)
				vals[i] = i * step - ((i != 0) ? 1 : 0);
		}
	}

	private Color max_color;
	public Color MaxColor {
		get => max_color;
		set {
			max_color = value;
			QueueDraw ();
		}
	}

	public int ValueIndex { get; private set; }

	public int GetValue (int i)
		=> (int) vals[i];

	public void SetValue (int i, int val)
	{
		if ((int) vals[i] == val) return;
		vals[i] = val;
		OnValueChanged (i);
	}

	private RectangleD GetAllocation ()
		=> new (
			0,
			0,
			GetAllocatedWidth (),
			GetAllocatedHeight ()
		);

	private double GetYFromValue (double val)
	{
		RectangleD rect = GradientRectangle;
		RectangleD all = GetAllocation ();
		return all.Y + Y_pad * all.Height + rect.Height * (255 - val) / 255;
	}

	private double NormalizeY (
		int index,
		double py)
	{
		RectangleD rect = GradientRectangle;

		var yvals = (
			vals
			.Select (GetYFromValue)
			.Concat (
				[
					rect.Y,
					rect.Y + rect.Height
				]
			)
			.OrderByDescending (v => v)
			.ToImmutableArray ()
		);

		index++;

		if (py >= yvals[index - 1])
			return yvals[index - 1];
		else if (py < yvals[index + 1])
			return yvals[index + 1];
		else
			return py;
	}

	private int GetValueFromY (double py)
	{
		RectangleD rect = GradientRectangle;
		RectangleD all = GetAllocation ();
		double y = py - (all.Y + Y_pad * all.Height);
		return (int) (255 * (rect.Height - y) / rect.Height);
	}

	private int FindValueIndex (int y)
	{
		if (ValueIndex != -1)
			return ValueIndex;

		var yvals =
			vals
			.Select (GetYFromValue)
			.ToImmutableArray ();

		int count = Count - 1;

		for (int i = 0; i < count; i++) {

			double y1 = yvals[i];
			double y2 = yvals[i + 1];

			double h = (y1 - y2) / 2;

			// pointer is below the lowest value triangle
			if (i == 0 && y1 < y)
				return i;

			// pointer is above the highest value triangle
			if (i == (count - 1) && y2 > y)
				return i + 1;

			// pointer is outside i and i + 1 value triangles
			if (!(y1 >= y && y >= y2))
				continue;

			// pointer is closer to lower value triangle
			if (y1 - y <= h)
				return i;

			// pointer is closer to higher value triangle
			if (y - y2 <= h)
				return i + 1;
		}

		return -1;
	}

	private void HandleMotionNotifyEvent (
		Gtk.EventControllerMotion controller,
		Gtk.EventControllerMotion.MotionSignalArgs args)
	{
		PointI p = new (
			X: (int) args.X,
			Y: (int) args.Y);

		int index = FindValueIndex (p.Y);
		p = p with { Y = (int) NormalizeY (index, p.Y) };

		if (controller.GetCurrentEventState ().IsLeftMousePressed ()) {
			if (index != -1) {
				int y = GetValueFromY (p.Y);

				vals[index] = y;
				OnValueChanged (index);
			}
		}

		last_mouse_pos = p;

		// to avoid unnecessary costly redrawing
		if (index != -1)
			QueueDraw ();
	}

	private void HandleLeaveNotifyEvent (
		Gtk.EventControllerMotion controller,
		EventArgs args)
	{
		if (!controller.GetCurrentEventState ().IsLeftMousePressed ())
			ValueIndex = -1;

		QueueDraw ();
	}

	private void HandleButtonPressEvent (
		Gtk.GestureClick controller,
		Gtk.GestureClick.PressedSignalArgs args)
	{
		int index = FindValueIndex ((int) args.Y);

		if (index != -1)
			ValueIndex = index;
	}

	private void HandleButtonReleaseEvent (
		Gtk.GestureClick controller,
		Gtk.GestureClick.ReleasedSignalArgs args)
	{
		ValueIndex = -1;
	}

	private void DrawGradient (Context g)
	{
		RectangleD rect = GradientRectangle;
		LinearGradient pat = new (rect.X, rect.Y, rect.X, rect.Y + rect.Height);

		pat.AddColorStop (0, MaxColor);
		pat.AddColorStop (1, new Cairo.Color (0, 0, 0));

		g.Rectangle (rect);
		g.SetSource (pat);
		g.Fill ();
	}

	private void DrawTriangles (Context g)
	{
		GetStyleContext ().GetColor (out Gdk.RGBA hover_color);
		Cairo.Color inactive_color = hover_color.ToCairoColor () with { A = 0.5 };

		int px = last_mouse_pos.X;
		int py = last_mouse_pos.Y;

		RectangleD rect = GradientRectangle;
		RectangleD all = GetAllocation ();

		int index = FindValueIndex (py);

		for (int i = 0; i < Count; i++) {

			double val = vals[i];
			double y = GetYFromValue (val);
			bool hover = (index == i) && (all.ContainsPoint (px, py) || ValueIndex != -1);
			Cairo.Color color = hover ? hover_color.ToCairoColor () : inactive_color;

			DrawLeftTriangle (g, rect, y, color);
			DrawRightTriangle (g, rect, y, color);
		}
	}

	private static void DrawRightTriangle (
		Context g,
		RectangleD rect,
		double y,
		Color color)
	{
		double x = rect.X + rect.Width;

		// right triangle
		ReadOnlySpan<PointD> points = [
			new (x, y),
			new (x + X_pad * rect.Width, y + Y_pad * rect.Height),
			new (x + X_pad * rect.Width, y - Y_pad * rect.Height),
		];

		g.FillPolygonal (points, color);
	}

	private static void DrawLeftTriangle (
		Context g,
		RectangleD rect,
		double y,
		Color color)
	{
		// left triangle
		ReadOnlySpan<PointD> points = [
			new (rect.X, y),
			new (rect.X - X_pad * rect.Width, y + Y_pad * rect.Height),
			new (rect.X - X_pad * rect.Width, y - Y_pad * rect.Height),
		];

		g.FillPolygonal (points, color);
	}

	private void Draw (Context g)
	{
		DrawGradient (g);
		DrawTriangles (g);

		g.Dispose ();
	}

	private void OnValueChanged (int index)
		=> ValueChanged?.Invoke (this, new IndexEventArgs (index));

	public event EventHandler<IndexEventArgs>? ValueChanged;
}
