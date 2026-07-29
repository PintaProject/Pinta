// 
// BaseTransformTool.cs
//  
// Author:
//       Volodymyr <${AuthorEmail}>
// 
// Copyright (c) 2012 Volodymyr
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
using System.IO;
using Cairo;
using Pinta.Core;

namespace Pinta.Tools;

public abstract class BaseTransformTool : BaseTool
{
	private readonly int rotate_steps = 32;
	private readonly Matrix transform = CairoExtensions.CreateIdentityMatrix ();
	private RectangleD source_rect;
	private PointD original_point;
	private bool is_dragging = false;
	private bool is_rotating = false;
	private bool is_scaling = false;
	private bool using_mouse = false;

	// Drag-handle resizing of the moved/pasted content. Credit: issue #585,
	// marcovr's allow-handle-resize branch, and cameronwhite's draft PR #1515.
	// See docs/selection-transform-handles.md.
	private readonly IWorkspaceService workspace;
	private readonly IToolService tool_service;
	private readonly RectangleHandle handle;
	private readonly Gdk.Cursor rotate_cursor;
	private bool is_handle_scaling = false;
	public override IEnumerable<IToolHandle> Handles => [handle];

	// Live orientation of the moved content (issue #4): maps the axis-aligned
	// reference rect (ref_rect, captured when the selection first attaches) onto
	// the current on-screen quad, so the resize grips stay glued to rotated
	// content. Only ever a rotation + ref-space axis scale + translation, so it
	// always maps ref_rect to a proper (non-sheared) oriented rectangle.
	private readonly Matrix live = CairoExtensions.CreateIdentityMatrix ();
	private RectangleD ref_rect;

	// Nudge hint when holding arrow key for >2s (Issue #1559 extension)
	private DateTime? nudge_start_time;
	private uint nudge_hint_timeout_id = 0;
	private bool nudge_hint_visible = false;
	private string? last_nudge_hint;
	private Gtk.Popover? nudge_popover;
	private Gtk.Label? nudge_label;

	/// <summary>
	/// Initializes a new instance of the <see cref="BaseTransformTool"/> class.
	/// </summary>
	public BaseTransformTool (IServiceProvider services) : base (services)
	{
		workspace = services.GetService<IWorkspaceService> ();
		tool_service = services.GetService<IToolService> ();
		handle = new (workspace) { InvertIfNegative = true };
		// A larger, high-contrast bidirectional rotate cursor, sized to match the
		// resize grips' cursors. Resource SVGs load at their natural size (not the
		// requested one), so center the hotspot on the actual texture — a fixed
		// hotspot past the edge trips a GDK assertion.
		// Use a non-symbolic icon (rotate-handle.svg) so GTK preserves the white
		// halo + dark stroke. Symbolic icons get recolored to a single color and
		// lose contrast, making the cursor invisible against some backgrounds.
		Gdk.Texture rotate_texture = LoadRotateTexture ();
		rotate_cursor = Gdk.Cursor.NewFromTexture (rotate_texture, rotate_texture.Width / 2, rotate_texture.Height / 2, null);

		// The pasted/moved selection is often created *after* this tool is
		// activated (see PasteAction), so OnActivated is too early to show the
		// grips. Refresh them whenever the selection changes.
		workspace.SelectionChanged += (_, _) => {
			if (IsActive || tool_service.CurrentTool != this || !workspace.HasOpenDocuments)
				return;
			UpdateHandlesFromDocument (workspace.ActiveDocument);
		};
	}

	protected override void OnMouseDown (
		Document document,
		ToolMouseEventArgs e)
	{
		if (IsActive)
			return;

		original_point = e.PointDouble;

		// Alt (or right button) rotates, and takes priority over grabbing a grip.
		bool rotate_requested = e.MouseButton == MouseButton.Right || e.IsAltPressed;

		// If the mouse is on a resize grip, scale by dragging the handle.
		if (!rotate_requested && handle.Active && handle.BeginDrag (e.PointDouble, document.ImageSize)) {
			is_handle_scaling = true;
			using_mouse = true;
			OnStartTransform (document);
			// Grip scaling is computed in the reference frame (see ApplyRefScale).
			source_rect = ref_rect;
			return;
		}

		if (!document.Workspace.PointInCanvas (e.PointDouble))
			return;

		if (rotate_requested) {
			is_rotating = true;
			SetCursor (rotate_cursor);
		} else if (e.IsControlPressed)
			is_scaling = true;
		else
			is_dragging = true;

		using_mouse = true;

		OnStartTransform (document);
	}

