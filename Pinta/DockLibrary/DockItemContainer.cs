//
// DockItemContainer.cs
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
using Mono.Unix;

namespace MonoDevelop.Components.Docking
{
	class DockItemContainer: VBox
	{
		static Gdk.Pixbuf pixClose;
		static Gdk.Pixbuf pixAutoHide;
		static Gdk.Pixbuf pixDock;
		
		Gtk.Label title;
		Gtk.Button btnClose;
		Gtk.Button btnDock;
		string txt;
		Gtk.EventBox header;
		Gtk.Alignment headerAlign;
		DockFrame frame;
		DockItem item;
		Widget widget;
		Container borderFrame;
		bool allowPlaceholderDocking;
		bool pointerHover;
		Box contentBox;
		
		static Gdk.Cursor fleurCursor = new Gdk.Cursor (Gdk.CursorType.Fleur);
		static Gdk.Cursor handCursor = new Gdk.Cursor (Gdk.CursorType.Hand2);
		
		static DockItemContainer ()
		{
			try {
				pixClose = Gdk.Pixbuf.LoadFromResource ("stock-close-12.png");
				pixAutoHide = Gdk.Pixbuf.LoadFromResource ("stock-auto-hide.png");
				pixDock = Gdk.Pixbuf.LoadFromResource ("stock-dock.png");
			} catch (Exception) {
			}
		}
		
		public DockItemContainer (DockFrame frame, DockItem item)
		{
			this.frame = frame;
			this.item = item;
			
			ResizeMode = Gtk.ResizeMode.Queue;
			Spacing = 0;
			
			title = new Gtk.Label ();
			title.Xalign = 0;
			title.Xpad = 3;
			title.UseMarkup = true;
			
			btnDock = new Button (new Gtk.Image (pixAutoHide));
			btnDock.Relief = ReliefStyle.None;
			btnDock.CanFocus = false;
			btnDock.WidthRequest = btnDock.HeightRequest = 17;
			btnDock.Clicked += OnClickDock;
			
			btnClose = new Button (new Gtk.Image (pixClose));
			btnClose.TooltipText = Catalog.GetString ("Hide");
			btnClose.Relief = ReliefStyle.None;
			btnClose.CanFocus = false;
			btnClose.WidthRequest = btnClose.HeightRequest = 17;
			btnClose.Clicked += delegate {
				item.Visible = false;
			};
			
			HBox box = new HBox (false, 0);
			box.PackStart (title, true, true, 0);
			box.PackEnd (btnClose, false, false, 0);
			box.PackEnd (btnDock, false, false, 0);
			
			headerAlign = new Alignment (0.0f, 0.0f, 1.0f, 1.0f);
			headerAlign.TopPadding = headerAlign.BottomPadding = headerAlign.RightPadding = headerAlign.LeftPadding = 1;
			headerAlign.Add (box);
			
			header = new EventBox ();
			header.Events |= Gdk.EventMask.KeyPressMask | Gdk.EventMask.KeyReleaseMask;
			header.ButtonPressEvent += HeaderButtonPress;
			header.ButtonReleaseEvent += HeaderButtonRelease;
			header.MotionNotifyEvent += HeaderMotion;
			header.KeyPressEvent += HeaderKeyPress;
			header.KeyReleaseEvent += HeaderKeyRelease;
			header.Add (headerAlign);
			header.ExposeEvent += HeaderExpose;
			header.Realized += delegate {
				header.GdkWindow.Cursor = handCursor;
			};
			
			foreach (Widget w in new Widget [] { header, btnDock, btnClose }) {
				w.EnterNotifyEvent += HeaderEnterNotify;
				w.LeaveNotifyEvent += HeaderLeaveNotify;
			}
			
			PackStart (header, false, false, 0);
			ShowAll ();
			
			PackStart (item.GetToolbar (PositionType.Top).Container, false, false, 0);
			
			HBox hbox = new HBox ();
			hbox.Show ();
			hbox.PackStart (item.GetToolbar (PositionType.Left).Container, false, false, 0);
			
			contentBox = new HBox ();
			contentBox.Show ();
			hbox.PackStart (contentBox, true, true, 0);
			
			hbox.PackStart (item.GetToolbar (PositionType.Right).Container, false, false, 0);
			
			PackStart (hbox, true, true, 0);
			
			PackStart (item.GetToolbar (PositionType.Bottom).Container, false, false, 0);
			
			UpdateBehavior ();
		}
		
		void OnClickDock (object s, EventArgs a)
		{
			if (item.Status == DockItemStatus.AutoHide || item.Status == DockItemStatus.Floating)
				item.Status = DockItemStatus.Dockable;
			else
				item.Status = DockItemStatus.AutoHide;
		}
		
