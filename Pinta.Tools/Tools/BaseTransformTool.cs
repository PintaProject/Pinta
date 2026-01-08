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
using Cairo;
using Pinta.Core;

namespace Pinta.Tools;

public abstract class BaseTransformTool : BaseTool
{
	private readonly IWorkspaceService workspace;

	private readonly int rotate_steps = 32;
	private readonly Matrix transform = CairoExtensions.CreateIdentityMatrix ();
	private RectangleD source_rect;
	private PointD original_point;
	private bool is_dragging = false;
	private bool is_rotating = false;
	private bool is_scaling = false;
	private bool using_mouse = false;

	private readonly RectangleHandle rect_handle;

	/// <summary>
	/// Initializes a new instance of the <see cref="BaseTransformTool"/> class.
	/// </summary>
	public BaseTransformTool (IServiceProvider services) : base (services)
	{
		workspace = services.GetService<IWorkspaceService> ();

		rect_handle = new (workspace) {
			InvertIfNegative = false,
			Active = true
		};
	}

	public override IEnumerable<IToolHandle> Handles => [rect_handle];

	protected override void OnMouseDown (
		Document document,
		ToolMouseEventArgs e)
	{
		if (IsActive)
			return;

		original_point = e.PointDouble;

		if (!document.Workspace.PointInCanvas (e.PointDouble))
			return;

		if (e.MouseButton == MouseButton.Right)
			is_rotating = true;
		else if (rect_handle.BeginDrag (e.PointDouble, document.ImageSize))
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
		if (!IsActive || !using_mouse) {
			UpdateCursor (e.WindowPoint);
			return;
		}

		transform.InitIdentity ();

		if (is_scaling) {
			// TODO - the constrain option should preserve the original aspect ratio, rather than creating a square.
			rect_handle.UpdateDrag (e.PointDouble, e.IsShiftPressed);

			// Scale the original rectangle to fit the target rectangle.
			RectangleD targetRect = rect_handle.Rectangle;
			double sx = (source_rect.Width > 0) ? (targetRect.Width / source_rect.Width) : 0.0;
			double sy = (source_rect.Height > 0) ? (targetRect.Height / source_rect.Height) : 0.0;

			transform.Translate (targetRect.Left, targetRect.Top);
			transform.Scale (sx, sy);
			transform.Translate (-source_rect.Left, -source_rect.Top);
		} else if (is_rotating) {
			PointD center = source_rect.GetCenter ();

			PointD c1 = original_point - center;
			PointD c2 = e.PointDouble - center;

			RadiansAngle angle = new (Math.Atan2 (c1.Y, c1.X) - Math.Atan2 (c2.Y, c2.X));

			if (e.IsShiftPressed)
				angle = Utility.GetNearestStepAngle (angle, rotate_steps);

			transform.Translate (center.X, center.Y);
			transform.Rotate (-angle.Radians);
			transform.Translate (-center.X, -center.Y);

		} else {
			// The cursor position can be a subpixel value. Round to an integer
			// so that we only translate by entire pixels.
			// (Otherwise, blurring / anti-aliasing may be introduced)
			double dx = Math.Floor (e.PointDouble.X - original_point.X);
			double dy = Math.Floor (e.PointDouble.Y - original_point.Y);
			transform.Translate (dx, dy);

			// Update the rectangle handle.
			rect_handle.Rectangle = source_rect with { X = source_rect.X + dx, Y = source_rect.Y + dy };
		}

		OnUpdateTransform (document, transform);
	}

	protected override void OnMouseUp (
		Document document,
		ToolMouseEventArgs e)
	{
		if (!IsActive || !using_mouse)
			return;

		if (is_scaling)
			rect_handle.EndDrag ();

		OnFinishTransform (document, transform);
		UpdateCursor (e.WindowPoint);
	}

	protected override bool OnKeyDown (
		Document document,
		ToolKeyEventArgs e)
	{
		if (using_mouse) // Don't handle the arrow keys while already interacting via the mouse.
			return base.OnKeyDown (document, e);

		double dx = 0.0;
		double dy = 0.0;

		switch (e.Key.Value) {
			case Gdk.Constants.KEY_Left:
				dx = -1;
				break;
			case Gdk.Constants.KEY_Right:
				dx = 1;
				break;
			case Gdk.Constants.KEY_Up:
				dy = -1;
				break;
			case Gdk.Constants.KEY_Down:
				dy = 1;
				break;
			default:
				// Otherwise, let the key be handled elsewhere.
				return base.OnKeyDown (document, e);
		}

		if (!IsActive) {
			is_dragging = true;
			OnStartTransform (document);
		}

		transform.Translate (dx, dy);
		OnUpdateTransform (document, transform);

		return true;
	}

	protected override bool OnKeyUp (
		Document document,
		ToolKeyEventArgs e)
	{
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
		using_mouse = false;
	}

	private bool IsActive
		=> is_dragging || is_rotating || is_scaling;

	protected override void OnActivated (Document? document)
	{
		base.OnActivated (document);

		workspace.ActiveDocumentChanged += HandleActiveDocumentChanged;

		if (document is not null)
			UpdateSourceRectangle (document);
	}

	protected override void OnDeactivated (Document? document, BaseTool? newTool)
	{
		base.OnDeactivated (document, newTool);

		workspace.ActiveDocumentChanged -= HandleActiveDocumentChanged;
	}

	protected override void OnAfterUndo (Document document)
	{
		base.OnAfterUndo (document);
		UpdateSourceRectangle (document);
	}

	protected override void OnAfterRedo (Document document)
	{
		base.OnAfterRedo (document);
		UpdateSourceRectangle (document);
	}

	/// <summary>
	/// Update the handles whenever we switch to a new document.
	/// </summary>
	private void HandleActiveDocumentChanged (object? sender, EventArgs args)
	{
		if (!PintaCore.Workspace.HasOpenDocuments)
			return;

		UpdateSourceRectangle (PintaCore.Workspace.ActiveDocument);
	}

	private void UpdateSourceRectangle (Document document)
	{
		rect_handle.Rectangle = GetSourceRectangle (document);
	}

	private void UpdateCursor (in PointD viewPos)
	{
		Gdk.Cursor? cursor = null;
		if (rect_handle.Active)
			cursor = rect_handle.GetCursorAtPoint (viewPos);

		SetCursor (cursor ?? DefaultCursor);
	}
}