	protected override void OnMouseMove (
		Document document,
		ToolMouseEventArgs e)
	{
		// While a grip is being dragged, scale the content to match the handle.
		if (is_handle_scaling) {
			HandlePoint? active = handle.ActiveHandlePoint;
			if (active is null)
				return;

			// Work in the reference frame: un-rotate the mouse through the live
			// orientation so a (possibly rotated) grip drag reduces to an
			// axis-aligned resize of ref_rect. The resize matrices built below
			// are then re-applied through `live` in ApplyRefScale, and the grips
			// are re-positioned in the oriented frame (never via handle.UpdateDrag,
			// which would corrupt the reference rectangle).
			Matrix liveInv = live.Clone ();
			liveInv.Invert ();
			PointD mouse = liveInv.TransformPoint (e.PointDouble);
			PointD srcCenter = source_rect.GetCenter ();
			bool keepAspect = e.IsShiftPressed;
			bool fromCenter = e.IsControlPressed;

			if (IsCorner (active)) {
				// --- Corner handles: allow flipping (mirroring) ---
				PointD opp = OppositeCorner (source_rect, active.Value);
				PointD srcCorner = GetCornerPoint (source_rect, active.Value);

				Matrix flipTransform = CairoExtensions.CreateIdentityMatrix ();
				RectangleD newRect;

				if (fromCenter) {
					// Scale about center; flip occurs when mouse crosses center.
					double cdx0 = srcCorner.X - srcCenter.X;
					double cdy0 = srcCorner.Y - srcCenter.Y;
					double cdx1 = mouse.X - srcCenter.X;
					double cdy1 = mouse.Y - srcCenter.Y;

					if (keepAspect && source_rect.Width > 0 && source_rect.Height > 0) {
						double halfW = source_rect.Width / 2.0;
						double halfH = source_rect.Height / 2.0;
						if (halfW > 0 && halfH > 0) {
							double s = Math.Max (Math.Abs (cdx1) / halfW, Math.Abs (cdy1) / halfH);
							double signX = cdx1 < 0 ? -1 : 1;
							double signY = cdy1 < 0 ? -1 : 1;
							// When exactly zero, keep positive to avoid NaN.
							cdx1 = signX * halfW * s;
							cdy1 = signY * halfH * s;
						}
					}

					double sx = cdx0 != 0 ? cdx1 / cdx0 : 1;
					double sy = cdy0 != 0 ? cdy1 / cdy0 : 1;

					double w = Math.Abs (cdx1) * 2;
					double h = Math.Abs (cdy1) * 2;
					newRect = new RectangleD (srcCenter.X - w / 2, srcCenter.Y - h / 2, w, h);

					flipTransform.Translate (srcCenter.X, srcCenter.Y);
					flipTransform.Scale (sx, sy);
					flipTransform.Translate (-srcCenter.X, -srcCenter.Y);
				} else {
					// Scale about opposite corner; flip occurs when mouse crosses that corner.
					double dx0 = srcCorner.X - opp.X;
					double dy0 = srcCorner.Y - opp.Y;
					double dx1 = mouse.X - opp.X;
					double dy1 = mouse.Y - opp.Y;

					if (keepAspect && source_rect.Width > 0 && source_rect.Height > 0) {
						double s = Math.Max (Math.Abs (dx1) / source_rect.Width, Math.Abs (dy1) / source_rect.Height);
						double signX = dx1 < 0 ? -1 : 1;
						double signY = dy1 < 0 ? -1 : 1;
						dx1 = signX * source_rect.Width * s;
						dy1 = signY * source_rect.Height * s;
					}

					double sx = dx0 != 0 ? dx1 / dx0 : 1;
					double sy = dy0 != 0 ? dy1 / dy0 : 1;

					double w = Math.Abs (dx1);
					double h = Math.Abs (dy1);
					double rx = Math.Min (opp.X, opp.X + dx1);
					double ry = Math.Min (opp.Y, opp.Y + dy1);
					newRect = new RectangleD (rx, ry, w, h);

					flipTransform.Translate (opp.X, opp.Y);
					flipTransform.Scale (sx, sy);
					flipTransform.Translate (-opp.X, -opp.Y);
				}

				_ = newRect; // grips are placed via the oriented frame, not this rect
				ApplyRefScale (document, flipTransform);
			} else {
				// Edge handles: allow flipping (mirroring) as well, so user can
				// mirror horizontally by dragging left/right past opposite edge,
				// and vertically by dragging up/down past opposite edge.
				Matrix edgeTransform = CairoExtensions.CreateIdentityMatrix ();
				RectangleD edgeRect;
				PointD edgeAnchor;

				double srcW = source_rect.Width;
				double srcH = source_rect.Height;

				switch (active.Value) {
					case HandlePoint.Left: {
						double oppX = source_rect.X + srcW;
						if (fromCenter) {
							double cdx0 = source_rect.X - srcCenter.X;
							double cdx1 = mouse.X - srcCenter.X;
							double sx = cdx0 != 0 ? cdx1 / cdx0 : 1;
							double w = Math.Abs (cdx1) * 2;
							double h = srcH;
							double sy = 1;
							if (keepAspect && srcW > 0) {
								h = w * srcH / srcW;
								sy = Math.Abs (sx);
							}
							edgeRect = new RectangleD (srcCenter.X - w / 2, srcCenter.Y - h / 2, w, h);
							edgeAnchor = srcCenter;
							edgeTransform.Translate (edgeAnchor.X, edgeAnchor.Y);
							edgeTransform.Scale (sx, sy);
							edgeTransform.Translate (-edgeAnchor.X, -edgeAnchor.Y);
						} else {
							double dx0 = source_rect.X - oppX;
							double dx1 = mouse.X - oppX;
							double sx = dx0 != 0 ? dx1 / dx0 : 1;
							double w = Math.Abs (dx1);
							double h = srcH;
							double sy = 1;
							if (keepAspect && srcW > 0) {
								h = w * srcH / srcW;
								sy = Math.Abs (sx);
							}
							double rx = Math.Min (oppX, mouse.X);
							double ry = keepAspect ? srcCenter.Y - h / 2 : source_rect.Y;
							edgeRect = new RectangleD (rx, ry, w, h);
							edgeAnchor = new PointD (oppX, srcCenter.Y);
							edgeTransform.Translate (edgeAnchor.X, edgeAnchor.Y);
							edgeTransform.Scale (sx, sy);
							edgeTransform.Translate (-edgeAnchor.X, -edgeAnchor.Y);
						}
						break;
					}
					case HandlePoint.Right: {
						double oppX = source_rect.X;
						if (fromCenter) {
							double cdx0 = source_rect.X + srcW - srcCenter.X;
							double cdx1 = mouse.X - srcCenter.X;
							double sx = cdx0 != 0 ? cdx1 / cdx0 : 1;
							double w = Math.Abs (cdx1) * 2;
							double h = srcH;
							double sy = 1;
							if (keepAspect && srcW > 0) {
								h = w * srcH / srcW;
								sy = Math.Abs (sx);
							}
							edgeRect = new RectangleD (srcCenter.X - w / 2, srcCenter.Y - h / 2, w, h);
							edgeAnchor = srcCenter;
							edgeTransform.Translate (edgeAnchor.X, edgeAnchor.Y);
							edgeTransform.Scale (sx, sy);
							edgeTransform.Translate (-edgeAnchor.X, -edgeAnchor.Y);
						} else {
							double dx0 = srcW;
							double dx1 = mouse.X - oppX;
							double sx = dx0 != 0 ? dx1 / dx0 : 1;
							double w = Math.Abs (dx1);
							double h = srcH;
							double sy = 1;
							if (keepAspect && srcW > 0) {
								h = w * srcH / srcW;
								sy = Math.Abs (sx);
							}
							double rx = Math.Min (oppX, mouse.X);
							double ry = keepAspect ? srcCenter.Y - h / 2 : source_rect.Y;
							edgeRect = new RectangleD (rx, ry, w, h);
							edgeAnchor = new PointD (oppX, srcCenter.Y);
							edgeTransform.Translate (edgeAnchor.X, edgeAnchor.Y);
							edgeTransform.Scale (sx, sy);
							edgeTransform.Translate (-edgeAnchor.X, -edgeAnchor.Y);
						}
						break;
					}
					case HandlePoint.Up: {
						double oppY = source_rect.Y + srcH;
						if (fromCenter) {
							double cdy0 = source_rect.Y - srcCenter.Y;
							double cdy1 = mouse.Y - srcCenter.Y;
							double sy = cdy0 != 0 ? cdy1 / cdy0 : 1;
							double h = Math.Abs (cdy1) * 2;
							double w = srcW;
							double sx = 1;
							if (keepAspect && srcH > 0) {
								w = h * srcW / srcH;
								sx = Math.Abs (sy);
							}
							edgeRect = new RectangleD (srcCenter.X - w / 2, srcCenter.Y - h / 2, w, h);
							edgeAnchor = srcCenter;
							edgeTransform.Translate (edgeAnchor.X, edgeAnchor.Y);
							edgeTransform.Scale (sx, sy);
							edgeTransform.Translate (-edgeAnchor.X, -edgeAnchor.Y);
						} else {
							double dy0 = source_rect.Y - oppY;
							double dy1 = mouse.Y - oppY;
							double sy = dy0 != 0 ? dy1 / dy0 : 1;
							double h = Math.Abs (dy1);
							double w = srcW;
							double sx = 1;
							if (keepAspect && srcH > 0) {
								w = h * srcW / srcH;
								sx = Math.Abs (sy);
							}
							double ry = Math.Min (oppY, mouse.Y);
							double rx = keepAspect ? srcCenter.X - w / 2 : source_rect.X;
							edgeRect = new RectangleD (rx, ry, w, h);
							edgeAnchor = new PointD (srcCenter.X, oppY);
							edgeTransform.Translate (edgeAnchor.X, edgeAnchor.Y);
							edgeTransform.Scale (sx, sy);
							edgeTransform.Translate (-edgeAnchor.X, -edgeAnchor.Y);
						}
						break;
					}
					case HandlePoint.Down: {
						double oppY = source_rect.Y;
						if (fromCenter) {
							double cdy0 = source_rect.Y + srcH - srcCenter.Y;
							double cdy1 = mouse.Y - srcCenter.Y;
							double sy = cdy0 != 0 ? cdy1 / cdy0 : 1;
							double h = Math.Abs (cdy1) * 2;
							double w = srcW;
							double sx = 1;
							if (keepAspect && srcH > 0) {
								w = h * srcW / srcH;
								sx = Math.Abs (sy);
							}
							edgeRect = new RectangleD (srcCenter.X - w / 2, srcCenter.Y - h / 2, w, h);
							edgeAnchor = srcCenter;
							edgeTransform.Translate (edgeAnchor.X, edgeAnchor.Y);
							edgeTransform.Scale (sx, sy);
							edgeTransform.Translate (-edgeAnchor.X, -edgeAnchor.Y);
						} else {
							double dy0 = srcH;
							double dy1 = mouse.Y - oppY;
							double sy = dy0 != 0 ? dy1 / dy0 : 1;
							double h = Math.Abs (dy1);
							double w = srcW;
							double sx = 1;
							if (keepAspect && srcH > 0) {
								w = h * srcW / srcH;
								sx = Math.Abs (sy);
							}
							double ry = Math.Min (oppY, mouse.Y);
							double rx = keepAspect ? srcCenter.X - w / 2 : source_rect.X;
							edgeRect = new RectangleD (rx, ry, w, h);
							edgeAnchor = new PointD (srcCenter.X, oppY);
							edgeTransform.Translate (edgeAnchor.X, edgeAnchor.Y);
							edgeTransform.Scale (sx, sy);
							edgeTransform.Translate (-edgeAnchor.X, -edgeAnchor.Y);
						}
						break;
					}
					default: {
						// Unreachable (all 8 HandlePoints handled above); kept as a guard.
						RectangleD to = ComputeEdgeRect (source_rect, ref_rect, active.Value, keepAspect, fromCenter);
						ApplyRefScale (document, ComputeScaleTransform (source_rect, to));
						return;
					}
				}

				_ = edgeRect; // grips are placed via the oriented frame, not this rect
				ApplyRefScale (document, edgeTransform);
			}
			return;
		}

		if (!IsActive || !using_mouse) {
			if (!using_mouse && handle.Active) {
				Gdk.Cursor? gripCursor = handle.GetCursorAtPoint (e.WindowPoint);
				// Alt rotates; show the rotate cursor. Otherwise show the grip's
				// resize cursor when hovering one.
				SetCursor (e.IsAltPressed ? rotate_cursor : gripCursor ?? DefaultCursor);
				// Hint the modifier keys when hovering a grip.
				UpdateHandleHint (gripCursor is not null);
			}
			return;
		}

		// Keep rotate cursor visible while actively rotating.
		if (is_rotating)
			SetCursor (rotate_cursor);

		bool constrain = e.IsShiftPressed;

		PointD center = source_rect.GetCenter ();

		// The cursor position can be a subpixel value. Round to an integer
		// so that we only translate by entire pixels.
		// (Otherwise, blurring / anti-aliasing may be introduced)

		double dx = Math.Floor (e.PointDouble.X - original_point.X);
		double dy = Math.Floor (e.PointDouble.Y - original_point.Y);

		PointD c1 = original_point - center;
		PointD c2 = e.PointDouble - center;

		RadiansAngle angle = new (Math.Atan2 (c1.Y, c1.X) - Math.Atan2 (c2.Y, c2.X));

		transform.InitIdentity ();

		if (is_scaling) {

			double sx = (c1.X + dx) / c1.X;
			double sy = (c1.Y + dy) / c1.Y;

			if (constrain) {

				double max_scale = Math.Max (Math.Abs (sx), Math.Abs (sy));

				sx = max_scale * Math.Sign (sx);
				sy = max_scale * Math.Sign (sy);
			}

			transform.Translate (center.X, center.Y);
			transform.Scale (sx, sy);
			transform.Translate (-center.X, -center.Y);
		} else if (is_rotating) {

			if (constrain)
				angle = Utility.GetNearestStepAngle (angle, rotate_steps);

			transform.Translate (center.X, center.Y);
			transform.Rotate (-angle.Radians);
			transform.Translate (-center.X, -center.Y);

		} else {
			transform.Translate (dx, dy);
		}

		OnUpdateTransform (document, transform);

		// Keep the grips glued to the moved/rotated content in the oriented frame.
		RefreshOrientedHandles (document);
	}

