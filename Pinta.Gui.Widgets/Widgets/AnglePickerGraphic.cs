// 
// AnglePickerWidget.cs
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
using Gdk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem (true)]
	public class AnglePickerGraphic : Gtk.DrawingArea
	{
		private bool tracking = false;
		private Point lastMouseXY;
		private double angleValue;

		public AnglePickerGraphic ()
		{
			Events = ((Gdk.EventMask)(16134));
			
			ButtonPressEvent += HandleHandleButtonPressEvent;
			ButtonReleaseEvent += HandleHandleButtonReleaseEvent;
			MotionNotifyEvent += HandleHandleMotionNotifyEvent;
		}

		#region Public Properties
		public int Value {
			get { return (int)angleValue; }
			set {
				double v = value % 360;
				if (angleValue != v) {
					angleValue = v;
					OnValueChanged ();
					this.GdkWindow.Invalidate ();
				}
			}
		}

		public double ValueDouble {
			get { return angleValue; }
			set {
				//double v = Math.IEEERemainder (value, 360.0);
				if (angleValue != value) {
					angleValue = value;
					OnValueChanged ();

					if (GdkWindow != null)
						GdkWindow.Invalidate ();
				}
			}
		}
		#endregion

		#region Mouse Handlers
		private void HandleHandleMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			ProcessMouseEvent (new Point ((int)args.Event.X, (int)args.Event.Y), (args.Event.State & ModifierType.ShiftMask) == ModifierType.ShiftMask);
		}

		private void HandleHandleButtonReleaseEvent (object o, Gtk.ButtonReleaseEventArgs args)
		{
			tracking = false;
		}

		private void HandleHandleButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			tracking = true;

			ProcessMouseEvent (new Point ((int)args.Event.X, (int)args.Event.Y), (args.Event.State & ModifierType.ShiftMask) == ModifierType.ShiftMask);
		}
		
		private void ProcessMouseEvent (Point pt, bool constrainAngle)
		{
			lastMouseXY = pt;

			if (tracking) {
				Rectangle ourRect = Rectangle.Inflate (GdkWindow.GetBounds (), -2, -2);
				int diameter = Math.Min (ourRect.Width, ourRect.Height);
				Point center = new Point (ourRect.X + (diameter / 2), ourRect.Y + (diameter / 2));

				int dx = lastMouseXY.X - center.X;
				int dy = lastMouseXY.Y - center.Y;
				double theta = Math.Atan2 (-dy, dx);

				double newAngle = (theta * 360) / (2 * Math.PI);

				if (newAngle < 0)
					newAngle = newAngle + 360;

				if (constrainAngle) {
					const double constraintAngle = 15.0;

					double multiple = newAngle / constraintAngle;
					double top = Math.Floor (multiple);
					double topDelta = Math.Abs (top - multiple);
					double bottom = Math.Ceiling (multiple);
					double bottomDelta = Math.Abs (bottom - multiple);

					double bestMultiple;
					if (bottomDelta < topDelta) {
						bestMultiple = bottom;
					} else {
						bestMultiple = top;
					}

					newAngle = bestMultiple * constraintAngle;
				}

				this.ValueDouble = newAngle;

				GdkWindow.Invalidate ();
			}
		}
		#endregion

		#region Drawing Code
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			base.OnExposeEvent (ev);

			using (Cairo.Context g = CairoHelper.Create (GdkWindow)) {
				Cairo.Rectangle ourRect = Rectangle.Inflate (GdkWindow.GetBounds (), -1, -1).ToCairoRectangle ();
				double diameter = Math.Min (ourRect.Width, ourRect.Height);

				double radius = (diameter / 2.0);

				Cairo.PointD center = new Cairo.PointD (
				    (float)(ourRect.X + radius),
				    (float)(ourRect.Y + radius));

				double theta = (this.angleValue * 2.0 * Math.PI) / 360.0;

				Cairo.Rectangle ellipseRect = new Cairo.Rectangle (ourRect.Location (), diameter, diameter);
				Cairo.Rectangle ellipseOutlineRect = ellipseRect;

				g.DrawEllipse (ellipseOutlineRect, new Cairo.Color (.1, .1, .1), 1);

				double endPointRadius = radius - 2;
				
				Cairo.PointD endPoint = new Cairo.PointD (
				    (float)(center.X + (endPointRadius * Math.Cos (theta))),
				    (float)(center.Y - (endPointRadius * Math.Sin (theta))));

				float gripSize = 2.5f;
				Cairo.Rectangle gripEllipseRect = new Cairo.Rectangle (center.X - gripSize, center.Y - gripSize, gripSize * 2, gripSize * 2);
				
				g.FillEllipse (gripEllipseRect, new Cairo.Color (.1, .1, .1));
				g.DrawLine (center, endPoint, new Cairo.Color (.1, .1, .1), 1);
			}
			
			return true;
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			// Calculate desired size here.
			requisition.Height = 50;
			requisition.Width = 50;
		}
		#endregion
		
		#region Public Events
		public event EventHandler ValueChanged;
		
		protected virtual void OnValueChanged ()
		{
			if (ValueChanged != null) {
				ValueChanged (this, EventArgs.Empty);
			}
		}
		#endregion
	}
}
