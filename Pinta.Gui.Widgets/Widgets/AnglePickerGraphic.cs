/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Gdk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class AnglePickerGraphic : Gtk.DrawingArea
	{
		private PointD drag_start;
		private double angle_value;

		public AnglePickerGraphic ()
		{
			HeightRequest = WidthRequest = 50;

			SetDrawFunc ((area, context, width, height) => Draw (context));

			// Handle click + drag.
			var gesture = Gtk.GestureDrag.New ();
			gesture.SetButton (GtkExtensions.MouseLeftButton);

			gesture.OnDragBegin += (_, args) => {
				drag_start = new PointD (args.StartX, args.StartY);
				ProcessMouseEvent (drag_start, gesture.GetCurrentEventState ().IsShiftPressed ());
				gesture.SetState (Gtk.EventSequenceState.Claimed);
			};
			gesture.OnDragUpdate += (_, args) => {
				var drag_offset = new PointD (args.OffsetX, args.OffsetY);
				ProcessMouseEvent (drag_start + drag_offset, gesture.GetCurrentEventState ().IsShiftPressed ());
				gesture.SetState (Gtk.EventSequenceState.Claimed);
			};
			AddController (gesture);

			ValueChanged += (_, _) => QueueDraw ();
		}

		public int Value {
			get => (int) angle_value;
			set {
				var v = value % 360;
				if (angle_value != v) {
					angle_value = v;
					OnValueChanged ();
				}
			}
		}

		public double ValueDouble {
			get => angle_value;
			set {
				//double v = Math.IEEERemainder (value, 360.0);
				if (angle_value != value) {
					angle_value = value;
					OnValueChanged ();
				}
			}
		}

		private RectangleD GetDrawBounds ()
		{
			var rect = new RectangleD (0, 0, GetAllocatedWidth (), GetAllocatedHeight ());
			rect.Inflate (-1, -1);
			return rect;
		}

		private void ProcessMouseEvent (PointD pt, bool constrainAngle)
		{
			var rect = GetDrawBounds ();
			var diameter = Math.Min (rect.Width, rect.Height);
			var center = new PointD (rect.X + diameter * 0.5, rect.Y + diameter * 0.5);

			var dx = pt.X - center.X;
			var dy = pt.Y - center.Y;
			var theta = Math.Atan2 (-dy, dx);

			var newAngle = (theta * 360) / (2 * Math.PI);

			if (newAngle < 0)
				newAngle += 360;

			if (constrainAngle) {
				const double constraintAngle = 15.0;

				var multiple = newAngle / constraintAngle;
				var top = Math.Floor (multiple);
				var topDelta = Math.Abs (top - multiple);
				var bottom = Math.Ceiling (multiple);
				var bottomDelta = Math.Abs (bottom - multiple);

				double bestMultiple;

				if (bottomDelta < topDelta)
					bestMultiple = bottom;
				else
					bestMultiple = top;

				newAngle = bestMultiple * constraintAngle;
			}

			ValueDouble = newAngle;
		}

		private void Draw (Context g)
		{
#if false // TODO-GTK4 GdkRGBA bindings
			var color = GetStyleContext().GetColor (StateFlags).ToCairoColor ();
#else
			var color = new Color (1, 1, 1);
#endif
			var rect = GetDrawBounds ();
			var diameter = Math.Min (rect.Width, rect.Height);
			var radius = (diameter / 2.0);

			var center = new PointD (rect.X + radius, rect.Y + radius);

			var theta = (angle_value * 2.0 * Math.PI) / 360.0;

			var ellipseRect = new RectangleD (0, 0, diameter, diameter);
			var ellipseOutlineRect = ellipseRect;

			g.DrawEllipse (ellipseOutlineRect, color, 1);

			var endPointRadius = radius - 2;

			var endPoint = new PointD (
			    (float) (center.X + (endPointRadius * Math.Cos (theta))),
			    (float) (center.Y - (endPointRadius * Math.Sin (theta))));

			var gripSize = 2.5f;
			var gripEllipseRect = new RectangleD (center.X - gripSize, center.Y - gripSize, gripSize * 2, gripSize * 2);

			g.FillEllipse (gripEllipseRect, color);
			g.DrawLine (center, endPoint, color, 1);
		}

		public event EventHandler? ValueChanged;

		protected virtual void OnValueChanged () => ValueChanged?.Invoke (this, EventArgs.Empty);
	}
}
