//
// PintaCanvas.cs
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

using System.Collections.Generic;
using System.Linq;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class PintaCanvas : Gtk.Picture
{
	private readonly CanvasRenderer cr;
	private readonly Document document;
	private readonly CanvasWindow canvas_window;

	private Cairo.ImageSurface? flattened_surface;

	private readonly ChromeManager chrome;
	private readonly ToolManager tools;

	public PintaCanvas (
		ChromeManager chrome,
		ToolManager tools,
		CanvasWindow window,
		Document document,
		ICanvasGridService canvasGrid)
	{
		this.chrome = chrome;
		this.tools = tools;
		canvas_window = window;
		this.document = document;

		cr = new (canvasGrid, enableLivePreview: true);

		document.Workspace.ViewSizeChanged += OnViewSizeChanged;
		document.Workspace.CanvasInvalidated += OnCanvasInvalidated;

		// Forward mouse press / release events to the current tool
		Gtk.GestureClick click_controller = Gtk.GestureClick.New ();
		click_controller.SetButton (0); // Listen for all mouse buttons.
		click_controller.OnPressed += OnMouseDown;
		click_controller.OnReleased += OnMouseUp;
		AddController (click_controller);

		// Forward mouse move events to the current tool
		Gtk.EventControllerMotion motion_controller = Gtk.EventControllerMotion.New ();
		motion_controller.OnMotion += OnMouseMove;
		AddController (motion_controller);

		// If there is additional space available, keep the image centered and prevent stretching.
		Hexpand = false;
		Halign = Gtk.Align.Center;
		Vexpand = false;
		Valign = Gtk.Align.Center;
	}

	/// <summary>
	/// Update the canvas when the image changes.
	/// </summary>
	private void OnCanvasInvalidated (object? o, CanvasInvalidatedEventArgs e)
	{
		// Compute the flattened image.
		if (flattened_surface is null ||
		    flattened_surface.Width != document.ImageSize.Width ||
		    flattened_surface.Height != document.ImageSize.Height) {

			flattened_surface?.Dispose ();
			flattened_surface = CairoExtensions.CreateImageSurface (Cairo.Format.Argb32, document.ImageSize.Width, document.ImageSize.Height);
		}

		RenderCanvas (flattened_surface);

		// FIXME - Gdk.MemoryTextureBuilder is only available in GTK 4.16+, and Gsk.Path etc require 4.14
		// TODO - if we used cairo_image_surface_create_for_data() to wrap the GLib.Bytes buffer, we might be able to avoid this extra copy
		// TODO - is there any benefit to caching the texture builder?
		// TODO - investigate using gdk_memory_texture_builder_set_update_region() for partial canvas updates based on the invalidated area
		// TODO - could use gtk_snapshot_push_blend() to avoid flattening each layer on the CPU?
		GLib.Bytes bytes = GLib.Bytes.New (flattened_surface.GetData ());
		Gdk.MemoryTextureBuilder builder = new () {
			Bytes = bytes,
			Width = flattened_surface.Width,
			Height = flattened_surface.Height,
			Format = Gdk.MemoryFormat.B8g8r8a8Premultiplied

		};
		// Workaround for https://github.com/gircore/gir.core/issues/1257 - the Stride property produces an error
		builder.SetStride ((nuint) flattened_surface.Stride);

		Gdk.Texture texture = builder.Build ();

		// Scale to fit the view size (when zooming in or out).
		Graphene.Rect canvasBounds = Graphene.Rect.Alloc ();
		Size viewSize = document.Workspace.ViewSize;
		canvasBounds.Init (0.0f, 0.0f, (float) viewSize.Width, (float) viewSize.Height);

		Gtk.Snapshot snapshot = Gtk.Snapshot.New ();
		Gsk.ScalingFilter scalingFilter = (document.Workspace.Scale >= 1.0) ?
			Gsk.ScalingFilter.Nearest :
			Gsk.ScalingFilter.Linear;
		snapshot.AppendScaledTexture (texture, scalingFilter, canvasBounds);

		DrawSelection (snapshot);

		// In the future, this would be cleaner to implement as a custom widget once gir.core supports virtual methods
		// (in particular, zooming might be easier when we have control over the size allocation)
		// For now, we just use a Gtk.Picture widget with a custom Gdk.Paintable for its contents.
		Gdk.Paintable? paintable = snapshot.ToPaintable (size: null);
		if (paintable is not null)
			Paintable = paintable;
		else
			System.Console.WriteLine ("Failed to render snapshot for canvas");

		QueueDraw ();
	}

	private void RenderCanvas (Cairo.ImageSurface flattenedSurface)
	{
		// Note we are always rendering without scaling, since the scaling is applied when drawing the texture later.
		cr.Initialize (document.ImageSize, document.ImageSize);

		// TODO - sort out how to render the pixel grid, drop shadow (CSS?) and screen space handles

		List<Layer> layers = document.Layers.GetLayersToPaint ().ToList ();

		if (layers.Count == 0)
			flattenedSurface.Clear ();

		cr.Render (layers, flattenedSurface, offset: PointI.Zero);
	}

	private void DrawSelection (Gtk.Snapshot snapshot)
	{
		if (!document.Selection.Visible)
			return;

		bool fillSelection = tools.CurrentTool?.IsSelectionTool ?? false;

		// Convert the selection path.
		Gsk.PathBuilder pathBuilder = Gsk.PathBuilder.New ();
		pathBuilder.AddCairoPath (document.Selection.SelectionPath);
		Gsk.Path selectionPath = pathBuilder.ToPath ();

		snapshot.Save ();

		// Scale the selection path up to the view size.
		// Note the outline width (below) remains at a constant size.
		float scale = (float) document.Workspace.Scale;
		snapshot.Scale (scale, scale);

		if (fillSelection) {
			Gdk.RGBA fillColor = new () { Red = 0.7f, Green = 0.8f, Blue = 0.9f, Alpha = 0.2f };
			snapshot.AppendFill (selectionPath, Gsk.FillRule.EvenOdd, fillColor);
		}

		// Draw a white line first so it shows up on dark backgrounds
		Gsk.Stroke stroke = Gsk.Stroke.New (lineWidth: 1.0f / scale);
		Gdk.RGBA white = new () { Red = 1, Green = 1, Blue = 1, Alpha = 1 };
		snapshot.AppendStroke (selectionPath, stroke, white);

		// Draw a black dashed line over the white line
		stroke.SetDash ([2.0f / scale, 4.0f / scale]);
		Gdk.RGBA black = new () { Red = 0, Green = 0, Blue = 0, Alpha = 1 };
		snapshot.AppendStroke (selectionPath, stroke, black);

		snapshot.Restore ();
	}

	/// <summary>
	/// Update the widget's size when the image size has changed, or e.g. zooming in.
	/// </summary>
	private void OnViewSizeChanged (object? o, System.EventArgs args)
	{
		Size viewSize = document.Workspace.ViewSize;
		SetSizeRequest (viewSize.Width, viewSize.Height);
	}

	private void OnMouseDown (Gtk.GestureClick gesture, Gtk.GestureClick.PressedSignalArgs args)
	{
		// Note we don't call gesture.SetState (Gtk.EventSequenceState.Claimed) here, so
		// that the CanvasWindow can also receive motion events to update the root window mouse position.

		// A mouse click on the canvas should grab focus away from any toolbar widgets, etc
		// Using the root canvas widget works best - if the drawing area is given focus, the scroll
		// widget jumps back to the origin.
		canvas_window.GrabFocus ();

		// Note: if we ever regain support for docking multiple canvas
		// widgets side by side (like Pinta 1.7 could), a mouse click should switch
		// the active document to this document.

		// Send the mouse press event to the current tool.
		PointD window_point = new (args.X, args.Y);
		PointD canvas_point = document.Workspace.ViewPointToCanvas (window_point);

		ToolMouseEventArgs tool_args = new () {
			State = gesture.GetCurrentEventState (),
			MouseButton = gesture.GetCurrentMouseButton (),
			PointDouble = canvas_point,
			WindowPoint = window_point,
			RootPoint = canvas_window.WindowMousePosition,
		};

		tools.DoMouseDown (document, tool_args);
	}

	private void OnMouseUp (Gtk.GestureClick gesture, Gtk.GestureClick.ReleasedSignalArgs args)
	{
		// Send the mouse release event to the current tool.
		PointD window_point = new (args.X, args.Y);
		PointD canvas_point = document.Workspace.ViewPointToCanvas (window_point);

		ToolMouseEventArgs tool_args = new () {
			State = gesture.GetCurrentEventState (),
			MouseButton = gesture.GetCurrentMouseButton (),
			PointDouble = canvas_point,
			WindowPoint = window_point,
			RootPoint = canvas_window.WindowMousePosition,
		};

		tools.DoMouseUp (document, tool_args);
	}

	private void OnMouseMove (Gtk.EventControllerMotion controller, Gtk.EventControllerMotion.MotionSignalArgs args)
	{
		PointD window_point = new (args.X, args.Y);
		PointD canvas_point = document.Workspace.ViewPointToCanvas (window_point);

		if (document.Workspace.PointInCanvas (canvas_point))
			chrome.LastCanvasCursorPoint = canvas_point.ToInt ();

		ToolMouseEventArgs tool_args = new () {
			State = controller.GetCurrentEventState (),
			MouseButton = MouseButton.None,
			PointDouble = canvas_point,
			WindowPoint = window_point,
			RootPoint = canvas_window.WindowMousePosition,
		};

		tools.DoMouseMove (document, tool_args);
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