	protected override void OnMouseUp (
		Document document,
		ToolMouseEventArgs e)
	{
		if (!IsActive || !using_mouse)
			return;

		if (is_handle_scaling)
			handle.EndDrag ();

		// For handle scaling we already computed the (possibly flipped) transform
		// in OnMouseMove and stored it in `transform`. Using ComputeScaleTransform
		// from the positive bounding rect would lose the sign and thus the flip.
		Matrix final = transform;

		OnFinishTransform (document, final);
	}

	protected override bool OnKeyDown (
		Document document,
		ToolKeyEventArgs e)
	{
		if (using_mouse) // Don't handle the arrow keys while already interacting via the mouse.
			return base.OnKeyDown (document, e);

		// Determine if this is an arrow key for nudging.
		bool isArrow = e.Key.Value is Gdk.Constants.KEY_Left or Gdk.Constants.KEY_Right
			or Gdk.Constants.KEY_Up or Gdk.Constants.KEY_Down;

		if (!isArrow)
			return base.OnKeyDown (document, e);

		// Track nudge hold duration for hint (2 seconds).
		if (nudge_start_time is null) {
			nudge_start_time = DateTime.UtcNow;
			// Schedule a one-shot timeout to show hint after 2s even if no key repeat.
			if (nudge_hint_timeout_id == 0) {
				Document docForHint = document;
				nudge_hint_timeout_id = GLib.Functions.TimeoutAdd (0, 2000, () => {
					nudge_hint_timeout_id = 0;
					if (nudge_start_time is not null && IsActive && !using_mouse) {
						ShowNudgeHint (docForHint);
					}
					return false;
				});
			}
		} else {
			// If we've been holding for >2s, ensure hint is visible and updated.
			if ((DateTime.UtcNow - nudge_start_time.Value).TotalSeconds >= 2.0) {
				ShowNudgeHint (document);
			}
		}

		// Compute step amounts:
		// - 1px base
		// - Shift: 10px (Paint.NET parity, Issue #1559)
		// - Ctrl: 10% of canvas size (user request: based on canvas size)
		// - Ctrl+Shift: 20% of canvas size
		double dx = 0.0;
		double dy = 0.0;

		bool isCtrl = e.IsControlPressed;
		bool isShift = e.IsShiftPressed;

		int canvasW = document.ImageSize.Width;
		int canvasH = document.ImageSize.Height;

		// 10% and 20% of canvas, with sensible minimums so Ctrl is distinguishable.
		int ctrl10X = Math.Max (10, (int) Math.Round (canvasW * 0.10));
		int ctrl10Y = Math.Max (10, (int) Math.Round (canvasH * 0.10));
		int ctrl20X = Math.Max (20, (int) Math.Round (canvasW * 0.20));
		int ctrl20Y = Math.Max (20, (int) Math.Round (canvasH * 0.20));

		double stepX, stepY;

		if (isCtrl && isShift) {
			stepX = ctrl20X;
			stepY = ctrl20Y;
		} else if (isCtrl) {
			stepX = ctrl10X;
			stepY = ctrl10Y;
		} else if (isShift) {
			stepX = stepY = 10.0;
		} else {
			stepX = stepY = 1.0;
		}

		switch (e.Key.Value) {
			case Gdk.Constants.KEY_Left:
				dx = -stepX;
				break;
			case Gdk.Constants.KEY_Right:
				dx = stepX;
				break;
			case Gdk.Constants.KEY_Up:
				dy = -stepY;
				break;
			case Gdk.Constants.KEY_Down:
				dy = stepY;
				break;
		}

		if (!IsActive) {
			is_dragging = true;
			OnStartTransform (document);
		}

		transform.Translate (dx, dy);
		OnUpdateTransform (document, transform);

		// Keep handles glued to the nudged content in realtime (issue #1).
		RefreshOrientedHandles (document);

		// If hint is already visible, refresh its position/content (e.g., ctrl px changes).
		if (nudge_hint_visible) {
			ShowNudgeHint (document);
		}

		return true;
	}

