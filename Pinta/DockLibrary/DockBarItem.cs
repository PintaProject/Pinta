//
// DockBarItem.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using Gtk;

namespace MonoDevelop.Components.Docking
{
	class DockBarItem: EventBox
	{
		DockBar bar;
		DockItem it;
		Box box;
		Label label;
		Alignment mainBox;
		AutoHideBox autoShowFrame;
		AutoHideBox hiddenFrame;
		uint autoShowTimeout = uint.MaxValue;
		uint autoHideTimeout = uint.MaxValue;
		int size;
		
		public DockBarItem (DockBar bar, DockItem it, int size)
		{
			Events = Events | Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask;
			this.size = size;
			this.bar = bar;
			this.it = it;
			VisibleWindow = false;
			UpdateTab ();
		}
		
		public void Close ()
		{
			UnscheduleAutoShow ();
			UnscheduleAutoHide ();
			AutoHide (false);
			bar.RemoveItem (this);
			Destroy ();
		}
		
		public int Size {
			get { return size; }
			set { size = value; }
		}
		
		public void UpdateTab ()
		{
			if (Child != null) {
				Widget w = Child;
				Remove (w);
				w.Destroy ();
			}
			
			mainBox = new Alignment (0,0,1,1);
			if (bar.Orientation == Gtk.Orientation.Horizontal) {
				box = new HBox ();
				mainBox.LeftPadding = mainBox.RightPadding = 2;
			}
			else {
				box = new VBox ();
				mainBox.TopPadding = mainBox.BottomPadding = 2;
			}
			
			Gtk.Widget customLabel = null;
			if (it.DockLabelProvider != null)
				customLabel = it.DockLabelProvider.CreateLabel (bar.Orientation);
			
			if (customLabel != null) {
				customLabel.ShowAll ();
				box.PackStart (customLabel, true, true, 0);
			}
			else {
				if (it.Icon != null)
					box.PackStart (new Gtk.Image (it.Icon), false, false, 0);
					
				if (!string.IsNullOrEmpty (it.Label)) {
					label = new Gtk.Label (it.Label);
					label.UseMarkup = true;
					if (bar.Orientation == Gtk.Orientation.Vertical)
						label.Angle = 270;
					box.PackStart (label, true, true, 0);
				} else
					label = null;
			}

			box.BorderWidth = 2;
			box.Spacing = 2;
			mainBox.Add (box);
			mainBox.ShowAll ();
			Add (mainBox);
			SetNormalColor ();
		}
		
		public MonoDevelop.Components.Docking.DockItem DockItem {
			get {
				return it;
			}
		}
		
		protected override void OnHidden ()
		{
			base.OnHidden ();
			UnscheduleAutoShow ();
			UnscheduleAutoHide ();
			AutoHide (false);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (State == StateType.Prelight) {
				int w = Allocation.Width, h = Allocation.Height;
				double x=Allocation.Left, y=Allocation.Top, r=3;
				x += 0.5; y += 0.5; h -=1; w -= 1;
				
				using (Cairo.Context ctx = Gdk.CairoHelper.Create (GdkWindow)) {
					HslColor c = new HslColor (Style.Background (Gtk.StateType.Normal));
					HslColor c1 = c;
					HslColor c2 = c;
					if (State != StateType.Prelight) {
						c1.L *= 0.8;
						c2.L *= 0.95;
					} else {
						c1.L *= 1.1;
						c2.L *= 1;
					}
					Cairo.Gradient pat;
					switch (bar.Position) {
						case PositionType.Top: pat = new Cairo.LinearGradient (x, y, x, y+h); break;
						case PositionType.Bottom: pat = new Cairo.LinearGradient (x, y, x, y+h); break;
						case PositionType.Left: pat = new Cairo.LinearGradient (x+w, y, x, y); break;
						default: pat = new Cairo.LinearGradient (x, y, x+w, y); break;
					}
					pat.AddColorStop (0, c1);
					pat.AddColorStop (1, c2);
					ctx.NewPath ();
					ctx.Arc (x+r, y+r, r, 180 * (Math.PI / 180), 270 * (Math.PI / 180));
					ctx.LineTo (x+w-r, y);
					ctx.Arc (x+w-r, y+r, r, 270 * (Math.PI / 180), 360 * (Math.PI / 180));
					ctx.LineTo (x+w, y+h);
					ctx.LineTo (x, y+h);
					ctx.ClosePath ();
					ctx.Pattern = pat;
					ctx.FillPreserve ();
					c1 = c;
					c1.L *= 0.7;
					ctx.LineWidth = 1;
					ctx.Color = c1;
					ctx.Stroke ();
					
					// Inner line
					ctx.NewPath ();
					ctx.Arc (x+r+1, y+r+1, r, 180 * (Math.PI / 180), 270 * (Math.PI / 180));
					ctx.LineTo (x+w-r-1, y+1);
					ctx.Arc (x+w-r-1, y+r+1, r, 270 * (Math.PI / 180), 360 * (Math.PI / 180));
					ctx.LineTo (x+w-1, y+h-1);
					ctx.LineTo (x+1, y+h-1);
					ctx.ClosePath ();
					c1 = c;
					//c1.L *= 0.9;
					ctx.LineWidth = 1;
					ctx.Color = c1;
					ctx.Stroke ();
				}
			}
		
			bool res = base.OnExposeEvent (evnt);
			return res;
		}

