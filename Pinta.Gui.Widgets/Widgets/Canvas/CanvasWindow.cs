//
// CanvasWindow.cs
//
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
//
// Copyright (c) 2015 Jonathan Pobst
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
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta;

public sealed class CanvasWindow : Gtk.Grid
{
	private readonly Document document;

	private readonly Ruler horizontal_ruler;
	private readonly Ruler vertical_ruler;
	private readonly Gtk.ScrolledWindow scrolled_window;
	private readonly Gtk.EventControllerMotion motion_controller;
	private readonly Gtk.GestureZoom gesture_zoom;

	private PointD current_window_pos = PointD.Zero;
	private PointD current_canvas_pos = PointD.Zero;
	private double cumulative_zoom_amount;
	private double last_scale_delta;

	private const double ZOOM_THRESHOLD_SCROLL = 1.25;
	private const double ZOOM_THRESHOLD_PINCH = 0.15;

	public PintaCanvas Canvas { get; }

	public CanvasWindow (
		ChromeManager chrome,
		ToolManager tools,
		Document document,
		ICanvasGridService canvasGrid)
	{
		Gtk.GestureZoom gestureZoom = Gtk.GestureZoom.New ();
		gestureZoom.SetPropagationPhase (Gtk.PropagationPhase.Bubble);
		gestureZoom.OnScaleChanged += HandleGestureZoomScaleChanged;
		gestureZoom.OnEnd += (_, _) => cumulative_zoom_amount = last_scale_delta = 0;
		gestureZoom.OnCancel += (_, _) => cumulative_zoom_amount = last_scale_delta = 0;

		Gtk.EventControllerScroll scrollController = Gtk.EventControllerScroll.New (Gtk.EventControllerScrollFlags.BothAxes); // Both axes must be captured so the zoom gesture can cancel them
		scrollController.OnScroll += HandleScrollEvent;
		scrollController.OnDecelerate += (_, _) => gestureZoom.IsActive (); // Cancel scroll deceleration when zooming

		PintaCanvas canvas = new (
			chrome,
			tools,
			this,
			document,
			canvasGrid
		) {
			// For CSS: add a drop shadow outline to the canvas to give it a clear border
			// when the image is close to the background color.
			Name = "canvas",
		};

		Gtk.Viewport viewPort = new ();
		viewPort.AddController (scrollController);
		viewPort.Child = canvas;

		Gtk.ScrolledWindow scrolledWindow = new () {
			Hexpand = true,
			Vexpand = true,
			Child = viewPort,
		};

		Ruler horizontalRuler = new (Gtk.Orientation.Horizontal) {
			Metric = MetricType.Pixels,
			Visible = false,
		};

		Ruler verticalRuler = new (Gtk.Orientation.Vertical) {
			Metric = MetricType.Pixels,
			Visible = false,
		};

		Gtk.EventControllerMotion motionController = Gtk.EventControllerMotion.New ();
		motionController.OnMotion += HandleMotion;

		// --- Initialization (Gtk.Widget)

		// The mouse handler in PintaCanvas grabs focus away from toolbar widgets, along
		// with DocumentWorkpace.GrabFocusToCanvas()
		Focusable = true;

		AddController (gestureZoom);
		AddController (motionController);

		// --- Initialization (Gtk.Grid)

		ColumnHomogeneous = false;
		RowHomogeneous = false;

		Attach (horizontalRuler, 1, 0, 1, 1);
		Attach (verticalRuler, 0, 1, 1, 1);
		Attach (scrolledWindow, 1, 1, 1, 1);

		// --- References to keep

		Canvas = canvas;

		this.document = document;

		scrolled_window = scrolledWindow;
		gesture_zoom = gestureZoom;
		horizontal_ruler = horizontalRuler;
		vertical_ruler = verticalRuler;
		motion_controller = motionController;

		// --- Further initialization

		// Update the ruler when the horizontal or vertical size has changed.
		// This can happen either from the canvas size changing (e.g. zooming),
		// or when the window is resized and the scroll area's size changes.
		scrolledWindow.Hadjustment!.OnChanged += UpdateRulerRange;
		scrolledWindow.Vadjustment!.OnChanged += UpdateRulerRange;

		// Update the ruler when scrolling around.
		scrolledWindow.Hadjustment!.OnValueChanged += UpdateRulerRange;
		scrolledWindow.Vadjustment!.OnValueChanged += UpdateRulerRange;

		// Also update if the view size changed without affecting the size of
		// the canvas widget (e.g. when zoomed out and no scrollbars are required)
		document.Workspace.ViewSizeChanged += UpdateRulerRange;
		document.SelectionChanged += UpdateRulerSelection;
	}

	private void UpdateRulerSelection (object? sender, EventArgs e)
	{
		if (document.Selection.Visible) {
			RectangleD bounds = document.Selection.GetBounds ();
			var horizontalBounds = NumberRange.Create (bounds.Left, bounds.Left + bounds.Width);
			var verticalBounds = NumberRange.Create (bounds.Top, bounds.Top + bounds.Height);
			horizontal_ruler.SelectionBounds = horizontalBounds;
			vertical_ruler.SelectionBounds = verticalBounds;
		} else {
			// If there's no selection, clear the highlight
			horizontal_ruler.SelectionBounds = null;
			vertical_ruler.SelectionBounds = null;
		}
	}

