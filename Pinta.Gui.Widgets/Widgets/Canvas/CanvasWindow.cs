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
	private readonly ChromeManager chrome;
	private readonly ToolManager tools;

	private readonly RulerViewModel horizontal_ruler;
	private readonly RulerViewModel vertical_ruler;
	private readonly Gtk.ScrolledWindow scrolled_window;
	private readonly Gtk.EventControllerMotion motion_controller;
	private readonly Gtk.GestureDrag drag_controller;
	private readonly Gtk.GestureZoom gesture_zoom;

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
			tools,
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

		// Use the drag gesture to forward a sequence of mouse press -> move -> release events to the current tool.
		// This is more reliable than using just a click gesture in combination with the move controller (see bug #1456)
		// Note that we attach this to the root canvas widget, not the canvas, so that it can receive drags that start outside the canvas.
		Gtk.GestureDrag dragController = Gtk.GestureDrag.New ();
		dragController.SetButton (0); // Listen for all mouse buttons.
		dragController.OnDragBegin += OnDragBegin;
		dragController.OnDragUpdate += OnDragUpdate;
		dragController.OnDragEnd += OnDragEnd;

		Gtk.ScrolledWindow scrolledWindow = new () {
			Hexpand = true,
			Vexpand = true,
			Child = viewPort,
		};

		RulerModel horizontalRulerModel = new (Gtk.Orientation.Horizontal) {
			Metric = MetricType.Pixels,
		};
		RulerModel verticalRulerModel = new (Gtk.Orientation.Vertical) {
			Metric = MetricType.Pixels,
		};

		RulerViewModel horizontalRulerViewModel = new (horizontalRulerModel);
		RulerViewModel verticalRulerViewModel = new (verticalRulerModel);

		RulerView horizontalRuler = new (horizontalRulerViewModel) {
			Visible = false,
		};

		RulerView verticalRuler = new (verticalRulerViewModel) {
			Visible = false,
		};

		Gtk.EventControllerMotion motionController = Gtk.EventControllerMotion.New ();
		motionController.OnMotion += HandleMotion;

		// --- Initialization (Gtk.Widget)

		// The mouse handler in PintaCanvas grabs focus away from toolbar widgets, along
		// with DocumentWorkpace.GrabFocusToCanvas()
		Focusable = true;

		AddController (gestureZoom);
		AddController (dragController);
		AddController (motionController);

		// --- Initialization (Gtk.Grid)

		ColumnHomogeneous = false;
		RowHomogeneous = false;

		Attach (horizontalRuler, 1, 0, 1, 1);
		Attach (verticalRuler, 0, 1, 1, 1);
		Attach (scrolledWindow, 1, 1, 1, 1);

		// --- References to keep

		Canvas = canvas;

		this.chrome = chrome;
		this.tools = tools;
		this.document = document;

		scrolled_window = scrolledWindow;
		gesture_zoom = gestureZoom;
		horizontal_ruler = horizontalRulerViewModel;
		vertical_ruler = verticalRulerViewModel;
		motion_controller = motionController;
		drag_controller = dragController;

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
		Gtk.EventControllerMotion controller,
		Gtk.EventControllerMotion.MotionSignalArgs args)
	{
		PointD rootPoint = new (args.X, args.Y);

		// These coordinates are relative to our grid widget, so transform into the child image
		// view's coordinates, and then to the canvas coordinates.
		this.TranslateCoordinates (Canvas, rootPoint, out PointD viewPos);

		current_canvas_pos = document.Workspace.ViewPointToCanvas (viewPos);
		horizontal_ruler.Position = current_canvas_pos.X;
		vertical_ruler.Position = current_canvas_pos.Y;

		// Forward mouse move events to the current tool when not dragging.
		if (drag_controller.GetStartPoint (out _, out _))
			return;

		if (document.Workspace.PointInCanvas (current_canvas_pos))
			chrome.LastCanvasCursorPoint = current_canvas_pos.ToInt ();

		ToolMouseEventArgs tool_args = new () {
			State = controller.GetCurrentEventState (),
			MouseButton = MouseButton.None,
			PointDouble = current_canvas_pos,
			WindowPoint = viewPos,
			RootPoint = rootPoint,
		};

		tools.DoMouseMove (document, tool_args);
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

	private void OnDragBegin (Gtk.GestureDrag gesture, Gtk.GestureDrag.DragBeginSignalArgs args)
	{
		// A mouse click on the canvas should grab focus away from any toolbar widgets, etc
		// Using the root canvas widget works best - if the drawing area is given focus, the scroll
		// widget jumps back to the origin.
		GrabFocus ();

		// Note: if we ever regain support for docking multiple canvas
		// widgets side by side (like Pinta 1.7 could), a mouse click should switch
		// the active document to this document.

		// Send the mouse press event to the current tool.
		// Translate coordinates to the canvas widget.
		PointD rootPoint = new (args.StartX, args.StartY);
		this.TranslateCoordinates (Canvas, rootPoint, out PointD viewPoint);
		PointD canvasPoint = document.Workspace.ViewPointToCanvas (viewPoint);

		ToolMouseEventArgs tool_args = new () {
			State = gesture.GetCurrentEventState (),
			MouseButton = gesture.GetCurrentMouseButton (),
			PointDouble = canvasPoint,
			WindowPoint = viewPoint,
			RootPoint = rootPoint,
		};

		tools.DoMouseDown (document, tool_args);
	}

	private void OnDragUpdate (Gtk.GestureDrag gesture, Gtk.GestureDrag.DragUpdateSignalArgs args)
	{
		gesture.GetStartPoint (out double startX, out double startY);
		PointD rootPoint = new (startX + args.OffsetX, startY + args.OffsetY);

		// Translate coordinates to the canvas widget.
		this.TranslateCoordinates (Canvas, rootPoint, out PointD viewPoint);

		current_canvas_pos = document.Workspace.ViewPointToCanvas (viewPoint);
		if (document.Workspace.PointInCanvas (current_canvas_pos))
			chrome.LastCanvasCursorPoint = current_canvas_pos.ToInt ();

		// Send the mouse move event to the current tool.
		ToolMouseEventArgs tool_args = new () {
			State = gesture.GetCurrentEventState (),
			MouseButton = gesture.GetCurrentMouseButton (),
			PointDouble = current_canvas_pos,
			WindowPoint = viewPoint,
			RootPoint = rootPoint,
		};

		tools.DoMouseMove (document, tool_args);
	}

	private void OnDragEnd (Gtk.GestureDrag gesture, Gtk.GestureDrag.DragEndSignalArgs args)
	{
		gesture.GetStartPoint (out double startX, out double startY);
		PointD rootPoint = new (startX + args.OffsetX, startY + args.OffsetY);

		// Translate coordinates to the canvas widget.
		this.TranslateCoordinates (Canvas, rootPoint, out PointD viewPoint);
		PointD canvasPoint = document.Workspace.ViewPointToCanvas (viewPoint);

		// Send the mouse release event to the current tool.
		ToolMouseEventArgs tool_args = new () {
			State = gesture.GetCurrentEventState (),
			MouseButton = gesture.GetCurrentMouseButton (),
			PointDouble = canvasPoint,
			WindowPoint = viewPoint,
			RootPoint = rootPoint,
		};

		tools.DoMouseUp (document, tool_args);
	}

	public bool DoKeyPressEvent (
		Gtk.EventControllerKey controller,
		Gtk.EventControllerKey.KeyPressedSignalArgs args)
	{
		// Give the current tool a chance to handle the key press
		ToolKeyEventArgs tool_args = new () {
			Event = controller.GetCurrentEvent (),
			Key = args.GetKey (),
			State = args.State,
		};

		return tools.DoKeyDown (document, tool_args);
	}

	public bool DoKeyReleaseEvent (
		Gtk.EventControllerKey controller,
		Gtk.EventControllerKey.KeyReleasedSignalArgs args)
	{
		ToolKeyEventArgs tool_args = new () {
			Event = controller.GetCurrentEvent (),
			Key = args.GetKey (),
			State = args.State,
		};

		return tools.DoKeyUp (document, tool_args);
	}
}