		public void UpdateContent ()
		{
			if (widget != null)
				((Gtk.Container)widget.Parent).Remove (widget);
			widget = item.Content;
			
			if (item.DrawFrame) {
				if (borderFrame == null) {
					borderFrame = new CustomFrame (1, 1, 1, 1);
					borderFrame.Show ();
					contentBox.Add (borderFrame);
				}
				if (widget != null) {
					borderFrame.Add (widget);
					widget.Show ();
				}
			}
			else if (widget != null) {
				if (borderFrame != null) {
					contentBox.Remove (borderFrame);
					borderFrame = null;
				}
				contentBox.Add (widget);
				widget.Show ();
			}
		}
		
		public void UpdateBehavior ()
		{
			btnClose.Visible = (item.Behavior & DockItemBehavior.CantClose) == 0;
			header.Visible = (item.Behavior & DockItemBehavior.Locked) == 0;
			btnDock.Visible = (item.Behavior & DockItemBehavior.CantAutoHide) == 0;
			
			if (item.Status == DockItemStatus.AutoHide || item.Status == DockItemStatus.Floating) {
				btnDock.Image = new Gtk.Image (pixDock);
				btnDock.TooltipText = Catalog.GetString ("Dock");
			}
			else {
				btnDock.Image = new Gtk.Image (pixAutoHide);
				btnDock.TooltipText = Catalog.GetString ("Auto Hide");
			}
		}
		
		void HeaderButtonPress (object ob, Gtk.ButtonPressEventArgs args)
		{
			if (args.Event.Button == 1) {
				frame.ShowPlaceholder ();
				header.GdkWindow.Cursor = fleurCursor;
				frame.Toplevel.KeyPressEvent += HeaderKeyPress;
				frame.Toplevel.KeyReleaseEvent += HeaderKeyRelease;
				allowPlaceholderDocking = true;
			}
			else if (args.Event.Button == 3) {
				item.ShowDockPopupMenu (args.Event.Time);
			}
		}
		
		void HeaderButtonRelease (object ob, Gtk.ButtonReleaseEventArgs args)
		{
			if (args.Event.Button == 1) {
				frame.DockInPlaceholder (item);
				frame.HidePlaceholder ();
				if (header.GdkWindow != null)
					header.GdkWindow.Cursor = handCursor;
				frame.Toplevel.KeyPressEvent -= HeaderKeyPress;
				frame.Toplevel.KeyReleaseEvent -= HeaderKeyRelease;
			}
		}
		
		void HeaderMotion (object ob, Gtk.MotionNotifyEventArgs args)
		{
			frame.UpdatePlaceholder (item, Allocation.Size, allowPlaceholderDocking);
		}
		
		[GLib.ConnectBeforeAttribute]
		void HeaderKeyPress (object ob, Gtk.KeyPressEventArgs a)
		{
			if (a.Event.Key == Gdk.Key.Control_L || a.Event.Key == Gdk.Key.Control_R) {
				allowPlaceholderDocking = false;
				frame.UpdatePlaceholder (item, Allocation.Size, false);
			}
			if (a.Event.Key == Gdk.Key.Escape) {
				frame.HidePlaceholder ();
				frame.Toplevel.KeyPressEvent -= HeaderKeyPress;
				frame.Toplevel.KeyReleaseEvent -= HeaderKeyRelease;
				Gdk.Pointer.Ungrab (0);
			}
		}
				
		[GLib.ConnectBeforeAttribute]
		void HeaderKeyRelease (object ob, Gtk.KeyReleaseEventArgs a)
		{
			if (a.Event.Key == Gdk.Key.Control_L || a.Event.Key == Gdk.Key.Control_R) {
				allowPlaceholderDocking = true;
				frame.UpdatePlaceholder (item, Allocation.Size, true);
			}
		}
		
		private void HeaderExpose (object ob, Gtk.ExposeEventArgs a)
		{
			Gdk.Rectangle rect = new Gdk.Rectangle (0, 0, header.Allocation.Width - 1, header.Allocation.Height);
			HslColor gcol = frame.Style.Background (Gtk.StateType.Normal);
			
			if (pointerHover)
				gcol.L *= 1.05;
			gcol.L = Math.Min (1, gcol.L);
				
			using (Cairo.Context cr = Gdk.CairoHelper.Create (a.Event.Window)) {
				cr.NewPath ();
				cr.MoveTo (0, 0);
				cr.RelLineTo (rect.Width, 0);
				cr.RelLineTo (0, rect.Height);
				cr.RelLineTo (-rect.Width, 0);
				cr.RelLineTo (0, -rect.Height);
				cr.ClosePath ();
				Cairo.SolidPattern solidPattern = new Cairo.SolidPattern (gcol);
				cr.Pattern = solidPattern;
				cr.FillPreserve ();
				solidPattern.Destroy ();
			}
			
			header.GdkWindow.DrawRectangle (frame.Style.DarkGC (Gtk.StateType.Normal), false, rect);
			
			foreach (Widget child in header.Children)
				header.PropagateExpose (child, a.Event);
		}
		
