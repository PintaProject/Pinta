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

using System;
using System.Collections.Generic;
using System.Linq;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class PintaCanvas : Gtk.Picture
{
	private readonly CanvasRenderer cr;
	private readonly Document document;
	private readonly CanvasWindow canvas_window;
	private readonly ICanvasGridService canvas_grid;

	private Cairo.ImageSurface? canvas_surface;
	private Gdk.Texture? canvas_texture;
	private static readonly Gdk.Texture transparent_pattern_texture = CreateTransparentPatternTexture ();
	private RectangleI? modified_area;

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
		canvas_grid = canvasGrid;
		this.document = document;

		cr = new (enableLivePreview: true, enableBackgroundPattern: false);

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
	/// Queue an update to the canvas.
	/// There can be multiple consecutive Invalidate() calls before a UI update, e.g.
	/// in the text tool, or undoing multiple history items.
	/// </summary>
	private void OnCanvasInvalidated (object? o, CanvasInvalidatedEventArgs e)
	{
		RectangleI rect = e.EntireSurface
			? new RectangleI (PointI.Zero, document.ImageSize)
			: e.Rectangle;

		// If an update is already queued, just extend the region to update.
		if (modified_area.HasValue) {
			modified_area = modified_area.Value.Union (rect);
			return;
		}

		modified_area = rect;
		GLib.Functions.IdleAdd (GLib.Constants.PRIORITY_DEFAULT, () => {
			UpdateCanvas ();
			return false;
		});
	}

	/// <summary>
	/// Update the canvas after changes to the image.
	/// </summary>
	private void UpdateCanvas ()
	{
		if (!modified_area.HasValue)
			throw new InvalidOperationException ("No canvas region was modified");

		Graphene.Rect canvasViewBounds = Graphene.Rect.Alloc ();
		Size viewSize = document.Workspace.ViewSize;
		canvasViewBounds.Init (0.0f, 0.0f, (float) viewSize.Width, (float) viewSize.Height);

		Gtk.Snapshot snapshot = Gtk.Snapshot.New ();

		DrawTransparentBackground (snapshot, canvasViewBounds);
		DrawCanvas (snapshot, modified_area.Value, canvasViewBounds);
		DrawSelection (snapshot, canvasViewBounds);
		DrawHandles (snapshot, canvasViewBounds);
		DrawCanvasGrid (snapshot, canvasViewBounds);
		DrawCanvasAxonometricGrid (snapshot, canvasViewBounds);

		// In the future, this would be cleaner to implement as a custom widget once gir.core supports virtual methods
		// (in particular, zooming might be easier when we have control over the size allocation)
		// For now, we just use a Gtk.Picture widget with a custom Gdk.Paintable for its contents.
		Gdk.Paintable? paintable = snapshot.ToPaintable (size: null);
		if (paintable is not null)
			Paintable = paintable;
		else
			System.Console.WriteLine ("Failed to render snapshot for canvas");

		modified_area = null;
		QueueDraw ();
	}

	private static Gdk.Texture CreateTextureFromSurface (
		Cairo.ImageSurface surface,
		Gdk.Texture? updateTexture = null,
		Cairo.Region? updateRegion = null)
	{
		// TODO - can we avoid copying the full image into GLib.Bytes on each update?
		GLib.Bytes bytes = GLib.Bytes.New (surface.GetData ());
		Gdk.MemoryTextureBuilder builder = new () {
			Bytes = bytes,
			Stride = (ulong) surface.Stride,
			Width = surface.Width,
			Height = surface.Height,
			Format = Gdk.MemoryFormat.B8g8r8a8Premultiplied,
			UpdateTexture = updateTexture,
			UpdateRegion = updateRegion ?? CairoExtensions.CreateRegion (RectangleI.Zero)
		};

		return builder.Build ();
	}

	private static Gdk.Texture CreateTransparentPatternTexture () =>
		CreateTextureFromSurface (CairoExtensions.CreateTransparentBackgroundSurface (size: 16));

	/// <summary>
	/// Draw the transparent checkboard background by tiling a small pattern.
	/// </summary>
	private void DrawTransparentBackground (Gtk.Snapshot snapshot, Graphene.Rect canvasViewBounds)
	{
		snapshot.PushRepeat (canvasViewBounds, childBounds: null);

		Graphene.Rect patternBounds = Graphene.Rect.Alloc ();
		patternBounds.Init (0, 0, transparent_pattern_texture.Width, transparent_pattern_texture.Height);
		snapshot.AppendTexture (transparent_pattern_texture, patternBounds);

		snapshot.Pop ();
	}

	private void DrawCanvas (Gtk.Snapshot snapshot, RectangleI modifiedArea, Graphene.Rect canvasViewBounds)
	{
		// Compute the flattened image for the modified region.
		if (canvas_surface is null ||
		    canvas_surface.Width != document.ImageSize.Width ||
		    canvas_surface.Height != document.ImageSize.Height) {

			canvas_surface?.Dispose ();
			canvas_surface = CairoExtensions.CreateImageSurface (Cairo.Format.Argb32, document.ImageSize.Width, document.ImageSize.Height);

			canvas_texture?.Dispose ();
			canvas_texture = null;
		}

		// Note we are always rendering without scaling, since the scaling is applied when drawing the texture later.
		// TODO - in the future we could experiment with creating a separate texture per layer and using gtk_snapshot_push_blend() to blend on the GPU
		cr.Initialize (document.ImageSize, document.ImageSize);

		List<Layer> layers = document.Layers.GetLayersToPaint ().ToList ();
		cr.Render (layers, canvas_surface, offset: PointI.Zero, clipRect: modifiedArea);

		Gdk.Texture? updateTexture = canvas_texture;
		Cairo.Region? updateRegion = (updateTexture is not null)
			? CairoExtensions.CreateRegion (modifiedArea)
			: null;
		canvas_texture = CreateTextureFromSurface (canvas_surface, updateTexture, updateRegion);

		// Scale to fit the view size (when zooming in or out).
		Gsk.ScalingFilter scalingFilter = (document.Workspace.Scale >= 1.0) ?
			Gsk.ScalingFilter.Nearest :
			Gsk.ScalingFilter.Linear;
		snapshot.AppendScaledTexture (canvas_texture, scalingFilter, canvasViewBounds);
	}

	private void DrawSelection (Gtk.Snapshot snapshot, Graphene.Rect canvasViewBounds)
	{
		if (!document.Selection.Visible)
			return;

		bool fillSelection = tools.CurrentTool?.IsSelectionTool ?? false;

		// Convert the selection path.
		Gsk.PathBuilder pathBuilder = Gsk.PathBuilder.New ();
		pathBuilder.AddCairoPath (document.Selection.SelectionPath);
		Gsk.Path selectionPath = pathBuilder.ToPath ();

		snapshot.Save ();
		snapshot.PushClip (canvasViewBounds);

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

		snapshot.Pop ();
		snapshot.Restore ();
	}

	private void DrawHandles (Gtk.Snapshot snapshot, Graphene.Rect canvasViewBounds)
	{
		BaseTool? tool = tools.CurrentTool;
		if (tool is null)
			return;

		snapshot.PushClip (canvasViewBounds);

		foreach (IToolHandle control in tool.Handles.Where (c => c.Active)) {
			control.Draw (snapshot);
		}

		snapshot.Pop ();
	}

	private void DrawCanvasGrid (Gtk.Snapshot snapshot, Graphene.Rect canvasViewBounds)
	{
		if (!ShouldShowCanvasGrid ())
			return;

		Gsk.Path gridPath = BuildCanvasGridPath ();

		snapshot.PushClip (canvasViewBounds);
		snapshot.Save ();

		// Scale the selection path up to the view size.
		float scale = (float) document.Workspace.Scale;
		snapshot.Scale (scale, scale);

		// Draw as a dotted line (every other pixel) to have a more subtle appearance.
		Gsk.Stroke stroke = Gsk.Stroke.New (lineWidth: 1.0f / scale);
		stroke.SetDash ([1.0f / scale, 1.0f / scale]);

		Gdk.RGBA color = new () { Red = 0, Green = 0, Blue = 0, Alpha = 1 };
		snapshot.AppendStroke (gridPath, stroke, color);

		snapshot.Restore ();
		snapshot.Pop ();
	}

	private Gsk.Path BuildCanvasGridPath ()
	{
		int cellHeight = canvas_grid.CellHeight;
		int cellWidth = canvas_grid.CellWidth;
		int imageHeight = document.ImageSize.Height;
		int imageWidth = document.ImageSize.Width;

		Gsk.PathBuilder pathBuilder = Gsk.PathBuilder.New ();
		// Add horizontal lines.
		for (int y = 0; y < imageHeight; y += cellHeight) {
			pathBuilder.MoveTo (0, y);
			pathBuilder.LineTo (imageWidth, y);
		}

		// Add vertical lines.
		for (int x = 0; x < imageWidth; x += cellWidth) {
			pathBuilder.MoveTo (x, 0);
			pathBuilder.LineTo (x, imageHeight);
		}

		return pathBuilder.ToPath ();
	}

	/// <summary>
	/// The grid should be drawn if it is enabled and we're zoomed in far enough.
	/// </summary>
	private bool ShouldShowCanvasGrid ()
	{
		if (!canvas_grid.ShowGrid)
			return false;

		const int MIN_GRID_LINE_DISTANCE = 5;

		int cellHeight = canvas_grid.CellHeight;
		int cellWidth = canvas_grid.CellWidth;

		int minCanvasDistance = Math.Min (cellHeight, cellWidth);
		int minViewDistance = (int) Math.Ceiling (minCanvasDistance * document.Workspace.Scale);

		return minViewDistance >= MIN_GRID_LINE_DISTANCE;
	}

	private void DrawCanvasAxonometricGrid (Gtk.Snapshot snapshot, Graphene.Rect canvasViewBounds)
	{
		if (!ShouldShowCanvasAxonometricGrid ())
			return;

		Gsk.Path gridPath = BuildCanvasAxonometricGridPath ();

		snapshot.PushClip (canvasViewBounds);
		snapshot.Save ();

		// Scale the selection path up to the view size.
		float scale = (float) document.Workspace.Scale;
		snapshot.Scale (scale, scale);

		// Draw as a dotted line (every other pixel) to have a more subtle appearance.
		Gsk.Stroke stroke = Gsk.Stroke.New (lineWidth: 1.0f / scale);
		stroke.SetDash ([1.0f / scale, 1.0f / scale]);

		Gdk.RGBA color = new () { Red = 0, Green = 0, Blue = 0, Alpha = 1 };
		snapshot.AppendStroke (gridPath, stroke, color);

		snapshot.Restore ();
		snapshot.Pop ();
	}

	private Gsk.Path BuildCanvasAxonometricGridPath ()
	{
		int axonometricWidth = canvas_grid.AxonometricWidth;
		int imageHeight = document.ImageSize.Height;
		int imageWidth = document.ImageSize.Width;

		Gsk.PathBuilder pathBuilder = Gsk.PathBuilder.New ();

		// Adds ascending diagonal lines.
		// '2 * axonometricWidth' ensures the vertical lines will align correctly.
		for (int i = 0; i < imageWidth + imageHeight; i += 2 * axonometricWidth) {
			pathBuilder.MoveTo (0, i);
			pathBuilder.LineTo (i, 0);
		}

		// Adds descending diagonal lines.
		// The 'imageHeight % (2 * axonometricWidth)' adjustment is used to ensure our lines will align
		// to the top [0, 0] instead of the bottom [0, imageHeight]
		for (int i = -imageHeight + imageHeight % (2 * axonometricWidth); i < imageWidth; i += 2 * axonometricWidth) {
			pathBuilder.MoveTo (i, 0);
			pathBuilder.LineTo (i + imageHeight, imageHeight);
		}

		// Add vertical lines.
		for (int i = 0; i < imageWidth; i += axonometricWidth) {
			pathBuilder.MoveTo (i, 0);
			pathBuilder.LineTo (i, imageHeight);
		}

		return pathBuilder.ToPath ();
	}

	/// <summary>
	/// The axonometric grid should be drawn if it is enabled and we're zoomed in far enough.
	/// </summary>
	private bool ShouldShowCanvasAxonometricGrid ()
	{
		if (!canvas_grid.ShowAxonometricGrid)
			return false;

		const int MIN_GRID_LINE_DISTANCE = 5;

		int minCanvasDistance = canvas_grid.AxonometricWidth;
		int minViewDistance = (int) Math.Ceiling (minCanvasDistance * document.Workspace.Scale);

		return minViewDistance >= MIN_GRID_LINE_DISTANCE;
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