		public void Present (bool giveFocus)
		{
			AutoShow ();
			if (giveFocus) {
				GLib.Timeout.Add (200, delegate {
					// Using a small delay because AutoShow uses an animation and setting focus may
					// not work until the item is visible
					it.SetFocus ();
					ScheduleAutoHide (false);
					return false;
				});
			}
		}

		void AutoShow ()
		{
			UnscheduleAutoHide ();
			if (autoShowFrame == null) {
				if (hiddenFrame != null)
					bar.Frame.AutoHide (it, hiddenFrame, false);
				autoShowFrame = bar.Frame.AutoShow (it, bar, size);
				autoShowFrame.EnterNotifyEvent += OnFrameEnter;
				autoShowFrame.LeaveNotifyEvent += OnFrameLeave;
				autoShowFrame.KeyPressEvent += OnFrameKeyPress;
				SetPrelight ();
			}
		}
		
		void AutoHide (bool animate)
		{
			UnscheduleAutoShow ();
			if (autoShowFrame != null) {
				size = autoShowFrame.Size;
				hiddenFrame = autoShowFrame;
				autoShowFrame.Hidden += delegate {
					hiddenFrame = null;
				};
				bar.Frame.AutoHide (it, autoShowFrame, animate);
				autoShowFrame.EnterNotifyEvent -= OnFrameEnter;
				autoShowFrame.LeaveNotifyEvent -= OnFrameLeave;
				autoShowFrame.KeyPressEvent -= OnFrameKeyPress;
				autoShowFrame = null;
				UnsetPrelight ();
			}
		}
		
		void ScheduleAutoShow ()
		{
			UnscheduleAutoHide ();
			if (autoShowTimeout == uint.MaxValue) {
				autoShowTimeout = GLib.Timeout.Add (bar.Frame.AutoShowDelay, delegate {
					autoShowTimeout = uint.MaxValue;
					AutoShow ();
					return false;
				});
			}
		}
		
		void ScheduleAutoHide (bool cancelAutoShow)
		{
			ScheduleAutoHide (cancelAutoShow, false);
		}
		
		void ScheduleAutoHide (bool cancelAutoShow, bool force)
		{
			if (cancelAutoShow)
				UnscheduleAutoShow ();
			if (force)
				it.Widget.FocusChild = null;
			if (autoHideTimeout == uint.MaxValue) {
				autoHideTimeout = GLib.Timeout.Add (force ? 0 : bar.Frame.AutoHideDelay, delegate {
					// Don't hide the item if it has the focus. Try again later.
					if (it.Widget.FocusChild != null)
						return true;
					autoHideTimeout = uint.MaxValue;
					AutoHide (true);
					return false;
				});
			}
		}
		
		void UnscheduleAutoShow ()
		{
			if (autoShowTimeout != uint.MaxValue) {
				GLib.Source.Remove (autoShowTimeout);
				autoShowTimeout = uint.MaxValue;
			}
		}
		
		void UnscheduleAutoHide ()
		{
			if (autoHideTimeout != uint.MaxValue) {
				GLib.Source.Remove (autoHideTimeout);
				autoHideTimeout = uint.MaxValue;
			}
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			ScheduleAutoShow ();
			SetPrelight ();
			return base.OnEnterNotifyEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			ScheduleAutoHide (true);
			if (autoShowFrame == null)
				UnsetPrelight ();
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		void SetPrelight ()
		{
			if (State != StateType.Prelight) {
				State = StateType.Prelight;
				if (label != null)
					label.ModifyFg (StateType.Normal, Style.Foreground (Gtk.StateType.Normal));
			}
		}
		
		void UnsetPrelight ()
		{
			if (State == StateType.Prelight) {
				State = StateType.Normal;
				SetNormalColor ();
			}
		}
		
		protected override void OnRealized ()
		{
			base.OnRealized();
			SetNormalColor ();
		}

		
		void SetNormalColor ()
		{
			if (label != null) {
				HslColor c = Style.Background (Gtk.StateType.Normal);
				c.L *= 0.4;
				label.ModifyFg (StateType.Normal, c);
			}
		}
		
		void OnFrameEnter (object s, Gtk.EnterNotifyEventArgs args)
		{
			AutoShow ();
		}

		void OnFrameKeyPress (object s, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Escape)
				ScheduleAutoHide (true, true);
		}
		
		void OnFrameLeave (object s, Gtk.LeaveNotifyEventArgs args)
		{
			if (args.Event.Detail != Gdk.NotifyType.Inferior)
				ScheduleAutoHide (true);
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1) {
				if (evnt.Type == Gdk.EventType.TwoButtonPress)
					it.Status = DockItemStatus.Dockable;
				else
					AutoShow ();
			}
			else if (evnt.Button == 3)
				it.ShowDockPopupMenu (evnt.Time);
			return base.OnButtonPressEvent (evnt);
		}
	}
}
