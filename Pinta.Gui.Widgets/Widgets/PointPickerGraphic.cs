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

public class PointPickerGraphic : Gtk.DrawingArea
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
		var scalex = (double) bounds.Width / (double) PintaCore.Workspace.ImageSize.Width;
		var scaley = (double) bounds.Height / (double) PintaCore.Workspace.ImageSize.Height;

		thumbnail = CairoExtensions.CreateImageSurface (Format.Argb32, bounds.Width, bounds.Height);

		var g = new Context (thumbnail);
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

	private void Draw (Context g)
	{
		if (thumbnail == null)
			UpdateThumbnail ();

		var rect = GetDrawBounds ();
		var pos = PositionToClientPt (Position);

		var black = new Color (0, 0, 0);

		// Background
		g.SetSourceSurface (thumbnail!, 0.0, 0.0);
		g.Paint ();

		g.DrawRectangle (new RectangleD (rect.X + 1, rect.Y + 1, rect.Width - 1, rect.Height - 1), new Cairo.Color (.75, .75, .75), 1);
		g.DrawRectangle (new RectangleD (rect.X + 2, rect.Y + 2, rect.Width - 3, rect.Height - 3), black, 1);

		// Cursor
		g.DrawLine (new PointD (pos.X + 1, rect.Top + 2), new PointD (pos.X + 1, rect.Bottom - 2), black, 1);
		g.DrawLine (new PointD (rect.Left + 2, pos.Y + 1), new PointD (rect.Right - 2, pos.Y + 1), black, 1);

		// Point
		g.DrawEllipse (new RectangleD (pos.X - 1, pos.Y - 1, 3, 3), black, 2);
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

	protected virtual void OnPositionChange ()
	{
		if (PositionChanged != null) {
			PositionChanged (this, EventArgs.Empty);
		}
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