	protected override bool OnKeyUp (
		Document document,
		ToolKeyEventArgs e)
	{
		// Clear nudge hint state when arrow key is released.
		if (e.Key.Value is Gdk.Constants.KEY_Left or Gdk.Constants.KEY_Right
			or Gdk.Constants.KEY_Up or Gdk.Constants.KEY_Down) {
			ClearNudgeState ();
		}

		if (IsActive && !using_mouse)
			OnFinishTransform (document, transform);

		return base.OnKeyUp (document, e);
	}

	protected abstract RectangleD GetSourceRectangle (Document document);

	protected virtual void OnStartTransform (Document document)
	{
		source_rect = GetSourceRectangle (document);
		transform.InitIdentity ();
	}

	protected virtual void OnUpdateTransform (
		Document document,
		Matrix transform)
	{ }

	protected virtual void OnFinishTransform (
		Document document,
		Matrix transform)
	{
		is_dragging = false;
		is_rotating = false;
		is_scaling = false;
		is_handle_scaling = false;
		using_mouse = false;

		// Clear any nudge hint when transform finishes.
		ClearNudgeState ();

		// Commit this gesture into the live orientation and snap the grips onto
		// it, so the next gesture (and the drawn handles) stay in the oriented
		// frame instead of resetting to an axis-aligned box (issue #4).
		live.Multiply (transform);
		if (document.Selection.Visible)
			handle.SetOriented (ref_rect, live.Clone ());
		else
			handle.Active = false;
		document.Workspace.Invalidate ();

		// Restore cursor after a rotate/scale gesture
		SetCursor (DefaultCursor);
	}