	private void HandleMotion (
		Gtk.EventControllerMotion _,
		Gtk.EventControllerMotion.MotionSignalArgs args)
	{
		PointD newPosition = new (args.X, args.Y);

		// These coordinates are relative to our grid widget, so transform into the child image
		// view's coordinates, and then to the canvas coordinates.
		this.TranslateCoordinates (Canvas, newPosition, out PointD viewPos);

		current_window_pos = newPosition;
		current_canvas_pos = document.Workspace.ViewPointToCanvas (viewPos);
		horizontal_ruler.Position = current_canvas_pos.X;
		vertical_ruler.Position = current_canvas_pos.Y;
	}

	private void HandleGestureZoomScaleChanged (object? sender, EventArgs e)
	{
		// Allow the user to zoom in/out by pinching the trackpad
		double pinchDelta = gesture_zoom.GetScaleDelta () - 1 - last_scale_delta;
		if (pinchDelta < 0) {
			if (cumulative_zoom_amount > 0)
				cumulative_zoom_amount = 0; // Reset the counter if the user changes direction so that changing direction doesn't take extra movement

			cumulative_zoom_amount += pinchDelta;
			if (cumulative_zoom_amount <= -ZOOM_THRESHOLD_PINCH) {
				document.Workspace.ZoomOutAroundCanvasPoint (current_canvas_pos);
				cumulative_zoom_amount = 0;
			}
		} else {
			if (cumulative_zoom_amount < 0)
				cumulative_zoom_amount = 0;

			cumulative_zoom_amount += pinchDelta;
			if (cumulative_zoom_amount >= ZOOM_THRESHOLD_PINCH) {
				document.Workspace.ZoomInAroundCanvasPoint (current_canvas_pos);
				cumulative_zoom_amount = 0;
			}
		}
		last_scale_delta = gesture_zoom.GetScaleDelta () - 1;
	}

	public PointD WindowMousePosition
		=> current_window_pos;

	public bool IsMouseOnCanvas
		=> motion_controller.ContainsPointer;

	public bool RulersVisible {
		get => horizontal_ruler.Visible;
		set {
			if (horizontal_ruler.Visible == value) return;
			horizontal_ruler.Visible = value;
			vertical_ruler.Visible = value;
		}
	}

	public MetricType RulerMetric {
		get => horizontal_ruler.Metric;
		set {
			if (horizontal_ruler.Metric == value) return;
			horizontal_ruler.Metric = value;
			vertical_ruler.Metric = value;
		}
	}

	public void UpdateRulerRange (object? sender, EventArgs e)
	{
		PointD lower = PointD.Zero;
		PointD upper = PointD.Zero;

		if (scrolled_window.Hadjustment == null || scrolled_window.Vadjustment == null)
			return;

		DocumentWorkspace workspace = document.Workspace;

		Gtk.Widget viewport = scrolled_window.Child!;
		Size viewSize = workspace.ViewSize;
		PointD offset = new (
			(viewport.GetAllocatedWidth () - viewSize.Width) / 2,
			(viewport.GetAllocatedHeight () - viewSize.Height) / 2);

		if (offset.X > 0) {
			lower = lower with { X = -offset.X / workspace.Scale };
			upper = upper with { X = document.ImageSize.Width - lower.X };
		} else {
			lower = lower with { X = scrolled_window.Hadjustment.Value / workspace.Scale };
			upper = upper with { X = (scrolled_window.Hadjustment.Value + scrolled_window.Hadjustment.PageSize) / workspace.Scale };
		}

		if (offset.Y > 0) {
			lower = lower with { Y = -offset.Y / workspace.Scale };
			upper = upper with { Y = document.ImageSize.Height - lower.Y };
		} else {
			lower = lower with { Y = scrolled_window.Vadjustment.Value / workspace.Scale };
			upper = upper with { Y = (scrolled_window.Vadjustment.Value + scrolled_window.Vadjustment.PageSize) / workspace.Scale };
		}

		horizontal_ruler.RulerRange = new (lower.X, upper.X);
		vertical_ruler.RulerRange = new (lower.Y, upper.Y);
	}

	private bool HandleScrollEvent (
		Gtk.EventControllerScroll controller,
		Gtk.EventControllerScroll.ScrollSignalArgs args)
	{
		if (gesture_zoom.IsActive ())
			return true;
		// Allow the user to zoom in/out with Ctrl-Mousewheel or Ctrl-two-finger-scroll
		if (!controller.GetCurrentEventState ().IsControlPressed ())
			return false;

		// "clicky" scroll wheels generate 1 or -1

		if (args.Dy == -1) {
			document.Workspace.ZoomInAroundCanvasPoint (current_canvas_pos);
			return true;
		}

		if (args.Dy == 1) {
			document.Workspace.ZoomOutAroundCanvasPoint (current_canvas_pos);
			return true;
		}

		// analog scroll wheels and scrolling on a touchpad generates a range of values constantly as the user scrolls
		// this might feel "backwards" on a touchpad to some people
		if (args.Dy < 0) {
			if (cumulative_zoom_amount > 0)
				cumulative_zoom_amount = 0;

			cumulative_zoom_amount += args.Dy;
			if (cumulative_zoom_amount <= -ZOOM_THRESHOLD_SCROLL) {
				document.Workspace.ZoomInAroundCanvasPoint (current_canvas_pos);
				cumulative_zoom_amount = 0;
			}

		} else {
			if (cumulative_zoom_amount < 0)
				cumulative_zoom_amount = 0;

			cumulative_zoom_amount += args.Dy;
			if (cumulative_zoom_amount >= ZOOM_THRESHOLD_SCROLL) {
				document.Workspace.ZoomOutAroundCanvasPoint (current_canvas_pos);
				cumulative_zoom_amount = 0;
			}

		}

		return true;
	}
}
