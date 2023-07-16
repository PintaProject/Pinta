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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cairo;
using GObject;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class ColorGradientWidget : Gtk.DrawingArea
	{
		private const double xpad = 0.15;       // gradient horizontal padding							
		private const double ypad = 0.03;       // gradient vertical padding

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

		public Gtk.GestureClick ClickGesture { get; private init; }

		private RectangleD GradientRectangle {
			get {
				var rect = GetAllocation ();
				var x = rect.X + xpad * rect.Width;
				var y = rect.Y + ypad * rect.Height;
				var width = (1 - 2 * xpad) * rect.Width;
				var height = (1 - 2 * ypad) * rect.Height;

				return new RectangleD (x, y, width, height);
			}
		}

		public int Count {
			get => vals.Length;
			[MemberNotNull (nameof (vals))]
			set {
				if (value < 2 || value > 3)
					throw new ArgumentOutOfRangeException (nameof(value), value, "Count must be 2 or 3");

				vals = new double[value];

				var step = 256 / (value - 1);

				for (var i = 0; i < value; i++)
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
		{
			return (int) vals[i];
		}

		public void SetValue (int i, int val)
		{
			if ((int) vals[i] != val) {
				vals[i] = val;
				OnValueChanged (i);
			}
		}

		private RectangleD GetAllocation () => new RectangleD (0, 0, GetAllocatedWidth (), GetAllocatedHeight ());

		private double GetYFromValue (double val)
		{
			var rect = GradientRectangle;
			var all = GetAllocation ();

			return all.Y + ypad * all.Height + rect.Height * (255 - val) / 255;
		}

		private double NormalizeY (int index, double py)
		{
			var rect = GradientRectangle;
			var yvals = (from val in vals select GetYFromValue (val)).Concat (
				      new double[] { rect.Y, rect.Y + rect.Height }).OrderByDescending (
				      v => v).ToArray ();
			index++;

			if (py >= yvals[index - 1])
				py = yvals[index - 1];
			else if (py < yvals[index + 1])
				py = yvals[index + 1];

			return py;
		}

		private int GetValueFromY (double py)
		{
			var rect = GradientRectangle;
			var all = GetAllocation ();

			py -= all.Y + ypad * all.Height;
			return ((int) (255 * (rect.Height - py) / rect.Height));
		}

		private int FindValueIndex (int y)
		{
			if (ValueIndex == -1) {
				var yvals = (from val in vals select GetYFromValue (val)).ToArray ();
				var count = Count - 1;

				for (var i = 0; i < count; i++) {
					var y1 = yvals[i];
					var y2 = yvals[i + 1];
					var h = (y1 - y2) / 2;

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
					if (y1 - y <= h) return i;
					// pointer is closer to higher value triangle
					if (y - y2 <= h) return i + 1;
				}

				return -1;
			} else {
				return ValueIndex;
			}
		}

		private void HandleMotionNotifyEvent (EventControllerMotion controller, EventControllerMotion.MotionSignalArgs args)
		{
			int px = (int) args.X;
			int py = (int) args.Y;

			var index = FindValueIndex (py);
			py = (int) NormalizeY (index, py);

			if (controller.GetCurrentEventState ().IsLeftMousePressed ()) {
				if (index != -1) {
					var y = GetValueFromY (py);

					vals[index] = y;
					OnValueChanged (index);
				}
			}

			last_mouse_pos = new (px, py);

			// to avoid unnecessary costly redrawing
			if (index != -1)
				QueueDraw ();
		}

		private void HandleLeaveNotifyEvent (EventControllerMotion controller, EventArgs args)
		{
			if (!controller.GetCurrentEventState ().IsLeftMousePressed ())
				ValueIndex = -1;

			QueueDraw ();
		}

		private void HandleButtonPressEvent (GestureClick controller, GestureClick.PressedSignalArgs args)
		{
			var index = FindValueIndex ((int) args.Y);

			if (index != -1)
				ValueIndex = index;
		}

		private void HandleButtonReleaseEvent (GestureClick controller, GestureClick.ReleasedSignalArgs args)
		{
			ValueIndex = -1;
		}

		private void DrawGradient (Context g)
		{
			var rect = GradientRectangle;

			var pat = new LinearGradient (rect.X, rect.Y, rect.X, rect.Y + rect.Height);
			pat.AddColorStop (0, MaxColor);
			pat.AddColorStop (1, new Cairo.Color (0, 0, 0));

			g.Rectangle (rect);
			g.SetSource (pat);
			g.Fill ();
		}

		private void DrawTriangles (Context g)
		{
			GetStyleContext ().GetColor (out var hover_color);
			var inactive_color = hover_color with { A = 0.5 };

			int px = last_mouse_pos.X;
			int py = last_mouse_pos.Y;

			var rect = GradientRectangle;
			var all = GetAllocation ();

			var index = FindValueIndex (py);

			for (var i = 0; i < Count; i++) {

				var val = vals[i];
				var y = GetYFromValue (val);
				var hover = ((index == i)) && (all.ContainsPoint (px, py) || ValueIndex != -1);
				var color = hover ? hover_color : inactive_color;

				// left triangle
				var points = new PointD[] { new PointD (rect.X, y),
							    new PointD (rect.X - xpad * rect.Width, y + ypad * rect.Height),
							    new PointD (rect.X - xpad * rect.Width, y - ypad * rect.Height)};

				g.FillPolygonal (points, color);

				var x = rect.X + rect.Width;

				// right triangle
				var points2 = new PointD[] { new PointD (x , y),
							     new PointD (x + xpad * rect.Width, y + ypad * rect.Height),
							     new PointD (x + xpad * rect.Width, y - ypad * rect.Height)};

				g.FillPolygonal (points2, color);
			}
		}

		private void Draw (Context g)
		{
			DrawGradient (g);
			DrawTriangles (g);
		}

		protected void OnValueChanged (int index) => ValueChanged?.Invoke (this, new IndexEventArgs (index));

		public event EventHandler<IndexEventArgs>? ValueChanged;
	}
}