	protected override void OnActivated (Document? document)
	{
		base.OnActivated (document);
		UpdateHandlesFromDocument (document);
	}

	protected override void OnDeactivated (Document? document, BaseTool? newTool)
	{
		base.OnDeactivated (document, newTool);
		handle.Active = false;
		UpdateHandleHint (false);
		ClearNudgeState ();

		// Fully detach popover to avoid holding parent reference.
		if (nudge_popover is not null) {
			try {
				nudge_popover.Popdown ();
				nudge_popover.Unparent ();
			} catch { }
			nudge_popover = null;
			nudge_label = null;
		}
	}

	/// <summary>
	/// Position the resize grips on the current selection's bounding box, and
	/// only show them when there is a <em>visible</em> selection to transform.
	/// A fresh document carries an invisible full-canvas select-all
	/// (<c>ResetSelectionPaths</c>), so gating on <c>SelectionPolygons.Count</c>
	/// would wrongly show grips on the default layer; paste sets <c>Visible</c>.
	/// </summary>
	private void UpdateHandlesFromDocument (Document? document)
	{
		bool hasSelection = document is not null && document.Selection.Visible;

		if (!hasSelection) {
			handle.Active = false;
			return;
		}

		// Derive the grips from the selection's own polygon. It is part of
		// document.Selection, so history saves/restores it, and it outlines the
		// transformed content exactly (a rotate maps the polygon corners the same
		// way it maps the pixels). A rotated rectangular selection is a 4-corner
		// quad; map an axis-aligned reference rect onto it so the existing
		// draw/hit-test/scale code (which works in ref space) keeps functioning.
		// Non-rectangular selections fall back to the axis-aligned bounding box.
		if (TryGetOrientedQuad (document!, out RectangleD refRect, out Matrix orientation)) {
			ref_rect = refRect;
			live.InitMatrix (orientation);
		} else {
			ref_rect = GetSourceRectangle (document!);
			live.InitIdentity ();
		}
		handle.Active = true;
		handle.SetOriented (ref_rect, live.Clone ());
		document!.Workspace.Invalidate ();
	}

	/// <summary>
	/// Re-position the grips on the moved/rotated content during a non-grip
	/// gesture (body drag, rotate, nudge). The content transform for the whole
	/// gesture is <c>transform</c>; the grips follow at <c>live · transform</c>.
	/// </summary>
	private void RefreshOrientedHandles (Document document)
	{
		Matrix disp = live.Clone ();
		disp.Multiply (transform);
		handle.SetOriented (ref_rect, disp);
		document.Workspace.Invalidate ();
	}

	/// <summary>
	/// Apply a resize computed in the reference frame (<paramref name="s"/> maps
	/// ref_rect onto the resized rect) to the actual content, mapped through the
	/// live orientation so rotated content scales along its own axes:
	/// <c>g = live · s · live⁻¹</c>. The grips are drawn at the candidate
	/// post-drag orientation (committed on mouse-up in OnFinishTransform).
	/// </summary>
	private void ApplyRefScale (Document document, Matrix s)
	{
		Matrix liveInv = live.Clone ();
		liveInv.Invert ();

		Matrix g = liveInv;   // apply live⁻¹ first,
		g.Multiply (s);       // then the ref-space resize,
		g.Multiply (live);    // then re-apply live.

		transform.InitMatrix (g);
		OnUpdateTransform (document, transform);

		Matrix disp = live.Clone ();
		disp.Multiply (g);
		handle.SetOriented (ref_rect, disp);
		document.Workspace.Invalidate ();
	}

	/// <summary>
	/// Show/clear a canvas tooltip explaining the modifier keys while the
	/// cursor is over a resize grip.
	/// </summary>
	private void UpdateHandleHint (bool overGrip)
	{
		if (!workspace.HasOpenDocuments)
			return;

		if (nudge_hint_visible)
			return;

		string? hint = overGrip
			// Translators: hint shown when hovering a selection resize handle. Now lists shortcuts vertically.
			? Translations.GetString ("Drag to resize\nShift: keep aspect ratio\nCtrl+drag: scale from center\nAlt-drag: rotate")
			: null;

		Gtk.Widget canvas = workspace.ActiveWorkspace.Canvas;
		if (canvas.TooltipText != hint)
			canvas.SetTooltipText (hint);
	}

