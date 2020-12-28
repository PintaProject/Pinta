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
		private bool tracking = false;
		private Gdk.Point last_mouse_xy;
		private double angle_value;

		public AnglePickerGraphic ()
		{
			Events = ((EventMask) (16134));

			ButtonPressEvent += HandleHandleButtonPressEvent;
			ButtonReleaseEvent += HandleHandleButtonReleaseEvent;
			MotionNotifyEvent += HandleHandleMotionNotifyEvent;
		}

		public int Value {
			get => (int) angle_value;
			set {
				var v = value % 360;
				if (angle_value != v) {
					angle_value = v;
					OnValueChanged ();
					Window.Invalidate ();
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

					if (Window != null)
						Window.Invalidate ();
				}
			}
		}

		private void HandleHandleMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			ProcessMouseEvent (new Gdk.Point ((int) args.Event.X, (int) args.Event.Y), (args.Event.State & ModifierType.ShiftMask) == ModifierType.ShiftMask);
		}

		private void HandleHandleButtonReleaseEvent (object o, Gtk.ButtonReleaseEventArgs args)
		{
			tracking = false;
		}

		private void HandleHandleButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			tracking = true;

			ProcessMouseEvent (new Gdk.Point ((int) args.Event.X, (int) args.Event.Y), args.Event.IsShiftPressed ());
		}

		private void ProcessMouseEvent (Gdk.Point pt, bool constrainAngle)
		{
			last_mouse_xy = pt;

			if (tracking) {
				var ourRect = Gdk.Rectangle.Inflate (Window.GetBounds (), -2, -2);
				var diameter = Math.Min (ourRect.Width, ourRect.Height);
				var center = new Gdk.Point (ourRect.X + (diameter / 2), ourRect.Y + (diameter / 2));

				var dx = last_mouse_xy.X - center.X;
				var dy = last_mouse_xy.Y - center.Y;
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

				Window.Invalidate ();
			}
		}

		protected override bool OnDrawn (Context g)
		{
			base.OnDrawn (g);

			var ourRect = Gdk.Rectangle.Inflate (Window.GetBounds (), -1, -1).ToCairoRectangle ();

			var diameter = Math.Min (ourRect.Width, ourRect.Height);
			var radius = (diameter / 2.0);

			var center = new PointD (
			    (float) (ourRect.X + radius),
			    (float) (ourRect.Y + radius));

			var theta = (angle_value * 2.0 * Math.PI) / 360.0;

			var ellipseRect = new Cairo.Rectangle (ourRect.Location (), diameter, diameter);
			var ellipseOutlineRect = ellipseRect;

			g.DrawEllipse (ellipseOutlineRect, new Cairo.Color (.1, .1, .1), 1);

			var endPointRadius = radius - 2;

			var endPoint = new PointD (
			    (float) (center.X + (endPointRadius * Math.Cos (theta))),
			    (float) (center.Y - (endPointRadius * Math.Sin (theta))));

			var gripSize = 2.5f;
			var gripEllipseRect = new Cairo.Rectangle (center.X - gripSize, center.Y - gripSize, gripSize * 2, gripSize * 2);

			g.FillEllipse (gripEllipseRect, new Cairo.Color (.1, .1, .1));
			g.DrawLine (center, endPoint, new Cairo.Color (.1, .1, .1), 1);

			return true;
		}

		protected override void OnGetPreferredHeight (out int minimum_height, out int natural_height)
		{
			minimum_height = natural_height = 50;
		}

		protected override void OnGetPreferredWidth (out int minimum_width, out int natural_width)
		{
			minimum_width = natural_width = 50;
		}

		public event EventHandler? ValueChanged;

		protected virtual void OnValueChanged () => ValueChanged?.Invoke (this, EventArgs.Empty);
	}
}
