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

public sealed class PintaCanvas : Gtk.DrawingArea
{
	private readonly CanvasRenderer cr;
	private readonly Document document;

	private Cairo.ImageSurface? canvas;

	public CanvasWindow CanvasWindow { get; }

	private readonly ToolManager tools;

	public PintaCanvas (
		ActionManager actions,
		ChromeManager chrome,
		ToolManager tools,
		WorkspaceManager workspace,
		CanvasWindow window,
		Document document,
		ICanvasGridService canvasGrid)
	{
		this.tools = tools;

		CanvasWindow = window;
		this.document = document;

		cr = new CanvasRenderer (canvasGrid, true);

		// Keep the widget the same size as the canvas
		document.Workspace.ViewSizeChanged += (_, _) => SetRequisition (document.Workspace.ViewSize);

		// Update the canvas when the image changes
		document.Workspace.CanvasInvalidated += (_, _) => {
			// If GTK+ hasn't created the canvas window yet, no need to invalidate it
			if (!GetRealized ())
				return;

			// TODO-GTK4 (improvement) - is there a way to invalidate only a rectangle?
#if false
			if (e.EntireSurface)
				Window.Invalidate ();
			else
				Window.InvalidateRect (e.Rectangle, false);
#else
			QueueDraw ();
#endif
		};

		// Give mouse press / release events to the current tool
		var click_controller = Gtk.GestureClick.New ();
		click_controller.SetButton (0); // Listen for all mouse buttons.

		click_controller.OnPressed += (_, args) => {
			// Note we don't call click_controller.SetState (Gtk.EventSequenceState.Claimed) here, so
			// that the CanvasWindow can also receive motion events to update the root window mouse position.

			// A mouse click on the canvas should grab focus away from any toolbar widgets, etc
			// Using the root canvas widget works best - if the drawing area is given focus, the scroll
			// widget jumps back to the origin.
			CanvasWindow.GrabFocus ();

			// The canvas gets the button press before the tab system, so
			// if this click is on a canvas that isn't currently the ActiveDocument yet, 
			// we need to go ahead and make it the active document for the tools
			// to use it, even though right after this the tab system would have switched it
			if (workspace.ActiveDocument != document)
				actions.Window.SetActiveDocument (document);

			PointD window_point = new (args.X, args.Y);
			PointD canvas_point = document.Workspace.ViewPointToCanvas (window_point);

			ToolMouseEventArgs tool_args = new () {
				State = click_controller.GetCurrentEventState (),
				MouseButton = click_controller.GetCurrentMouseButton (),
				PointDouble = canvas_point,
				WindowPoint = window_point,
				RootPoint = CanvasWindow.WindowMousePosition,
			};

			tools.DoMouseDown (document, tool_args);
		};

		click_controller.OnReleased += (_, args) => {

			PointD window_point = new (args.X, args.Y);
			PointD canvas_point = document.Workspace.ViewPointToCanvas (window_point);

			ToolMouseEventArgs tool_args = new () {
				State = click_controller.GetCurrentEventState (),
				MouseButton = click_controller.GetCurrentMouseButton (),
				PointDouble = canvas_point,
				WindowPoint = window_point,
				RootPoint = CanvasWindow.WindowMousePosition,
			};

			tools.DoMouseUp (document, tool_args);
		};

		AddController (click_controller);

		// Give mouse move events to the current tool
		var motion_controller = Gtk.EventControllerMotion.New ();
		motion_controller.OnMotion += (_, args) => {

			PointD window_point = new (args.X, args.Y);
			PointD canvas_point = document.Workspace.ViewPointToCanvas (window_point);

			if (document.Workspace.PointInCanvas (canvas_point))
				chrome.LastCanvasCursorPoint = canvas_point.ToInt ();

			if (tools.CurrentTool == null)
				return;

			ToolMouseEventArgs tool_args = new () {
				State = motion_controller.GetCurrentEventState (),
				MouseButton = MouseButton.None,
				PointDouble = canvas_point,
				WindowPoint = window_point,
				RootPoint = CanvasWindow.WindowMousePosition,
			};

			tools.DoMouseMove (document, tool_args);
		};

		AddController (motion_controller);

		SetDrawFunc ((area, context, width, height) => Draw (context, width, height));
	}