	/// <summary>
	/// Show a hint about arrow-key nudging after holding for 2 seconds.
	/// Similar UI to the tool menu button's hint popovers (issue #1559).
	/// Anchored to the lower-right of the nudged area so it appears near the
	/// content even when the mouse is elsewhere (keyboard-only use).
	/// </summary>
	private void ShowNudgeHint (Document document)
	{
		if (!workspace.HasOpenDocuments)
			return;

		int w = document.ImageSize.Width;
		int h = document.ImageSize.Height;
		int ctrl10X = Math.Max (10, (int) Math.Round (w * 0.10));
		int ctrl10Y = Math.Max (10, (int) Math.Round (h * 0.10));
		int ctrl20X = Math.Max (20, (int) Math.Round (w * 0.20));
		int ctrl20Y = Math.Max (20, (int) Math.Round (h * 0.20));

		// List shortcuts vertically per user request (instead of dot-separated).
		string template = Translations.GetString (
			"Arrow: 1px\nShift+Arrow: 10px\nCtrl+Arrow: 10% of canvas ({0}×{1}px)\nCtrl+Shift+Arrow: 20% of canvas ({2}×{3}px)");

		string hint;
		try {
			hint = string.Format (template, ctrl10X, ctrl10Y, ctrl20X, ctrl20Y);
		} catch {
			string fallback = Translations.GetString (
				"Arrow: 1px\nShift+Arrow: 10px\nCtrl+Arrow: 10% of canvas\nCtrl+Shift+Arrow: 20% of canvas");
			hint = fallback;
		}

		var activeWs = workspace.ActiveWorkspace;
		Gtk.Widget canvas = activeWs.Canvas;

		// Determine lower-right of the nudged area in canvas coordinates.
		RectangleD rect;
		if (handle.Active) {
			rect = handle.Rectangle;
		} else {
			// Fallback to current selection bounds.
			try {
				rect = GetSourceRectangle (document);
			} catch {
				rect = document.Selection.GetBounds ();
			}
		}

		PointD lowerRightCanvas = new (rect.X + rect.Width, rect.Y + rect.Height);
		// Anchor to the oriented lower-right when the content is rotated (issue #4).
		if (handle.Orientation is not null)
			lowerRightCanvas = handle.Orientation.TransformPoint (lowerRightCanvas);
		PointD lowerRightView = activeWs.CanvasPointToView (lowerRightCanvas);

		// Create or reuse popover + label.
		if (nudge_popover is null) {
			nudge_popover = Gtk.Popover.New ();
			nudge_popover.Autohide = false;
			nudge_popover.Position = Gtk.PositionType.Bottom;
			nudge_popover.SetParent (canvas);
			nudge_label = Gtk.Label.New (hint);
			nudge_label.Wrap = true;
			nudge_label.MaxWidthChars = 60;
			nudge_popover.SetChild (nudge_label);
		} else {
			if (nudge_label is not null)
				nudge_label.SetText (hint);
			// Re-parent if canvas changed.
			if (nudge_popover.GetParent () != canvas) {
				nudge_popover.Unparent ();
				nudge_popover.SetParent (canvas);
			}
		}

		// Anchor popover to lower-right of the nudge area.
		// If mouse is elsewhere, popover still appears near content (fixes issue #2).
		Gdk.Rectangle pointing = new () {
			X = (int) Math.Clamp (lowerRightView.X, 0, 10000),
			Y = (int) Math.Clamp (lowerRightView.Y, 0, 10000),
			Width = 1,
			Height = 1
		};
		nudge_popover.PointingTo = pointing;

		// Also set tooltip as fallback for accessibility / hover.
		canvas.SetTooltipText (hint);

		nudge_popover.Popup ();
		last_nudge_hint = hint;
		nudge_hint_visible = true;
	}

	private void HideNudgeHint ()
	{
		if (!nudge_hint_visible && nudge_popover is null)
			return;

		if (workspace.HasOpenDocuments) {
			try {
				Gtk.Widget canvas = workspace.ActiveWorkspace.Canvas;
				if (last_nudge_hint is not null && canvas.TooltipText == last_nudge_hint) {
					canvas.SetTooltipText (null);
				} else if (canvas.TooltipText is not null && nudge_hint_visible) {
					if (canvas.TooltipText.Contains ("Arrow:") || canvas.TooltipText.Contains ("Shift+Arrow"))
						canvas.SetTooltipText (null);
				}
			} catch {
				// Workspace may be disposed.
			}
		}

		if (nudge_popover is not null) {
			try {
				nudge_popover.Popdown ();
			} catch {
				// Ignore if already closed.
			}
		}

		nudge_hint_visible = false;
		last_nudge_hint = null;
	}

	private void ClearNudgeState ()
	{
		if (nudge_hint_timeout_id != 0) {
			GLib.Functions.SourceRemove (nudge_hint_timeout_id);
			nudge_hint_timeout_id = 0;
		}

		nudge_start_time = null;
		HideNudgeHint ();
	}

	/// <summary>
	/// Build the transform that maps the <paramref name="from"/> rectangle onto
	/// <paramref name="to"/>. Because the dragged handle keeps the opposite corner
	/// fixed, this scales relative to that corner rather than the center.
	/// </summary>
	private Matrix ComputeScaleTransform (RectangleD from, RectangleD to)
	{
		double sx = from.Width != 0 ? to.Width / from.Width : 1.0;
		double sy = from.Height != 0 ? to.Height / from.Height : 1.0;

		transform.InitIdentity ();
		transform.Translate (to.X, to.Y);
		transform.Scale (sx, sy);
		transform.Translate (-from.X, -from.Y);
		return transform;
	}

