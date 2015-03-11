//
// FixedPanel.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using Gtk;
using Gdk;
using Pinta.Docking;

namespace Pinta.Docking.DockToolbars
{
	public enum Placement
	{
		Top, Bottom, Left, Right
	}

	public class FixedPanel: Container
	{
		ArrayList widgets = new ArrayList ();
		Placement placement = Placement.Top;
		
		public FixedPanel ()
		{
			GtkWorkarounds.FixContainerLeak (this);
			
			WidgetFlags |= WidgetFlags.NoWindow;
		}
		
		public Placement Placement {
			get { return placement; }
			set { placement = value; }
		}
		
		public override GLib.GType ChildType ()
		{
			return Widget.GType;
		}
		
		public void Put (Widget w, int x, int y)
		{
			WidgetPosition wpos = new WidgetPosition ();
			wpos.X = x;
			wpos.Y = y;
			wpos.Widget = w;
			widgets.Add (wpos);
			w.Parent = this;
			QueueResize ();
		}
		
		public void Move (Widget w, int x, int y)
		{
			int n = GetWidgetPosition (w);
			if (n != -1) {
				WidgetPosition wpos = (WidgetPosition) widgets [n];
				if (wpos.X == x && wpos.Y == y) return; 
				wpos.X = x;
				wpos.Y = y;
				QueueResize ();
			}
		}
		
		public bool GetPosition (Widget w, out int x, out int y)
		{
			int n = GetWidgetPosition (w);
			if (n != -1) {
				WidgetPosition wpos = (WidgetPosition) widgets [n];
				x = wpos.X;
				y = wpos.Y;
				return true;
			}
			x = y = 0;
			return false;
		}
		
		public virtual Requisition GetChildRequisition (Widget w)
		{
			return w.ChildRequisition;
		}
		
		public int GetChildWidth (Widget w)
		{
	//		ResizeChildren ();
			if (placement == Placement.Top || placement == Placement.Bottom)
				return w.Allocation.Width;
			else
				return w.Allocation.Height;
		}
		
		public int GetChildHeight (Widget w)
		{
			if (placement == Placement.Top || placement == Placement.Bottom)
				return w.Allocation.Height;
			else
				return w.Allocation.Width;
		}
		
		public int PanelWidth {
			get {
				if (placement == Placement.Top || placement == Placement.Bottom)
					return Allocation.Width;
				else
					return Allocation.Height;
			}
		}
		
		public void WindowToPanel (int x, int y, int w, int h, out int rx, out int ry)
		{
			switch (placement) {
				case Placement.Top:
					rx = x - Allocation.X;
					ry = y - Allocation.Y;
					break;
				case Placement.Bottom:
					rx = x - Allocation.X;
					ry = Allocation.Bottom - y - h - 1;
					break;
				case Placement.Left:
					rx = y - Allocation.Y;
					ry = x - Allocation.X;
					break;
				default:
					rx = y - Allocation.Y;
					ry = Allocation.Right - x - w - 1;
					break;
			}
		}
		
		public void PanelToWindow (int x, int y, int w, int h, out int rx, out int ry, out int rw, out int rh)
		{
			switch (placement) {
				case Placement.Top:
					rx = x + Allocation.X;
					ry = y + Allocation.Y;
					rw = w;
					rh = h;
					break;
				case Placement.Bottom:
					rx = x + Allocation.X;
					ry = Allocation.Bottom - y - h - 1;
					rw = w;
					rh = h;
					break;
				case Placement.Left:
					rx = y + Allocation.X;
					ry = x + Allocation.Y;
					rw = h;
					rh = w;
					break;
				default:
					rx = Allocation.Right - y - h - 1;
					ry = x + Allocation.Y;
					rw = h;
					rh = w;
					break;
			}
		}
		
		protected override void OnAdded (Widget w)
		{
			Put (w, 0, 0);
		}
		
		protected override void OnRemoved (Widget w)
		{
			int i = GetWidgetPosition (w);
			if (i != -1) {
				widgets.RemoveAt (i);
				w.Unparent ();
				QueueResize ();
			}
		}
		
		int GetWidgetPosition (Widget w)
		{
			for (int n=0; n<widgets.Count; n++)
				if (((WidgetPosition)widgets[n]).Widget == w)
					return n;
			return -1;
		}
		
		protected override void OnSizeRequested (ref Requisition req)
		{
			req.Width = req.Height = 0;
			foreach (WidgetPosition pos in widgets) {
				Requisition wreq = pos.Widget.SizeRequest ();
				if (placement == Placement.Top || placement == Placement.Bottom) {
					if (pos.X + wreq.Width > req.Width)
						req.Width = pos.X + wreq.Width;
					if (pos.Y + wreq.Height > req.Height)
						req.Height = pos.Y + wreq.Height;
				} else {
					if (pos.Y + wreq.Width > req.Width)
						req.Width = pos.Y + wreq.Width;
					if (pos.X + wreq.Height > req.Height)
						req.Height = pos.X + wreq.Height;
				}
			}
			if (placement == Placement.Top || placement == Placement.Bottom)
				req.Width = 0;
			else
				req.Height = 0;
		}

		protected override void OnSizeAllocated (Gdk.Rectangle rect)
		{
			base.OnSizeAllocated (rect);
			foreach (WidgetPosition pos in widgets) {
				Requisition req = GetChildRequisition (pos.Widget);
				Rectangle crect = new Rectangle (pos.X, pos.Y, req.Width, req.Height);
				switch (placement) {
					case Placement.Top:
						break;
					case Placement.Bottom:
						crect.Y = Allocation.Height - crect.Y - crect.Height;
						break;
					case Placement.Left: {
						int t = crect.X; crect.X=crect.Y; crect.Y=t;
						break;
						}
					case Placement.Right: {
						int t = crect.X; crect.X=crect.Y; crect.Y=t;
						crect.X = Allocation.Width - crect.X - crect.Width;
						break;
						}
				}
				crect.X += Allocation.X;
				crect.Y += Allocation.Y;
				pos.Widget.SizeAllocate (crect);
			}
		}
		
		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			WidgetPosition[] positions = (WidgetPosition[]) widgets.ToArray (typeof (WidgetPosition));
			foreach (WidgetPosition pos in positions)
				callback (pos.Widget);
		}
	}

	class WidgetPosition
	{
		public int X;
		public int Y;
		public Widget Widget;
	}
}
