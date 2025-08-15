// 
// PointPickerGraphic.cs
//  
// Author:
//       Olivier Dufour <olivier.duff@gmail.com>
// 
// Copyright (c) 2010 Olivier Dufour
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
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class PointPickerGraphic : Gtk.DrawingArea
{
	private ImageSurface? thumbnail;
	private PointI position;
	private PointD drag_start;
	private readonly IWorkspaceService workspace;

	public PointPickerGraphic (IWorkspaceService workspace)
	{
		// TODO-GTK (improvement) - the allocated width should depend on the image aspect ratio. See the old GTK3 implementation
		HeightRequest = WidthRequest = 65;

		OnResize += (_, _) => UpdateThumbnail ();
		PositionChanged += (_, _) => QueueDraw ();

		SetDrawFunc ((area, context, width, height) => Draw (context));

		// Handle click + drag.
		Gtk.GestureDrag dragGesture = Gtk.GestureDrag.New ();
		dragGesture.SetButton (GtkExtensions.MOUSE_LEFT_BUTTON);
		dragGesture.OnDragBegin += (_, args) => {
			drag_start = new PointD (args.StartX, args.StartY);
			Position = MousePtToPosition (drag_start);
			dragGesture.SetState (Gtk.EventSequenceState.Claimed);
		};
		dragGesture.OnDragUpdate += (_, args) => {
			PointD dragOffset = new (args.OffsetX, args.OffsetY);
			Position = MousePtToPosition (drag_start + dragOffset);
		};
		dragGesture.OnDragEnd += (_, args) => {
			PointD dragOffset = new (args.OffsetX, args.OffsetY);
			Position = MousePtToPosition (drag_start + dragOffset);
		};

		AddController (dragGesture);

		this.workspace = workspace;
	}

	private void UpdateThumbnail ()
	{
		Document doc = workspace.ActiveDocument;

		RectangleI bounds = GetDrawBounds ();

		double scaleX = bounds.Width / (double) workspace.ImageSize.Width;
		double scaleY = bounds.Height / (double) workspace.ImageSize.Height;

		thumbnail = CairoExtensions.CreateImageSurface (Format.Argb32, bounds.Width, bounds.Height);

		using Context g = new (thumbnail);
		g.Scale (scaleX, scaleY);

		foreach (var layer in doc.Layers.GetLayersToPaint ())
			layer.Draw (g);
	}

	public void Init (PointI position)
	{
		this.position = position;
	}

	public PointI Position {
		get => position;
		set {
			if (position == value) return;
			position = value;
			OnPositionChanged ();
		}
	}

	private readonly record struct PointPickerVisualSettings (
		Color outerFrameColor,
		Color innerFrameColor,
		Color lineColor,
		Color pointMarkerColor,
		RectangleD outerFrame,
		RectangleD innerFrame,
		PointD verticalLineStart,
		PointD verticalLineEnd,
		PointD horizontalLineStart,
		PointD horizontalLineEnd,
		RectangleD pointMarker,
		ImageSurface imageThumbnail);
	private PointPickerVisualSettings CreateSettings ()
	{
		RectangleI drawBounds = GetDrawBounds ();
		PointD pos = PositionToClientPt (Position);
		Color black = new (0, 0, 0);
		Color lightGray = new (.75, .75, .75);
		return new (
			outerFrameColor: lightGray,
			innerFrameColor: black,
			lineColor: black,
			pointMarkerColor: black,
			outerFrame: new (drawBounds.X + 1, drawBounds.Y + 1, drawBounds.Width - 1, drawBounds.Height - 1),
			innerFrame: new (drawBounds.X + 2, drawBounds.Y + 2, drawBounds.Width - 3, drawBounds.Height - 3),
			verticalLineStart: new (pos.X + 1, drawBounds.Top + 2),
			verticalLineEnd: new (pos.X + 1, drawBounds.Bottom - 2),
			horizontalLineStart: new (drawBounds.Left + 2, pos.Y + 1),
			horizontalLineEnd: new (drawBounds.Right - 2, pos.Y + 1),
			pointMarker: new (pos.X - 1, pos.Y - 1, 3, 3),
			imageThumbnail: thumbnail!
		);
	}

	private void Draw (Context g)
	{
		if (thumbnail == null)
			UpdateThumbnail ();

		PointPickerVisualSettings settings = CreateSettings ();

		// Background
		g.SetSourceSurface (settings.imageThumbnail, 0.0, 0.0);
		g.Paint ();

		g.DrawRectangle (settings.outerFrame, settings.outerFrameColor, 1);
		g.DrawRectangle (settings.innerFrame, settings.innerFrameColor, 1);

		// Cursor
		g.DrawLine (settings.verticalLineStart, settings.verticalLineEnd, settings.lineColor, 1);
		g.DrawLine (settings.horizontalLineStart, settings.horizontalLineEnd, settings.lineColor, 1);

		// Point
		g.DrawEllipse (settings.pointMarker, settings.pointMarkerColor, 2);

		g.Dispose ();
	}

	private RectangleI GetDrawBounds ()
	{
		int width = GetAllocatedWidth ();
		int height = GetAllocatedHeight ();

		// Always be X pixels tall, but maintain aspect ratio
		Size imagesize = workspace.ImageSize;

		return new (
			0,
			0,
			Math.Min (width, imagesize.Width * height / imagesize.Height), // Adjusted width
			height);
	}

	public event EventHandler? PositionChanged;
	private void OnPositionChanged ()
	{
		PositionChanged?.Invoke (this, EventArgs.Empty);
	}

	private PointI MousePtToPosition (PointD clientMousePt)
	{
		RectangleI rect = GetDrawBounds ();

		int posX = (int) (clientMousePt.X * (workspace.ImageSize.Width / rect.Width));
		int posY = (int) (clientMousePt.Y * (workspace.ImageSize.Height / rect.Height));

		return new (posX, posY);
	}

	private PointD PositionToClientPt (PointI pos)
	{
		RectangleI rect = GetDrawBounds ();

		double halfWidth = workspace.ImageSize.Width / rect.Width;
		double halfHeight = workspace.ImageSize.Height / rect.Height;

		double ptX = pos.X / halfWidth;
		double ptY = pos.Y / halfHeight;

		return new (ptX, ptY);
	}
}