	/// <summary>
	/// If the selection is a rotated rectangle (a 4-corner quad), returns the
	/// axis-aligned reference rect <c>(0,0,w,h)</c> and the <paramref name="orientation"/>
	/// matrix that maps it onto that quad. The quad comes straight from the
	/// selection polygon, so it matches the on-screen content at any history step.
	/// Returns false for non-rectangular selections (caller uses the bbox).
	/// </summary>
	private static bool TryGetOrientedQuad (Document document, out RectangleD refRect, out Matrix orientation)
	{
		refRect = default;
		orientation = CairoExtensions.CreateIdentityMatrix ();

		var polys = document.Selection.SelectionPolygons;
		// A rectangle is 4 corners (some paths repeat the first point to close).
		if (polys.Count != 1 || (polys[0].Count != 4 && polys[0].Count != 5))
			return false;

		// p0 and its two adjacent corners p1 (width edge) and p3 (height edge).
		PointD p0 = new (polys[0][0].X, polys[0][0].Y);
		PointD p1 = new (polys[0][1].X, polys[0][1].Y);
		PointD p3 = new (polys[0][3].X, polys[0][3].Y);

		PointD widthVec = p1 - p0;
		PointD heightVec = p3 - p0;
		double w = Math.Sqrt (widthVec.X * widthVec.X + widthVec.Y * widthVec.Y);
		double h = Math.Sqrt (heightVec.X * heightVec.X + heightVec.Y * heightVec.Y);
		if (w < 1e-6 || h < 1e-6)
			return false;

		PointD ex = new (widthVec.X / w, widthVec.Y / w);
		PointD ey = new (heightVec.X / h, heightVec.Y / h);

		// Reject non-orthogonal quads (sheared/arbitrary polygons); we only model
		// rotated + flipped rectangles. SelectionPolygons stores integer points
		// (DocumentSelection.Transform truncates to IntPoint), so a genuinely
		// rotated rectangle carries up to ~1px error per corner — scale the
		// tolerance with edge length instead of demanding exact orthogonality.
		double dot = ex.X * ey.X + ex.Y * ey.Y;
		double tol = Math.Min (0.1, 4.0 * (1.0 / w + 1.0 / h));
		if (Math.Abs (dot) > tol)
			return false;

		double theta = Math.Atan2 (ex.Y, ex.X);
		double det = ex.X * ey.Y - ex.Y * ey.X; // ex × ey; <0 = mirrored

		Matrix m = CairoExtensions.CreateIdentityMatrix ();
		m.Translate (p0.X, p0.Y);
		m.Rotate (theta);
		if (det < 0)
			m.Scale (1, -1); // height edge is mirrored relative to a pure rotation

		refRect = new RectangleD (0, 0, w, h);
		orientation = m;
		return true;
	}

	private static bool IsCorner (HandlePoint? p)
		=> p is HandlePoint.UpperLeft or HandlePoint.UpperRight
			or HandlePoint.LowerLeft or HandlePoint.LowerRight;

	private static PointD ClampToImage (PointD p, Document document)
		=> new (
			Math.Round (Math.Clamp (p.X, 0, document.ImageSize.Width)),
			Math.Round (Math.Clamp (p.Y, 0, document.ImageSize.Height)));

	/// <summary>
	/// The corner of <paramref name="s"/> diagonally opposite the dragged one;
	/// this corner stays fixed while the dragged corner follows the mouse.
	/// </summary>
	private static PointD OppositeCorner (RectangleD s, HandlePoint dragged) => dragged switch {
		HandlePoint.UpperLeft => new (s.Right, s.Bottom),
		HandlePoint.UpperRight => new (s.Left, s.Bottom),
		HandlePoint.LowerLeft => new (s.Right, s.Top),
		HandlePoint.LowerRight => new (s.Left, s.Top),
		_ => s.GetCenter (),
	};

	private static PointD GetCornerPoint (RectangleD s, HandlePoint dragged) => dragged switch {
		HandlePoint.UpperLeft => new (s.X, s.Y),
		HandlePoint.UpperRight => new (s.X + s.Width, s.Y),
		HandlePoint.LowerLeft => new (s.X, s.Y + s.Height),
		HandlePoint.LowerRight => new (s.X + s.Width, s.Y + s.Height),
		HandlePoint.Left => new (s.X, s.GetCenter ().Y),
		HandlePoint.Right => new (s.X + s.Width, s.GetCenter ().Y),
		HandlePoint.Up => new (s.GetCenter ().X, s.Y),
		HandlePoint.Down => new (s.GetCenter ().X, s.Y + s.Height),
		_ => s.GetCenter (),
	};

	/// <summary>
	/// Target rectangle for a corner drag. Shift keeps the pasted content's
	/// original width:height ratio (not a square); Ctrl anchors the scale to
	/// the center instead of the opposite corner.
	/// </summary>
	private static RectangleD ComputeCornerRect (RectangleD source, PointD mouse, HandlePoint dragged, bool keepAspect, bool fromCenter)
	{
		PointD anchor = OppositeCorner (source, dragged);
		double dx = mouse.X - anchor.X;
		double dy = mouse.Y - anchor.Y;

		if (keepAspect && source.Width > 0 && source.Height > 0) {
			// Scale both axes by the same factor, following whichever axis was dragged further.
			double s = Math.Max (Math.Abs (dx) / source.Width, Math.Abs (dy) / source.Height);
			dx = (dx < 0 ? -1 : 1) * source.Width * s;
			dy = (dy < 0 ? -1 : 1) * source.Height * s;
		}

		RectangleD rect = RectangleD.FromPoints (anchor, new PointD (anchor.X + dx, anchor.Y + dy), true);
		return fromCenter ? CenterAnchored (source, rect) : rect;
	}

	/// <summary>
	/// Target rectangle for an edge drag. The dragged edge changes one dimension;
	/// Shift derives the other dimension from the pasted content's aspect ratio
	/// (expanding/contracting symmetrically about the perpendicular centerline),
	/// and Ctrl anchors the whole scale to the center.
	/// </summary>
	private static RectangleD ComputeEdgeRect (RectangleD source, RectangleD dragged, HandlePoint edge, bool keepAspect, bool fromCenter)
	{
		RectangleD rect = dragged;

		if (keepAspect && source.Width > 0 && source.Height > 0) {
			PointD c = source.GetCenter ();
			if (edge is HandlePoint.Left or HandlePoint.Right) {
				// Width was dragged; derive height, centered vertically.
				double height = dragged.Width * source.Height / source.Width;
				rect = new RectangleD (new PointD (dragged.X, c.Y - height / 2), dragged.Width, height);
			} else {
				// Up/Down: height was dragged; derive width, centered horizontally.
				double width = dragged.Height * source.Width / source.Height;
				rect = new RectangleD (new PointD (c.X - width / 2, dragged.Y), width, dragged.Height);
			}
		}

		return fromCenter ? CenterAnchored (source, rect) : rect;
	}