	private void Draw (Cairo.Context context, int width, int height)
	{
		double scale = document.Workspace.Scale;

		int x = (int) document.Workspace.Offset.X;
		int y = (int) document.Workspace.Offset.Y;

		// Translate our expose area for the whole drawingarea to just our canvas
		RectangleI canvas_bounds = new (
			x,
			y,
			document.Workspace.ViewSize.Width,
			document.Workspace.ViewSize.Height);

		if (CairoExtensions.GetClipRectangle (context, out RectangleI expose_rect))
			canvas_bounds = canvas_bounds.Intersect (expose_rect);

		if (canvas_bounds.IsEmpty)
			return;

		canvas_bounds = canvas_bounds with { X = canvas_bounds.X - x, Y = canvas_bounds.Y - y };

		// Resize our offscreen surface to a surface the size of our drawing area
		if (canvas == null || canvas.Width != canvas_bounds.Width || canvas.Height != canvas_bounds.Height) {
			canvas = CairoExtensions.CreateImageSurface (Cairo.Format.Argb32, canvas_bounds.Width, canvas_bounds.Height);
		}

		cr.Initialize (document.ImageSize, document.Workspace.ViewSize);

		Cairo.Context g = context;

		// Draw our canvas drop shadow
		g.DrawRectangle (new RectangleD (x - 1, y - 1, document.Workspace.ViewSize.Width + 2, document.Workspace.ViewSize.Height + 2), new Cairo.Color (.5, .5, .5), 1);
		g.DrawRectangle (new RectangleD (x - 2, y - 2, document.Workspace.ViewSize.Width + 4, document.Workspace.ViewSize.Height + 4), new Cairo.Color (.8, .8, .8), 1);
		g.DrawRectangle (new RectangleD (x - 3, y - 3, document.Workspace.ViewSize.Width + 6, document.Workspace.ViewSize.Height + 6), new Cairo.Color (.9, .9, .9), 1);

		// Set up our clip rectangle
		g.Rectangle (new RectangleD (x, y, document.Workspace.ViewSize.Width, document.Workspace.ViewSize.Height));
		g.Clip ();

		g.Translate (x, y);

		// Render all the layers to a surface
		var layers = document.Layers.GetLayersToPaint ().ToList ();

		if (layers.Count == 0)
			canvas.Clear ();

		cr.Render (layers, canvas, canvas_bounds.Location);

		// Paint the surface to our canvas
		g.SetSourceSurface (canvas, canvas_bounds.X + (int) (0 * scale), canvas_bounds.Y + (int) (0 * scale));
		g.Paint ();

		// Selection outline
		if (document.Selection.Visible) {
			string tool_name = tools.CurrentTool?.GetType ().Name ?? string.Empty;
			bool fillSelection = tool_name.Contains ("Select") && !tool_name.Contains ("Selected");
			document.Selection.Draw (g, scale, fillSelection);
		}

		if (tools.CurrentTool is not null) {

			g.Save ();

			g.ResetClip (); // Don't clip the control at the edge of the image.
			g.Translate (-x, -y);

			DrawHandles (g, tools.CurrentTool.Handles);

			g.Restore ();
		}

		// Explicitly dispose the context to avoid memory growth (bug #939).
		// This can be the last reference to a temporary surface from the GTK widget.
		context.Dispose ();
	}

	private static void DrawHandles (Cairo.Context cr, IEnumerable<IToolHandle> controls)
	{
		foreach (var control in controls.Where (c => c.Active))
			control.Draw (cr);
	}

	private void SetRequisition (Size size)
	{
		WidthRequest = size.Width;
		HeightRequest = size.Height;

		QueueResize ();
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
