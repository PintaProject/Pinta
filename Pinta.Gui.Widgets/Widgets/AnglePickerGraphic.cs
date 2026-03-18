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
	private NumberRange<int>? allowed_range;

	public AnglePickerGraphic ()
	{
		HeightRequest = WidthRequest = 50;

		SetDrawFunc ((area, context, width, height) => Draw (context));

		// Handle click + drag.
		var gesture = Gtk.GestureDrag.New ();
		gesture.SetButton (GtkExtensions.MOUSE_LEFT_BUTTON);

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
			DegreesAngle clamped = ClampToAllowedRange (value);
			if (angle_value == clamped) return;
			angle_value = clamped;
			OnValueChanged ();
		}
	}

	public NumberRange<int>? AllowedRange {
		get => allowed_range;
		set {
			if (value is NumberRange<int> range) {
				if (range.Lower < 0 || range.Upper > 360)
					throw new ArgumentOutOfRangeException (nameof (value), "Range must be within [0, 360]");
			}
			if (allowed_range == value) return;
			allowed_range = value;
			Value = ClampToAllowedRange (angle_value);
			QueueDraw ();
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

		if (constrainAngle)
			newAngle = GetConstrainedAngle (newAngle);

		return ClampToAllowedRange (newAngle);

		static DegreesAngle GetConstrainedAngle (DegreesAngle baseAngle)
		{
			const double constraint_angle = 15.0;
			double multiple = Math.Round (baseAngle.Degrees / constraint_angle);
			return new (multiple * constraint_angle);
		}
	}

	private DegreesAngle ClampToAllowedRange (DegreesAngle angle)
	{
		if (allowed_range is not NumberRange<int> range)
			return angle;

		double degrees = angle.Degrees;
		if (degrees >= range.Lower && degrees <= range.Upper)
			return angle;

		double distToLower = AngularDistance (degrees, range.Lower);
		double distToUpper = AngularDistance (degrees, range.Upper);

		return new DegreesAngle (distToLower <= distToUpper ? range.Lower : range.Upper);
	}

	private static double AngularDistance (double from, double to)
	{
		double diff = ((to - from) % 360 + 360) % 360;
		return Math.Min (diff, 360 - diff);
	}

	private readonly record struct AngleGraphicSettings (
		RectangleD ellipseOutlineRect,
		Color color,
		RectangleD gripEllipseRect,
		PointD center,
		PointD endPoint);

	private AngleGraphicSettings CreateSettings ()
	{
		GetStyleContext ().GetColor (out Gdk.RGBA color);

		RectangleD rect = GetDrawBounds ();

		double diameter = Math.Min (rect.Width, rect.Height);
		double radius = diameter / 2.0;

		PointD center = new (rect.X + radius, rect.Y + radius);

		RadiansAngle theta = angle_value.ToRadians ();

		RectangleD ellipseRect = new (0, 0, diameter, diameter);

		double endPointRadius = radius - 2;

		PointD endPoint = new (
			X: (float) (center.X + (endPointRadius * Math.Cos (theta.Radians))),
			Y: (float) (center.Y - (endPointRadius * Math.Sin (theta.Radians)))
		);

		const float gripSize = 2.5f;

		return new (
			ellipseOutlineRect: ellipseRect,
			color: color.ToCairoColor (),
			gripEllipseRect: new RectangleD (center.X - gripSize, center.Y - gripSize, gripSize * 2, gripSize * 2),
			center: center,
			endPoint: endPoint
		);
	}

	private void Draw (Context g)
	{
		AngleGraphicSettings settings = CreateSettings ();

		if (allowed_range is NumberRange<int> range && range.Upper - range.Lower < 360)
			DrawUnavailableSector (g, settings, range);

		g.DrawEllipse (settings.ellipseOutlineRect, settings.color, 1);
		g.FillEllipse (settings.gripEllipseRect, settings.color);
		g.DrawLine (settings.center, settings.endPoint, settings.color, 1);

		g.Dispose ();
	}

	private static void DrawUnavailableSector (Context g, AngleGraphicSettings settings, NumberRange<int> range)
	{
		double radius = settings.ellipseOutlineRect.Width / 2.0;
		PointD center = settings.center;

		double upperRad = range.Upper * Math.PI / 180.0;
		double lowerRad = range.Lower * Math.PI / 180.0;

		g.MoveTo (center.X, center.Y);
		g.LineTo (
			center.X + radius * Math.Cos (upperRad),
			center.Y - radius * Math.Sin (upperRad));

		// Arc through the unavailable region (from Upper to Lower the "long way")
		g.ArcNegative (center.X, center.Y, radius, -upperRad, -lowerRad);
		g.ClosePath ();

		Color fillColor = new (settings.color.R, settings.color.G, settings.color.B, 0.15);
		g.SetSourceColor (fillColor);
		g.Fill ();
	}

	public event EventHandler? ValueChanged;

	private void OnValueChanged ()
		=> ValueChanged?.Invoke (this, EventArgs.Empty);
}