	/// <summary>
	/// Re-centers <paramref name="rect"/> on the source's center, keeping its size,
	/// so scaling grows symmetrically about the center (Ctrl behavior).
	/// </summary>
	private static RectangleD CenterAnchored (RectangleD source, RectangleD rect)
	{
		PointD c = source.GetCenter ();
		return new RectangleD (new PointD (c.X - rect.Width / 2, c.Y - rect.Height / 2), rect.Width, rect.Height);
	}

	/// <summary>
	/// Load the rotate cursor texture with white halo + black outline preserved.
	/// Tries direct file load first (bypasses Gtk IconTheme recoloring), then
	/// Resources.GetIcon, then a manual Cairo fallback.
	/// </summary>
	private static Gdk.Texture LoadRotateTexture ()
	{
		// Try direct file load from the installed icons directory — this preserves
		// the white halo + black stroke, unlike the symbolic IconTheme path.
		string data_dir = Pinta.Core.SystemManager.GetDataRootDirectory ();
		string[] candidates = [
			System.IO.Path.Combine (data_dir, "icons", "hicolor", "scalable", "actions", "rotate-handle.svg"),
			System.IO.Path.Combine (data_dir, "icons", "hicolor", "scalable", "actions", "rotate-handle-symbolic.svg"),
		];

		foreach (string path in candidates) {
			try {
				if (!File.Exists (path))
					continue;
				byte[] data = File.ReadAllBytes (path);
				GLib.Bytes bytes = GLib.Bytes.New (data);
				Gdk.Texture tex = Gdk.Texture.NewFromBytes (bytes);
				if (tex.Width > 0 && tex.Height > 0)
					return tex;
			} catch {
				// fall through
			}
		}

		// Fallback to themed icon (may be recolored but better than nothing).
		try {
			Gdk.Texture themed = Pinta.Resources.ResourceLoader.GetIcon (Pinta.Resources.Icons.RotateHandle, 28);
			if (themed.Width > 0)
				return themed;
		} catch {
		}

		// Final fallback: draw a high-contrast rotate icon via Cairo (white halo + black arc).
		return CreateFallbackRotateTexture (32);
	}

	private static Gdk.Texture CreateFallbackRotateTexture (int size)
	{
		using ImageSurface surf = new (Format.Argb32, size, size);
		using Context g = new (surf);
		g.Antialias = Antialias.Subpixel;
		g.LineCap = LineCap.Round;
		g.LineJoin = LineJoin.Round;

		// Clear transparent.
		g.Operator = Operator.Source;
		g.SetSourceRgba (0, 0, 0, 0);
		g.Paint ();
		g.Operator = Operator.Over;

		double cx = size / 2.0;
		double cy = size / 2.0;
		double radius = size * 0.35; // ~11 at 32px, similar to 8 at 24px
		double gap_deg = 35.0;
		double start_rad = gap_deg * Math.PI / 180.0;
		double end_rad = (360.0 - gap_deg) * Math.PI / 180.0;

		// Helper to stroke arc + arrowheads.
		void StrokeArc (double r, double width, double[] color)
		{
			g.LineWidth = width;
			g.SetSourceRgb (color[0], color[1], color[2]);
			// Arc (Cairo angles: 0 = +X, positive clockwise because Y down, but we use math)
			g.Arc (cx, cy, r, start_rad, end_rad);
			g.Stroke ();

			// Arrowheads — small V at each end, tangent to circle.
			// Compute end points
			double sx = cx + r * Math.Cos (start_rad);
			double sy = cy + r * Math.Sin (start_rad);
			double ex = cx + r * Math.Cos (end_rad);
			double ey = cy + r * Math.Sin (end_rad);

			// Tangent directions (perpendicular to radius). For a clockwise arc,
			// tangent at start is roughly upwards-ish, at end downwards-ish.
			// Approximate arrowhead by drawing two short lines.
			double arrow_len = size * 0.18;
			double arrow_angle = 25.0 * Math.PI / 180.0;

			// Start arrow — tangent roughly -90deg from radius
			double t1 = start_rad - Math.PI / 2;
			g.MoveTo (sx, sy);
			g.LineTo (sx + arrow_len * Math.Cos (t1 + arrow_angle), sy + arrow_len * Math.Sin (t1 + arrow_angle));
			g.MoveTo (sx, sy);
			g.LineTo (sx + arrow_len * Math.Cos (t1 - arrow_angle), sy + arrow_len * Math.Sin (t1 - arrow_angle));
			g.Stroke ();

			// End arrow — tangent +90deg
			double t2 = end_rad + Math.PI / 2;
			g.MoveTo (ex, ey);
			g.LineTo (ex + arrow_len * Math.Cos (t2 + arrow_angle), ey + arrow_len * Math.Sin (t2 + arrow_angle));
			g.MoveTo (ex, ey);
			g.LineTo (ex + arrow_len * Math.Cos (t2 - arrow_angle), ey + arrow_len * Math.Sin (t2 - arrow_angle));
			g.Stroke ();
		}

		// White halo
		StrokeArc (radius, size * 0.16, [1, 1, 1]);
		// Black arc
		StrokeArc (radius, size * 0.09, [0.1, 0.1, 0.1]);

		return Gdk.Texture.NewForPixbuf (Gdk.Functions.PixbufGetFromSurface (surf, 0, 0, surf.Width, surf.Height)!);
	}

	private bool IsActive
		=> is_dragging || is_rotating || is_scaling || is_handle_scaling;
}

