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
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class AnglePickerGraphic : Gtk.DrawingArea
{
	private PointD drag_start;
	private DegreesAngle angle_value;

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

	public DegreesAngle Value {
		get => angle_value;
		set {
			if (angle_value == value)
				return;
			angle_value = value;
			OnValueChanged ();
		}
	}

	private RectangleD GetDrawBounds ()
	{
		RectangleD rect = new (0, 0, GetAllocatedWidth (), GetAllocatedHeight ());
		return rect.Inflated (-1, -1);
	}

	private void ProcessMouseEvent (PointD pt, bool constrainAngle)
	{
		Value = CalculateNewAngle (pt, constrainAngle);
	}

	private DegreesAngle CalculateNewAngle (PointD pt, bool constrainAngle)
	{
		RectangleD rect = GetDrawBounds ();
		double diameter = Math.Min (rect.Width, rect.Height);
		PointD center = new (rect.X + diameter * 0.5, rect.Y + diameter * 0.5);
		PointD delta = pt - center;
		RadiansAngle theta = new (Math.Atan2 (-delta.Y, delta.X));
		DegreesAngle newAngle = theta.ToDegrees ();

		if (!constrainAngle)
			return newAngle;
		else
			return GetConstrainedAngle (newAngle);

		static DegreesAngle GetConstrainedAngle (DegreesAngle baseAngle)
		{
			const double constraint_angle = 15.0;
			double multiple = Math.Round(baseAngle.Degrees / constraint_angle);
			return new (multiple * constraint_angle);
		}
	}

	private readonly record struct AngleGraphicSettings (
		RectangleD ellipseOutlineRect,
		Color color,
		RectangleD gripEllipseRect,
		PointD center,
		PointD endPoint);

	private AngleGraphicSettings CreateSettings ()
	{
		GetStyleContext ().GetColor (out var color);

		RectangleD rect = GetDrawBounds ();

		double diameter = Math.Min (rect.Width, rect.Height);
		double radius = (diameter / 2.0);

		PointD center = new (rect.X + radius, rect.Y + radius);

		double theta = (angle_value.Degrees * 2.0 * Math.PI) / 360.0;

		RectangleD ellipseRect = new (0, 0, diameter, diameter);

		double endPointRadius = radius - 2;

		PointD endPoint = new (
			X: (float) (center.X + (endPointRadius * Math.Cos (theta))),
			Y: (float) (center.Y - (endPointRadius * Math.Sin (theta)))
		);

		const float gripSize = 2.5f;

		return new (
			ellipseOutlineRect: ellipseRect,
			color: color,
			gripEllipseRect: new RectangleD (center.X - gripSize, center.Y - gripSize, gripSize * 2, gripSize * 2),
			center: center,
			endPoint: endPoint
		);
	}

	private void Draw (Context g)
	{
		AngleGraphicSettings settings = CreateSettings ();
		g.DrawEllipse (settings.ellipseOutlineRect, settings.color, 1);
		g.FillEllipse (settings.gripEllipseRect, settings.color);
		g.DrawLine (settings.center, settings.endPoint, settings.color, 1);
	}

	public event EventHandler? ValueChanged;

	private void OnValueChanged ()
		=> ValueChanged?.Invoke (this, EventArgs.Empty);
}
