// 
// SelectTool.cs
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
using Cairo;
using Gtk;
using Pinta.Core;
using System.Linq;
using Gdk;

namespace Pinta.Tools
{
	public abstract class SelectTool : BaseTool
	{
		private readonly IToolService tools;
		private readonly IWorkspaceService workspace;

		private bool is_drawing = false;
		private PointD shape_origin;
		private PointD reset_origin;
		private PointD shape_end;
		private Gdk.Rectangle last_dirty;
		private ToolControl[] controls = new ToolControl[8];
		private int? active_control;
		private SelectionHistoryItem? hist;
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.S; } }
		protected override bool ShowAntialiasingButton { get { return false; } }
		private CursorType? active_cursor;
		private CombineMode combine_mode;

		public SelectTool (IServiceManager services) : base (services)
		{
			tools = services.GetService<IToolService> ();
			workspace = services.GetService<IWorkspaceService> ();

			CreateHandler ();

			workspace.SelectionChanged += AfterSelectionChange;
		}

		protected abstract void DrawShape (Document document, Cairo.Rectangle r, Layer l);

		protected override void OnBuildToolBar (Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			workspace.SelectionHandler.BuildToolbar (tb, Settings);
		}

		protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
		{
			// Ignore extra button clicks while drawing
			if (is_drawing)
				return;

			hist = new SelectionHistoryItem (Icon, Name);
			hist.TakeSnapshot ();

			reset_origin = e.WindowPoint;
			active_control = HandleResize (e.PointDouble);

			if (!active_control.HasValue) {
				combine_mode = PintaCore.Workspace.SelectionHandler.DetermineCombineMode (e);

				var x = Utility.Clamp (e.PointDouble.X, 0, document.ImageSize.Width - 1);
				var y = Utility.Clamp (e.PointDouble.Y, 0, document.ImageSize.Height - 1);
				shape_origin = new PointD (x, y);

				document.PreviousSelection.Dispose ();
				document.PreviousSelection = document.Selection.Clone ();
				document.Selection.SelectionPolygons.Clear ();

				// The bottom right corner should be selected.
				active_control = 3;
			}

			// Do a full redraw for modes that can wipe existing selections outside the rectangle being drawn.
			if (combine_mode == CombineMode.Replace || combine_mode == CombineMode.Intersect) {
				var size = document.ImageSize;
				last_dirty = new Gdk.Rectangle (0, 0, size.Width, size.Height);
			}

			is_drawing = true;
		}

		protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
		{
			if (!is_drawing) {
				UpdateCursor (document, e.PointDouble);
				return;
			}

			var x = Utility.Clamp (e.PointDouble.X, 0, document.ImageSize.Width - 1);
			var y = Utility.Clamp (e.PointDouble.Y, 0, document.ImageSize.Height - 1);

			// Should always be true, set in OnMouseDown
			if (active_control.HasValue)
				controls[active_control.Value].HandleMouseMove (x, y, e.State);

			ClearHandles (document.Layers.ToolLayer);
			RefreshHandler ();

			var dirty = ReDraw (document, e.IsShiftPressed);

			if (document.Selection != null) {
				SelectionModeHandler.PerformSelectionMode (combine_mode, document.Selection.SelectionPolygons);
				document.Workspace.Invalidate (dirty.Union (last_dirty));
			}

			last_dirty = dirty;
		}

		protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
		{
			// If the user didn't move the mouse, they want to deselect
			var tolerance = 0;

			if (Math.Abs (reset_origin.X - e.WindowPoint.X) <= tolerance && Math.Abs (reset_origin.Y - e.WindowPoint.Y) <= tolerance) {
				// Mark as being done interactive drawing before invoking the deselect action.
				// This will allow AfterSelectionChanged() to clear the selection.
				is_drawing = false;

				if (hist != null) {
					// Roll back any changes made to the selection, e.g. in OnMouseDown().
					hist.Undo ();

					hist.Dispose ();
					hist = null;
				}

				PintaCore.Actions.Edit.Deselect.Activate ();

			} else {
				ClearHandles (document.Layers.ToolLayer);

				var dirty = ReDraw (document, e.IsShiftPressed);

				if (document.Selection != null) {
					SelectionModeHandler.PerformSelectionMode (combine_mode, document.Selection.SelectionPolygons);

					document.Selection.Origin = shape_origin;
					document.Selection.End = shape_end;
					document.Workspace.Invalidate (last_dirty.Union (dirty));
					last_dirty = dirty;
				}
				if (hist != null) {
					document.History.PushNewItem (hist);
					hist = null;
				}
			}

			is_drawing = false;
			active_control = null;

			// Update the mouse cursor.
			UpdateCursor (document, e.PointDouble);
		}

		protected override void OnActivated (Document? document)
		{
			base.OnActivated (document);

			// When entering the tool, update the selection handles from the
			// document's current selection.
			if (document is not null) {
				shape_origin = document.Selection.Origin;
				shape_end = document.Selection.End;
				UpdateHandler (document);
			}
		}

		protected override void OnDeactivated (Document? document, BaseTool? newTool)
		{
			base.OnDeactivated (document, newTool);

			document?.Layers.ToolLayer.Clear ();
		}

		protected override void OnSaveSettings (ISettingsService settings)
		{
			base.OnSaveSettings (settings);

			workspace.SelectionHandler.OnSaveSettings (settings);
		}

		private void RefreshHandler ()
		{
			controls[0].Position = new PointD (shape_origin.X, shape_origin.Y);
			controls[1].Position = new PointD (shape_origin.X, shape_end.Y);
			controls[2].Position = new PointD (shape_end.X, shape_origin.Y);
			controls[3].Position = new PointD (shape_end.X, shape_end.Y);
			controls[4].Position = new PointD (shape_origin.X, (shape_origin.Y + shape_end.Y) / 2);
			controls[5].Position = new PointD ((shape_origin.X + shape_end.X) / 2, shape_origin.Y);
			controls[6].Position = new PointD (shape_end.X, (shape_origin.Y + shape_end.Y) / 2);
			controls[7].Position = new PointD ((shape_origin.X + shape_end.X) / 2, shape_end.Y);
		}

		private Gdk.Rectangle ReDraw (Document document, bool constraint)
		{
			document.Selection.Visible = true;
			document.Layers.ToolLayer.Hidden = false;

			if (constraint) {
				var dx = Math.Abs (shape_end.X - shape_origin.X);
				var dy = Math.Abs (shape_end.Y - shape_origin.Y);

				if (dx <= dy)
					if (shape_end.X >= shape_origin.X)
						shape_end.X = shape_origin.X + dy;
					else
						shape_end.X = shape_origin.X - dy;
				else
					if (shape_end.Y >= shape_origin.Y)
					shape_end.Y = shape_origin.Y + dx;
				else
					shape_end.Y = shape_origin.Y - dx;
			}

			var rect = Utility.PointsToRectangle (shape_origin, shape_end, constraint);

			DrawShape (document, rect, document.Layers.SelectionLayer);
			DrawHandler (document, document.Layers.ToolLayer);

			// Figure out a bounding box for everything that was drawn, and add a bit of padding.
			var dirty = rect.ToGdkRectangle ();

			foreach (var tool_control in controls)
				dirty = dirty.Union (tool_control.GetHandleRect ().ToGdkRectangle ());

			dirty.Inflate (2, 2);
			return dirty;
		}

		protected void CreateHandler ()
		{
			controls[0] = new ToolControl (CursorType.TopLeftCorner, (x, y, s) => {
				shape_origin.X = x;
				shape_origin.Y = y;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					if (shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
						shape_origin.X = shape_end.X - shape_end.Y + shape_origin.Y;
					else
						shape_origin.Y = shape_end.Y - shape_end.X + shape_origin.X;
				}
			});
			controls[1] = new ToolControl (CursorType.BottomLeftCorner, (x, y, s) => {
				shape_origin.X = x;
				shape_end.Y = y;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					if (shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
						shape_origin.X = shape_end.X - shape_end.Y + shape_origin.Y;
					else
						shape_end.Y = shape_origin.Y + shape_end.X - shape_origin.X;
				}
			});
			controls[2] = new ToolControl (CursorType.TopRightCorner, (x, y, s) => {
				shape_end.X = x;
				shape_origin.Y = y;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					if (shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
						shape_end.X = shape_origin.X + shape_end.Y - shape_origin.Y;
					else
						shape_origin.Y = shape_end.Y - shape_end.X + shape_origin.X;
				}
			});
			controls[3] = new ToolControl (CursorType.BottomRightCorner, (x, y, s) => {
				shape_end.X = x;
				shape_end.Y = y;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					if (shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
						shape_end.X = shape_origin.X + shape_end.Y - shape_origin.Y;
					else
						shape_end.Y = shape_origin.Y + shape_end.X - shape_origin.X;
				}
			});
			controls[4] = new ToolControl (CursorType.LeftSide, (x, y, s) => {
				shape_origin.X = x;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					var d = shape_end.X - shape_origin.X;
					shape_origin.Y = (shape_origin.Y + shape_end.Y - d) / 2;
					shape_end.Y = (shape_origin.Y + shape_end.Y + d) / 2;
				}
			});
			controls[5] = new ToolControl (CursorType.TopSide, (x, y, s) => {
				shape_origin.Y = y;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					var d = shape_end.Y - shape_origin.Y;
					shape_origin.X = (shape_origin.X + shape_end.X - d) / 2;
					shape_end.X = (shape_origin.X + shape_end.X + d) / 2;
				}
			});
			controls[6] = new ToolControl (CursorType.RightSide, (x, y, s) => {
				shape_end.X = x;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					var d = shape_end.X - shape_origin.X;
					shape_origin.Y = (shape_origin.Y + shape_end.Y - d) / 2;
					shape_end.Y = (shape_origin.Y + shape_end.Y + d) / 2;
				}
			});
			controls[7] = new ToolControl (CursorType.BottomSide, (x, y, s) => {
				shape_end.Y = y;
				if ((s & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
					var d = shape_end.Y - shape_origin.Y;
					shape_origin.X = (shape_origin.X + shape_end.X - d) / 2;
					shape_end.X = (shape_origin.X + shape_end.X + d) / 2;
				}
			});
		}

		public int? HandleResize (PointD point)
		{
			for (var i = 0; i < controls.Length; ++i) {
				if (controls[i].IsInside (point))
					return i;
			}

			return null;
		}

		public void DrawHandler (Document document, Layer layer)
		{
			if (!document.Selection.Visible)
				return;

			using (var g = new Context (layer.Surface)) {
				foreach (var tool_control in controls)
					tool_control.Render (g);
			}
		}

		public void UpdateCursor (Document document, PointD point)
		{
			if (document.Selection.Visible) {
				foreach (var ct in controls.Where (ct => ct.IsInside (point))) {
					if (active_cursor != ct.Cursor) {
						SetCursor (new Cursor (ct.Cursor));
						active_cursor = ct.Cursor;
					}
					return;
				}
			}

			if (active_cursor.HasValue) {
				SetCursor (DefaultCursor);
				active_cursor = null;
			}
		}

		protected override void OnAfterUndo (Document document)
		{
			base.OnAfterUndo (document);

			if (tools.CurrentTool == this)
				document.Layers.ToolLayer.Hidden = false;

			shape_origin = document.Selection.Origin;
			shape_end = document.Selection.End;
			UpdateHandler (document);
		}

		protected override void OnAfterRedo (Document document)
		{
			base.OnAfterRedo (document);

			if (tools.CurrentTool == this)
				document.Layers.ToolLayer.Hidden = false;

			shape_origin = document.Selection.Origin;
			shape_end = document.Selection.End;
			UpdateHandler (document);
		}

		private void AfterSelectionChange (object? sender, EventArgs event_args)
		{
			if (is_drawing || !PintaCore.Workspace.HasOpenDocuments)
				return;

			// TODO: Try to remove this ActiveDocument call
			var document = workspace.ActiveDocument;
			var selection = document.Selection;

			shape_origin = selection.Origin;
			shape_end = selection.End;

			if (tools.CurrentTool == this)
				UpdateHandler (document);
		}

		/// <summary>
		/// Update the selection handles' positions, and redraw them.
		/// </summary>
		private void UpdateHandler (Document document)
		{
			ClearHandles (document.Layers.ToolLayer);
			RefreshHandler ();
			DrawHandler (document, document.Layers.ToolLayer);
			document.Workspace.Invalidate ();
		}

		/// <summary>
		/// Erase previously-drawn handles.
		/// </summary>
		private void ClearHandles (Layer layer)
		{
			using (var g = new Context (layer.Surface)) {
				foreach (var tool_control in controls)
					tool_control.Clear (g);
			}
		}
	}
}