		private void HeaderLeaveNotify (object ob, EventArgs a)
		{
			pointerHover = false;
			header.QueueDraw ();
		}
		
		private void HeaderEnterNotify (object ob, EventArgs a)
		{
			pointerHover = true;
			header.QueueDraw ();
		}
				
		public string Label {
			get { return txt; }
			set {
				title.Markup = "<small>" + value + "</small>";
				txt = value;
			}
		}
	}

	class CustomFrame: Bin
	{
		Gtk.Widget child;
		int topMargin;
		int bottomMargin;
		int leftMargin;
		int rightMargin;
		
		int topPadding;
		int bottomPadding;
		int leftPadding;
		int rightPadding;
		
		public CustomFrame ()
		{
		}
		
		public CustomFrame (int topMargin, int bottomMargin, int leftMargin, int rightMargin)
		{
			SetMargins (topMargin, bottomMargin, leftMargin, rightMargin);
		}
		
		public void SetMargins (int topMargin, int bottomMargin, int leftMargin, int rightMargin)
		{
			this.topMargin = topMargin;
			this.bottomMargin = bottomMargin;
			this.leftMargin = leftMargin;
			this.rightMargin = rightMargin;
		}
		
		public void SetPadding (int topPadding, int bottomPadding, int leftPadding, int rightPadding)
		{
			this.topPadding = topPadding;
			this.bottomPadding = bottomPadding;
			this.leftPadding = leftPadding;
			this.rightPadding = rightPadding;
		}
		
		public bool GradientBackround { get; set; }

		protected override void OnAdded (Widget widget)
		{
			base.OnAdded (widget);
			child = widget;
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition = child.SizeRequest ();
			requisition.Width += leftMargin + rightMargin + leftPadding + rightPadding;
			requisition.Height += topMargin + bottomMargin + topPadding + bottomPadding;
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (allocation.Width > leftMargin + rightMargin + leftPadding + rightPadding) {
				allocation.X += leftMargin + leftPadding;
				allocation.Width -= leftMargin + rightMargin + leftPadding + rightPadding;
			}
			if (allocation.Height > topMargin + bottomMargin + topPadding + bottomPadding) {
				allocation.Y += topMargin + topPadding;
				allocation.Height -= topMargin + bottomMargin + topPadding + bottomPadding;
			}
			child.SizeAllocate (allocation);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			Gdk.Rectangle rect;
			
			if (GradientBackround) {
				rect = new Gdk.Rectangle (Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);
				HslColor gcol = Style.Background (Gtk.StateType.Normal);
				
				using (Cairo.Context cr = Gdk.CairoHelper.Create (GdkWindow)) {
					cr.NewPath ();
					cr.MoveTo (rect.X, rect.Y);
					cr.RelLineTo (rect.Width, 0);
					cr.RelLineTo (0, rect.Height);
					cr.RelLineTo (-rect.Width, 0);
					cr.RelLineTo (0, -rect.Height);
					cr.ClosePath ();
					Cairo.Gradient pat = new Cairo.LinearGradient (rect.X, rect.Y, rect.X, rect.Bottom);
					Cairo.Color color1 = gcol;
					pat.AddColorStop (0, color1);
					gcol.L -= 0.1;
					if (gcol.L < 0) gcol.L = 0;
					pat.AddColorStop (1, gcol);
					cr.Pattern = pat;
					cr.FillPreserve ();
				}
			}
			
			bool res = base.OnExposeEvent (evnt);
			
			Gdk.GC borderColor = Style.DarkGC (Gtk.StateType.Normal);
			
			rect = Allocation;
			for (int n=0; n<topMargin; n++)
				GdkWindow.DrawLine (borderColor, rect.X, rect.Y + n, rect.Right - 1, rect.Y + n);
			
			for (int n=0; n<bottomMargin; n++)
				GdkWindow.DrawLine (borderColor, rect.X, rect.Bottom - n - 1, rect.Right - 1, rect.Bottom - n - 1);
			
			for (int n=0; n<leftMargin; n++)
				GdkWindow.DrawLine (borderColor, rect.X + n, rect.Y, rect.X + n, rect.Bottom - 1);
			
			for (int n=0; n<rightMargin; n++)
				GdkWindow.DrawLine (borderColor, rect.Right - n - 1, rect.Y, rect.Right - n - 1, rect.Bottom - 1);
			
			return res;
		}
	}
}
