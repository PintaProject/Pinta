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
	private readonly WorkspaceManager workspace;
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

	private const double ZoomThresholdScroll = 1.25;
	private const double ZoomThresholdPinch = 0.15;

	public PintaCanvas Canvas { get; }

	public CanvasWindow (
		WorkspaceManager workspace,
		Document document)
	{
		this.workspace = workspace;
		this.document = document;

		ColumnHomogeneous = false;
		RowHomogeneous = false;

		scrolled_window = new Gtk.ScrolledWindow ();

		gesture_zoom = Gtk.GestureZoom.New ();
		gesture_zoom.SetPropagationPhase (Gtk.PropagationPhase.Bubble);
		gesture_zoom.OnScaleChanged += HandleGestureZoomScaleChanged;
		gesture_zoom.OnEnd += (_, _) => cumulative_zoom_amount = last_scale_delta = 0;
		gesture_zoom.OnCancel += (_, _) => cumulative_zoom_amount = last_scale_delta = 0;

		AddController (gesture_zoom);

		Gtk.Viewport vp = new ();

		var scroll_controller = Gtk.EventControllerScroll.New (Gtk.EventControllerScrollFlags.BothAxes); // Both axes must be captured so the zoom gesture can cancel them
		scroll_controller.OnScroll += HandleScrollEvent;
		scroll_controller.OnDecelerate += (_, _) => gesture_zoom.IsActive (); // Cancel scroll deceleration when zooming
		vp.AddController (scroll_controller);

		// The mouse handler in PintaCanvas grabs focus away from toolbar widgets.
		Focusable = true;

		Canvas = new PintaCanvas (this, document) { Name = "canvas" };

		// Rulers
		horizontal_ruler = new Ruler (Gtk.Orientation.Horizontal) { Metric = MetricType.Pixels };

		Attach (horizontal_ruler, 1, 0, 1, 1);

		vertical_ruler = new Ruler (Gtk.Orientation.Vertical) { Metric = MetricType.Pixels };

		Attach (vertical_ruler, 0, 1, 1, 1);

		scrolled_window.Hexpand = true;
		scrolled_window.Vexpand = true;
		Attach (scrolled_window, 1, 1, 1, 1);

		scrolled_window.Child = vp;
		vp.Child = Canvas;

		horizontal_ruler.Visible = false;
		vertical_ruler.Visible = false;

		scrolled_window.Hadjustment!.OnValueChanged += UpdateRulerRange;
		scrolled_window.Vadjustment!.OnValueChanged += UpdateRulerRange;
		document.Workspace.ViewSizeChanged += UpdateRulerRange;
		Canvas.OnResize += UpdateRulerRange;

		motion_controller = Gtk.EventControllerMotion.New ();
		motion_controller.OnMotion += (_, args) => {
			if (!workspace.HasOpenDocuments)
				return;

			current_window_pos = new PointD (args.X, args.Y);
			// These coordinates are relative to our grid widget, so transform into the child image
			// view's coordinates, and then to the canvas coordinates.
			this.TranslateCoordinates (Canvas, current_window_pos, out PointD view_pos);
			current_canvas_pos = workspace.ViewPointToCanvas (view_pos);

			horizontal_ruler.Position = current_canvas_pos.X;
			vertical_ruler.Position = current_canvas_pos.Y;
		};

		AddController (motion_controller);
	}

	private void HandleGestureZoomScaleChanged (object? sender, EventArgs e)
	{
		// Allow the user to zoom in/out by pinching the trackpad
		double pinchDelta = gesture_zoom.GetScaleDelta () - 1 - last_scale_delta;
		if (pinchDelta < 0) {
			if (cumulative_zoom_amount > 0)
				cumulative_zoom_amount = 0; // Reset the counter if the user changes direction so that changing direction doesn't take extra movement

			cumulative_zoom_amount += pinchDelta;
			if (cumulative_zoom_amount <= -ZoomThresholdPinch) {
				document.Workspace.ZoomOutAroundCanvasPoint (current_canvas_pos);
				cumulative_zoom_amount = 0;
			}
		} else {
			if (cumulative_zoom_amount < 0)
				cumulative_zoom_amount = 0;

			cumulative_zoom_amount += pinchDelta;
			if (cumulative_zoom_amount >= ZoomThresholdPinch) {
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

		if (workspace.HasOpenDocuments) {

			if (workspace.Offset.X > 0) {
				lower = lower with { X = -workspace.Offset.X / workspace.Scale };
				upper = upper with { X = workspace.ImageSize.Width - lower.X };
			} else {
				lower = lower with { X = scrolled_window.Hadjustment.Value / workspace.Scale };
				upper = upper with { X = (scrolled_window.Hadjustment.Value + scrolled_window.Hadjustment.PageSize) / workspace.Scale };
			}

			if (workspace.Offset.Y > 0) {
				lower = lower with { Y = -workspace.Offset.Y / workspace.Scale };
				upper = upper with { Y = workspace.ImageSize.Height - lower.Y };
			} else {
				lower = lower with { Y = scrolled_window.Vadjustment.Value / workspace.Scale };
				upper = upper with { Y = (scrolled_window.Vadjustment.Value + scrolled_window.Vadjustment.PageSize) / workspace.Scale };
			}
		}

		horizontal_ruler.SetRange (lower.X, upper.X);
		vertical_ruler.SetRange (lower.Y, upper.Y);
	}

	private bool HandleScrollEvent (Gtk.EventControllerScroll controller, Gtk.EventControllerScroll.ScrollSignalArgs args)
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
			if (cumulative_zoom_amount <= -ZoomThresholdScroll) {
				document.Workspace.ZoomInAroundCanvasPoint (current_canvas_pos);
				cumulative_zoom_amount = 0;
			}

		} else {
			if (cumulative_zoom_amount < 0)
				cumulative_zoom_amount = 0;

			cumulative_zoom_amount += args.Dy;
			if (cumulative_zoom_amount >= ZoomThresholdScroll) {
				document.Workspace.ZoomOutAroundCanvasPoint (current_canvas_pos);
				cumulative_zoom_amount = 0;
			}

		}

		return true;
	}
}
