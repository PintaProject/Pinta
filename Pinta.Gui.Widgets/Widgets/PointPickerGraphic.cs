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
using Gdk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem(true)]
	public class PointPickerGraphic : Gtk.DrawingArea
	{
		private bool tracking = false;
		private Cairo.ImageSurface thumbnail;

		public PointPickerGraphic ()
		{
			Events = ((Gdk.EventMask)(16134));
			
			ButtonPressEvent += HandleHandleButtonPressEvent;
			ButtonReleaseEvent += HandleHandleButtonReleaseEvent;
			MotionNotifyEvent += HandleHandleMotionNotifyEvent;
		}

		private void UpdateThumbnail ()
		{
			double scalex = (double)Allocation.Width / (double)PintaCore.Workspace.ImageSize.Width;
			double scaley = (double)Allocation.Height / (double)PintaCore.Workspace.ImageSize.Height;
			
			thumbnail = new Cairo.ImageSurface (Cairo.Format.Argb32, Allocation.Width, Allocation.Height);
			
			using (Cairo.Context g = new Cairo.Context (thumbnail)) {
				g.Scale (scalex, scaley);
				foreach (Layer layer in PintaCore.Layers.GetLayersToPaint ()) {
					layer.Draw(g);
				}
			}
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			UpdateThumbnail ();
		}

		public void Init(Point position)
		{
			this.position = position;
		}

		#region Public Properties
		private Point position;

		public Point Position {
			get { return position; }
			set {
				if (position != value) {
					position = value;
					OnPositionChange ();
					GdkWindow.Invalidate ();
				}
			}
		}
		#endregion

		#region Mouse Handlers
		private void HandleHandleMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			if (tracking) {
				Position = MousePtToPosition (new Cairo.PointD (args.Event.X, args.Event.Y));
			}
		}

		private void HandleHandleButtonReleaseEvent (object o, Gtk.ButtonReleaseEventArgs args)
		{
			if (tracking) {
				//left
				if (args.Event.Button == 1) {
					Position = MousePtToPosition (new Cairo.PointD (args.Event.X, args.Event.Y));
				}
				tracking = false;
			}
		}

		private void HandleHandleButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			//Left
			if (args.Event.Button == 1) {
				tracking = true;
			}
		}
		#endregion

		#region Drawing Code
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			base.OnExposeEvent (ev);
			
			if (thumbnail == null)
				UpdateThumbnail ();
			
			Rectangle rect = GdkWindow.GetBounds ();
			Cairo.PointD pos = PositionToClientPt (Position);
			Cairo.Color black = new Cairo.Color (0, 0, 0);
			
			using (Cairo.Context g = CairoHelper.Create (GdkWindow)) {
				//background
				g.SetSource (thumbnail, 0.0, 0.0);
				g.Paint ();

				g.DrawRectangle (new Cairo.Rectangle (rect.X + 1, rect.Y + 1, rect.Width - 1, rect.Height - 1), new Cairo.Color (.75, .75, .75), 1);
				g.DrawRectangle (new Cairo.Rectangle (rect.X + 2, rect.Y + 2, rect.Width - 3, rect.Height - 3), black, 1);
				
				//cursor
				g.DrawLine (new Cairo.PointD (pos.X + 1, rect.Top + 2), new Cairo.PointD (pos.X + 1, rect.Bottom - 2), black, 1);
				g.DrawLine (new Cairo.PointD (rect.Left + 2, pos.Y + 1), new Cairo.PointD (rect.Right - 2, pos.Y + 1), black, 1);
				
				//point
				g.DrawEllipse (new Cairo.Rectangle (pos.X - 1, pos.Y - 1, 3, 3), black, 2);
			}
			return true;
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			// Always be X pixels tall, but maintain aspect ratio
			Size imagesize = PintaCore.Workspace.ImageSize;
			
			requisition.Height = 65;
			requisition.Width = (imagesize.Width * requisition.Height) / imagesize.Height;
			thumbnail = null;
		}
		#endregion

		#region Public Events
		public event EventHandler PositionChanged;

		protected virtual void OnPositionChange ()
		{
			if (PositionChanged != null) {
				PositionChanged (this, EventArgs.Empty);
			}
		}
		#endregion

		#region private methods
		private Point MousePtToPosition (Cairo.PointD clientMousePt)
		{
			int posX = (int)(clientMousePt.X * (PintaCore.Workspace.ImageSize.Width / Allocation.Width));
			int posY = (int)(clientMousePt.Y * (PintaCore.Workspace.ImageSize.Height / Allocation.Height));
			
			return new Point (posX, posY);
		}

		private Cairo.PointD PositionToClientPt (Point pos)
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
