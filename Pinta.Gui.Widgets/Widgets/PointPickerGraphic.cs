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

	public PointPickerGraphic ()
	{
		// TODO-GTK (improvement) - the allocated width should depend on the image aspect ratio. See the old GTK3 implementation
		HeightRequest = WidthRequest = 65;

		OnResize += (_, _) => UpdateThumbnail ();
		PositionChanged += (_, _) => QueueDraw ();

		SetDrawFunc ((area, context, width, height) => Draw (context));

		// Handle click + drag.
		var drag_gesture = Gtk.GestureDrag.New ();
		drag_gesture.SetButton (GtkExtensions.MouseLeftButton);

		drag_gesture.OnDragBegin += (_, args) => {
			drag_start = new PointD (args.StartX, args.StartY);
			Position = MousePtToPosition (drag_start);
			drag_gesture.SetState (Gtk.EventSequenceState.Claimed);
		};
		drag_gesture.OnDragUpdate += (_, args) => {
			var drag_offset = new PointD (args.OffsetX, args.OffsetY);
			Position = MousePtToPosition (drag_start + drag_offset);
		};
		drag_gesture.OnDragEnd += (_, args) => {
			var drag_offset = new PointD (args.OffsetX, args.OffsetY);
			Position = MousePtToPosition (drag_start + drag_offset);
		};

		AddController (drag_gesture);
	}

	private void UpdateThumbnail ()
	{
		var doc = PintaCore.Workspace.ActiveDocument;

		var bounds = GetDrawBounds ();
		var scalex = bounds.Width / (double) PintaCore.Workspace.ImageSize.Width;
		var scaley = bounds.Height / (double) PintaCore.Workspace.ImageSize.Height;

		thumbnail = CairoExtensions.CreateImageSurface (Format.Argb32, bounds.Width, bounds.Height);

		using Context g = new (thumbnail);
		g.Scale (scalex, scaley);

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
			if (position != value) {
				position = value;
				OnPositionChange ();
			}
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
		var rect = GetDrawBounds ();
		var pos = PositionToClientPt (Position);

		Color black = new (0, 0, 0);
		Color lightGray = new (.75, .75, .75);

		return new (
			outerFrameColor: lightGray,
			innerFrameColor: black,
			lineColor: black,
			pointMarkerColor: black,
			outerFrame: new (rect.X + 1, rect.Y + 1, rect.Width - 1, rect.Height - 1),
			innerFrame: new (rect.X + 2, rect.Y + 2, rect.Width - 3, rect.Height - 3),
			verticalLineStart: new (pos.X + 1, rect.Top + 2),
			verticalLineEnd: new (pos.X + 1, rect.Bottom - 2),
			horizontalLineStart: new (rect.Left + 2, pos.Y + 1),
			horizontalLineEnd: new (rect.Right - 2, pos.Y + 1),
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
		var imagesize = PintaCore.Workspace.ImageSize;
		width = Math.Min (width, (imagesize.Width * height) / imagesize.Height);

		return new RectangleI (0, 0, width, height);
	}

	#region Public Events
	public event EventHandler? PositionChanged;

	private void OnPositionChange ()
	{
		PositionChanged?.Invoke (this, EventArgs.Empty);
	}
	#endregion

	#region private methods
	private PointI MousePtToPosition (PointD clientMousePt)
	{
		var rect = GetDrawBounds ();
		int posX = (int) (clientMousePt.X * (PintaCore.Workspace.ImageSize.Width / rect.Width));
		int posY = (int) (clientMousePt.Y * (PintaCore.Workspace.ImageSize.Height / rect.Height));

		return new PointI (posX, posY);
	}

	private PointD PositionToClientPt (PointI pos)
	{
		var rect = GetDrawBounds ();
		double halfWidth = PintaCore.Workspace.ImageSize.Width / rect.Width;
		double halfHeight = PintaCore.Workspace.ImageSize.Height / rect.Height;

		double ptX = pos.X / halfWidth;
		double ptY = pos.Y / halfHeight;

		return new PointD (ptX, ptY);
	}
	#endregion
}
