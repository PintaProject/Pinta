// 
// PointPickerGraphic.cs
//  
// Author:
//       dufoli <${AuthorEmail}>
// 
// Copyright (c) 2010 dufoli
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
		private Cairo.PointD lastMouseXY;
		
		public PointPickerGraphic ()
		{
			Events = ((Gdk.EventMask)(16134));
			
			ButtonPressEvent += HandleHandleButtonPressEvent;
			ButtonReleaseEvent += HandleHandleButtonReleaseEvent;
			MotionNotifyEvent += HandleHandleMotionNotifyEvent;
		}

		#region Public Properties
		private Point position;
		
		public Point Position {
			get { return position; }
			set {
				if (position != value) {
					position = value;
					OnPositionChange ();
					this.GdkWindow.Invalidate ();
				}
			}
		}
		#endregion

		#region Mouse Handlers
		private void HandleHandleMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			lastMouseXY = new Cairo.PointD (args.Event.X, args.Event.Y);

			if (tracking) {
                Position = MousePtToPosition(lastMouseXY);
				GdkWindow.Invalidate ();
			}
		}

		private void HandleHandleButtonReleaseEvent (object o, Gtk.ButtonReleaseEventArgs args)
		{
			if (tracking )
            {
                if (args.Event.Button == 1)//left
                {
                    Position = MousePtToPosition(new Cairo.PointD(args.Event.X, args.Event.Y));
                }
                tracking = false;
            }
		}

		private void HandleHandleButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			if (args.Event.Button == 1)//Left
            {
                tracking = true;
                lastMouseXY = new Cairo.PointD(args.Event.X, args.Event.Y);
            }
		}
		#endregion

		#region Drawing Code
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			base.OnExposeEvent (ev);

			using (Cairo.Context g = CairoHelper.Create (GdkWindow)) {
				Cairo.Rectangle ourRect = Rectangle.Inflate (GdkWindow.GetBounds (), -1, -1).ToCairoRectangle ();
				g.DrawRectangle(outrect, new Cairo.Color (0, 0, 0), 1);
				//TODO Drawn position point
			}
			
			return true;
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			// Calculate desired size here.
			requisition.Height = 50;
			requisition.Width = 50;
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
		private Point MousePtToPosition(Cairo.PointD clientMousePt)
        {
			Rectangle ClientRectangle = GdkWindow.GetBounds ();
            double centerX = ClientRectangle.Left + ((ClientRectangle.Right - ClientRectangle.Left) / 2.0d);
            double centerY = ClientRectangle.Top + ((ClientRectangle.Bottom - ClientRectangle.Top) / 2.0d);

            double deltaX = clientMousePt.X - centerX;
            double deltaY = clientMousePt.Y - centerY;

            int posX = (int) (deltaX * (PintaCore.Workspace.ImageSize.X / ClientRectangle.Width));
            int posY = (int) (deltaY * (PintaCore.Workspace.ImageSize.Y / ClientRectangle.Height));

            return new Point(posX, posY);
        }

        private Cairo.PointD PositionToClientPt(Point pos)
        {
			Rectangle ClientRectangle = GdkWindow.GetBounds ();
            double centerX = ClientRectangle.Left + ((ClientRectangle.Right - ClientRectangle.Left) / 2.0d);
            double centerY = ClientRectangle.Top + ((ClientRectangle.Bottom - ClientRectangle.Top) / 2.0d);

            double halfWidth = PintaCore.Workspace.ImageSize.X / ClientRectangle.Width;
            double halfHeight = PintaCore.Workspace.ImageSize.Y / ClientRectangle.Height;

            double ptX = centerX + pos.X / halfWidth;
            double ptY = centerY + pos.Y / halfHeight;

            return new Cairo.PointD(ptX, ptY);
        }
		#endregion
	}
}
