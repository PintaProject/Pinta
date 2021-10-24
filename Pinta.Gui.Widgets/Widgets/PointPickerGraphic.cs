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
using Gdk;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class PointPickerGraphic : Gtk.DrawingArea
	{
		private bool tracking = false;
		private ImageSurface? thumbnail;
		private Gdk.Point position;

		public PointPickerGraphic ()
		{
			Events = ((EventMask) (16134));

			ButtonPressEvent += HandleHandleButtonPressEvent;
			ButtonReleaseEvent += HandleHandleButtonReleaseEvent;
			MotionNotifyEvent += HandleHandleMotionNotifyEvent;
		}

		private void UpdateThumbnail ()
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			var scalex = (double) Allocation.Width / (double) PintaCore.Workspace.ImageSize.Width;
			var scaley = (double) Allocation.Height / (double) PintaCore.Workspace.ImageSize.Height;

			thumbnail = CairoExtensions.CreateImageSurface (Format.Argb32, Allocation.Width, Allocation.Height);

			using (var g = new Context (thumbnail)) {
				g.Scale (scalex, scaley);

				foreach (var layer in doc.Layers.GetLayersToPaint ())
					layer.Draw (g);
			}
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			UpdateThumbnail ();
		}

		public void Init (Gdk.Point position)
		{
			this.position = position;
		}

		public Gdk.Point Position {
			get => position;
			set {
				if (position != value) {
					position = value;
					OnPositionChange ();
					Window.Invalidate ();
				}
			}
		}

		private void HandleHandleMotionNotifyEvent (object o, MotionNotifyEventArgs args)
		{
			if (tracking)
				Position = MousePtToPosition (new PointD (args.Event.X, args.Event.Y));
		}

		private void HandleHandleButtonReleaseEvent (object o, ButtonReleaseEventArgs args)
		{
			if (tracking) {
				// Left mouse button
				if (args.Event.Button == 1) 
					Position = MousePtToPosition (new PointD (args.Event.X, args.Event.Y));

				tracking = false;
			}
		}

		private void HandleHandleButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			// Left mouse button
			if (args.Event.Button == 1)
				tracking = true;
		}

		protected override bool OnDrawn (Context g)
		{
			base.OnDrawn (g);

			if (thumbnail == null)
				UpdateThumbnail ();

			var rect = Window.GetBounds ();
			var pos = PositionToClientPt (Position);
			var black = new Cairo.Color (0, 0, 0);

			// Background
			g.SetSource (thumbnail, 0.0, 0.0);
			g.Paint ();

			g.DrawRectangle (new Cairo.Rectangle (rect.X + 1, rect.Y + 1, rect.Width - 1, rect.Height - 1), new Cairo.Color (.75, .75, .75), 1);
			g.DrawRectangle (new Cairo.Rectangle (rect.X + 2, rect.Y + 2, rect.Width - 3, rect.Height - 3), black, 1);

			// Cursor
			g.DrawLine (new PointD (pos.X + 1, rect.Top + 2), new PointD (pos.X + 1, rect.Bottom - 2), black, 1);
			g.DrawLine (new PointD (rect.Left + 2, pos.Y + 1), new PointD (rect.Right - 2, pos.Y + 1), black, 1);

			// Point
			g.DrawEllipse (new Cairo.Rectangle (pos.X - 1, pos.Y - 1, 3, 3), black, 2);

			return true;
		}

		protected override void OnGetPreferredHeight (out int minimum_height, out int natural_height)
		{
			minimum_height = natural_height = 65;
			thumbnail = null;
		}

		protected override void OnGetPreferredWidthForHeight (int height, out int minimum_width, out int natural_width)
		{
			// Always be X pixels tall, but maintain aspect ratio
			var imagesize = PintaCore.Workspace.ImageSize;
			minimum_width = natural_width = (imagesize.Width * height) / imagesize.Height;
			thumbnail = null;
		}

		protected override SizeRequestMode OnGetRequestMode ()
		{
			return SizeRequestMode.WidthForHeight;
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
		private Gdk.Point MousePtToPosition (Cairo.PointD clientMousePt)
		{
			int posX = (int) (clientMousePt.X * (PintaCore.Workspace.ImageSize.Width / Allocation.Width));
			int posY = (int) (clientMousePt.Y * (PintaCore.Workspace.ImageSize.Height / Allocation.Height));

			return new Gdk.Point (posX, posY);
		}

		private Cairo.PointD PositionToClientPt (Gdk.Point pos)
		{
			double halfWidth = PintaCore.Workspace.ImageSize.Width / Allocation.Width;
			double halfHeight = PintaCore.Workspace.ImageSize.Height / Allocation.Height;

			double ptX = pos.X / halfWidth;
			double ptY = pos.Y / halfHeight;

			return new Cairo.PointD (ptX, ptY);
		}
		#endregion
	}
}
