//
// DockItemTitleTab.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using Gtk; 

using System;
using System.Linq;
using MonoDevelop.Components;
using Mono.Unix;

namespace Pinta.Docking
{
	
	class DockItemTitleTab: Gtk.EventBox
	{
		bool active;
		Gtk.Widget page;
		Label labelWidget;
		int labelWidth;
		DockVisualStyle visualStyle;
		Gtk.Widget tabIcon;
		DockFrame frame;
		string label;
		ImageButton btnDock;
		ImageButton btnClose;
		DockItem item;
		bool allowPlaceholderDocking;
		bool mouseOver;

		static Gdk.Cursor fleurCursor = new Gdk.Cursor (Gdk.CursorType.Fleur);

		static Gdk.Pixbuf pixClose;
        static Gdk.Pixbuf pixAutoHide;
        static Gdk.Pixbuf pixDock;

        static double PixelScale = GtkWorkarounds.GetPixelScale ();

		const int TopPadding = 5;
		const int BottomPadding = 7;
		const int TopPaddingActive = 5;
		const int BottomPaddingActive = 7;
		const int LeftPadding = 11;
		const int RightPadding = 9;

		static DockItemTitleTab ()
		{
            pixClose = GdkExtensions.FromResource ("pad-close-9.png");
            pixAutoHide = GdkExtensions.FromResource ("pad-minimize-9.png");
            pixDock = GdkExtensions.FromResource ("pad-dock-9.png");
		}
		
		public DockItemTitleTab (DockItem item, DockFrame frame)
		{
			this.item = item;
			this.frame = frame;
			this.VisibleWindow = false;
			UpdateVisualStyle ();
			NoShowAll = true;


			Events |= Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask | Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask;
			KeyPressEvent += HeaderKeyPress;
			KeyReleaseEvent += HeaderKeyRelease;

			this.SubscribeLeaveEvent (OnLeave);
		}

		public DockVisualStyle VisualStyle {
			get { return visualStyle; }
			set {
				visualStyle = value;
				UpdateVisualStyle ();
				QueueDraw ();
			}
		}

		void UpdateVisualStyle ()
		{
			if (labelWidget != null && label != null) {
				if (visualStyle.UppercaseTitles.Value)
					labelWidget.Text = label.ToUpper ();
				else
					labelWidget.Text = label;
				labelWidget.UseMarkup = true;
				if (visualStyle.ExpandedTabs.Value)
					labelWidget.Xalign = 0.5f;

				if (!(Parent is TabStrip.TabStripBox))
					labelWidget.Xalign = 0;
			}

			if (tabIcon != null)
				tabIcon.Visible = visualStyle.ShowPadTitleIcon.Value;
			if (IsRealized) {
#if false
				if (labelWidget != null)
					labelWidget.ModifyFg (StateType.Normal, visualStyle.PadTitleLabelColor.Value);
#endif
			}
			var r = WidthRequest;
			WidthRequest = -1;
			labelWidth = SizeRequest ().Width + 1;
			WidthRequest = r;

			if (visualStyle != null)
				HeightRequest = visualStyle.PadTitleHeight != null ? (int)(visualStyle.PadTitleHeight.Value * PixelScale) : -1;
		}

        public void SetLabel (Gtk.Widget page, Gdk.Pixbuf icon, string label)
		{
			this.label = label;
			this.page = page;
			if (Child != null) {
				Gtk.Widget oc = Child;
				Remove (oc);
				oc.Destroy ();
			}
			
			Gtk.HBox box = new HBox ();
			box.Spacing = 2;
			
			if (icon != null) {
				tabIcon = new ImageView (icon);
				tabIcon.Show ();
				box.PackStart (tabIcon, false, false, 0);
			} else
				tabIcon = null;

			if (!string.IsNullOrEmpty (label)) {
				labelWidget = new Label (label);
				//labelWidget.DropShadowVisible = true;
				labelWidget.UseMarkup = true;
				box.PackStart (labelWidget, true, true, 0);
			} else {
				labelWidget = null;
			}

			btnDock = new ImageButton ();
			btnDock.Image = pixAutoHide;
            btnDock.InactiveImage = pixAutoHide.WithAlpha (.5);
            btnDock.TooltipText = Catalog.GetString ("Auto Hide");
			btnDock.CanFocus = false;
//			btnDock.WidthRequest = btnDock.HeightRequest = 17;
			btnDock.Clicked += OnClickDock;
			btnDock.ButtonPressEvent += (o, args) => args.RetVal = true;
			btnDock.WidthRequest = btnDock.SizeRequest ().Width;

			btnClose = new ImageButton ();
			btnClose.Image = pixClose;
            btnClose.InactiveImage = pixClose.WithAlpha (.5);
			btnClose.TooltipText = Catalog.GetString ("Close");
			btnClose.CanFocus = false;
//			btnClose.WidthRequest = btnClose.HeightRequest = 17;
			btnClose.WidthRequest = btnDock.SizeRequest ().Width;
			btnClose.Clicked += delegate {
				item.Visible = false;
			};
			btnClose.ButtonPressEvent += (o, args) => args.RetVal = true;

			Gtk.Alignment al = new Alignment (0, 0, 1, 1);
			HBox btnBox = new HBox (false, 3);
			btnBox.PackStart (btnDock, false, false, 0);
			btnBox.PackStart (btnClose, false, false, 0);
			al.Add (btnBox);
			al.LeftPadding = 3;
			al.TopPadding = 1;
			box.PackEnd (al, false, false, 0);

			Add (box);
			
			// Get the required size before setting the ellipsize property, since ellipsized labels
			// have a width request of 0
			box.ShowAll ();
			Show ();

			UpdateBehavior ();
			UpdateVisualStyle ();
		}
		
		void OnClickDock (object s, EventArgs a)
		{
			if (item.Status == DockItemStatus.AutoHide || item.Status == DockItemStatus.Floating)
				item.Status = DockItemStatus.Dockable;
			else
				item.Status = DockItemStatus.AutoHide;
		}

		public int LabelWidth {
			get { return labelWidth; }
		}
		
		public bool Active {
			get {
				return active;
			}
			set {
				if (active != value) {
					active = value;
					this.QueueResize ();
					QueueDraw ();
					UpdateBehavior ();
				}
			}
		}

		public Widget Page {
			get {
				return page;
			}
		}
		
		public void UpdateBehavior ()
		{
			if (btnClose == null)
				return;

			btnClose.Visible = (item.Behavior & DockItemBehavior.CantClose) == 0;
			btnDock.Visible = (item.Behavior & DockItemBehavior.CantAutoHide) == 0;
			
			if (active || mouseOver) {
				if (btnClose.Image == null)
					btnClose.Image = pixClose;
				if (item.Status == DockItemStatus.AutoHide || item.Status == DockItemStatus.Floating) {
					btnDock.Image = pixDock;
                    btnDock.InactiveImage = pixDock.WithAlpha (.5);
                    btnDock.TooltipText = Catalog.GetString ("Dock");
				} else {
					btnDock.Image = pixAutoHide;
                    btnDock.InactiveImage = pixAutoHide.WithAlpha (.5);
                    btnDock.TooltipText = Catalog.GetString ("Auto Hide");
				}
			} else {
				btnDock.Image = null;
				btnClose.Image = null;
			}
		}

		bool tabPressed, tabActivated;
		double pressX, pressY;

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.TriggersContextMenu ()) {
				item.ShowDockPopupMenu (evnt.Time);
				return false;
			} else if (evnt.Button == 1) {
				if (evnt.Type == Gdk.EventType.ButtonPress) {
					tabPressed = true;
					pressX = evnt.X;
					pressY = evnt.Y;
				} else if (evnt.Type == Gdk.EventType.TwoButtonPress) {
					tabActivated = true;
				}
			}
			return base.OnButtonPressEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			if (tabActivated) {
				tabActivated = false;
				if (!item.Behavior.HasFlag (DockItemBehavior.CantAutoHide)) {
					if (item.Status == DockItemStatus.AutoHide)
						item.Status = DockItemStatus.Dockable;
					else
						item.Status = DockItemStatus.AutoHide;
				}
			}
			else if (!evnt.TriggersContextMenu () && evnt.Button == 1) {
				frame.DockInPlaceholder (item);
				frame.HidePlaceholder ();
				if (GdkWindow != null)
					GdkWindow.Cursor = null;
				frame.Toplevel.KeyPressEvent -= HeaderKeyPress;
				frame.Toplevel.KeyReleaseEvent -= HeaderKeyRelease;
			}
			tabPressed = false;
			return base.OnButtonReleaseEvent (evnt);
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			if (tabPressed && !item.Behavior.HasFlag (DockItemBehavior.NoGrip) && Math.Abs (evnt.X - pressX) > 3 && Math.Abs (evnt.Y - pressY) > 3) {
				frame.ShowPlaceholder (item);
				GdkWindow.Cursor = fleurCursor;
				frame.Toplevel.KeyPressEvent += HeaderKeyPress;
				frame.Toplevel.KeyReleaseEvent += HeaderKeyRelease;
				allowPlaceholderDocking = true;
				tabPressed = false;
			}
			frame.UpdatePlaceholder (item, Allocation.Size, allowPlaceholderDocking);
			return base.OnMotionNotifyEvent (evnt);
		}

		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			mouseOver = true;
			UpdateBehavior ();
			return base.OnEnterNotifyEvent (evnt);
		}

		void OnLeave ()
		{
			mouseOver = false;
			UpdateBehavior ();
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

		protected override void OnRealized ()
		{
			base.OnRealized ();
			UpdateVisualStyle ();
		}
		
		protected override void OnSizeRequested (ref Gtk.Requisition req)
		{
			if (Child != null) {
				req = Child.SizeRequest ();
				req.Width += LeftPadding + RightPadding;
				if (active)
					req.Height += TopPaddingActive + BottomPaddingActive;
				else
					req.Height += TopPadding + BottomPadding;
			}
		}
					
		protected override void OnSizeAllocated (Gdk.Rectangle rect)
		{
			base.OnSizeAllocated (rect);

			int leftPadding = LeftPadding;
			int rightPadding = RightPadding;
			if (rect.Width < labelWidth) {
				int red = (labelWidth - rect.Width) / 2;
				leftPadding -= red;
				rightPadding -= red;
				if (leftPadding < 2) leftPadding = 2;
				if (rightPadding < 2) rightPadding = 2;
			}
			
			rect.X += leftPadding;
			rect.Width -= leftPadding + rightPadding;

			if (Child != null) {
				if (active) {
					rect.Y += TopPaddingActive;
					rect.Height = Child.SizeRequest ().Height;
				}
				else {
					rect.Y += TopPadding;
					rect.Height = Child.SizeRequest ().Height;
				}
				Child.SizeAllocate (rect);
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (VisualStyle.TabStyle == DockTabStyle.Normal)
				DrawAsBrowser (evnt);
			else
				DrawNormal (evnt);
			return base.OnExposeEvent (evnt);
		}

		void DrawAsBrowser (Gdk.EventExpose evnt)
		{
			var alloc = Allocation;

			Gdk.GC bgc = new Gdk.GC (GdkWindow);
			var c = VisualStyle.PadBackgroundColor.Value.ToXwtColor ();
			c.Light *= 0.7;
			bgc.RgbFgColor = c.ToGdkColor ();
			bool first = true;
			bool last = true;
			TabStrip tabStrip = null;
			if (Parent is TabStrip.TabStripBox) {
				var tsb = (TabStrip.TabStripBox) Parent;
				var cts = tsb.Children;
				first = cts[0] == this;
				last = cts[cts.Length - 1] == this;
				tabStrip = tsb.TabStrip;
			}

			if (Active || (first && last)) {
				Gdk.GC gc = new Gdk.GC (GdkWindow);
				gc.RgbFgColor = VisualStyle.PadBackgroundColor.Value;
				evnt.Window.DrawRectangle (gc, true, alloc);
				if (!first)
					evnt.Window.DrawLine (bgc, alloc.X, alloc.Y, alloc.X, alloc.Y + alloc.Height - 1);
				if (!(last && first) && !(tabStrip != null && tabStrip.VisualStyle.ExpandedTabs.Value && last))
					evnt.Window.DrawLine (bgc, alloc.X + alloc.Width - 1, alloc.Y, alloc.X + alloc.Width - 1, alloc.Y + alloc.Height - 1);
				gc.Dispose ();

			} else {
				Gdk.GC gc = new Gdk.GC (GdkWindow);
				gc.RgbFgColor = tabStrip != null ? tabStrip.VisualStyle.InactivePadBackgroundColor.Value : frame.DefaultVisualStyle.InactivePadBackgroundColor.Value;
				evnt.Window.DrawRectangle (gc, true, alloc);
				gc.Dispose ();
				evnt.Window.DrawLine (bgc, alloc.X, alloc.Y + alloc.Height - 1, alloc.X + alloc.Width - 1, alloc.Y + alloc.Height - 1);
			}
			bgc.Dispose ();
		}

		void DrawNormal (Gdk.EventExpose evnt)
		{
			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				var x = Allocation.X;
				var y = Allocation.Y;

				ctx.Rectangle (x, y + 1, Allocation.Width, Allocation.Height - 1);
				using (var g = new Cairo.LinearGradient (x, y + 1, x, y + Allocation.Height - 1)) {
					g.AddColorStop (0, Styles.DockTabBarGradientStart);
					g.AddColorStop (1, Styles.DockTabBarGradientEnd);
					ctx.SetSource (g);
					ctx.Fill ();
				}

				ctx.MoveTo (x + 0.5, y + 0.5);
				ctx.LineTo (x + Allocation.Width - 0.5d, y + 0.5);
				ctx.SetSourceColor (Styles.DockTabBarGradientTop);
				ctx.Stroke ();

				if (active) {

					ctx.Rectangle (x, y + 1, Allocation.Width, Allocation.Height - 1);
					using (var g = new Cairo.LinearGradient (x, y + 1, x, y + Allocation.Height - 1)) {
						g.AddColorStop (0, new Cairo.Color (0, 0, 0, 0.01));
						g.AddColorStop (0.5, new Cairo.Color (0, 0, 0, 0.08));
						g.AddColorStop (1, new Cairo.Color (0, 0, 0, 0.01));
						ctx.SetSource (g);
						ctx.Fill ();
					}

/*					double offset = Allocation.Height * 0.25;
					var rect = new Cairo.Rectangle (x - Allocation.Height + offset, y, Allocation.Height, Allocation.Height);
					var cg = new Cairo.RadialGradient (rect.X + rect.Width / 2, rect.Y + rect.Height / 2, 0, rect.X, rect.Y + rect.Height / 2, rect.Height / 2);
					cg.AddColorStop (0, Styles.DockTabBarShadowGradientStart);
					cg.AddColorStop (1, Styles.DockTabBarShadowGradientEnd);
					ctx.Pattern = cg;
					ctx.Rectangle (rect);
					ctx.Fill ();

					rect = new Cairo.Rectangle (x + Allocation.Width - offset, y, Allocation.Height, Allocation.Height);
					cg = new Cairo.RadialGradient (rect.X + rect.Width / 2, rect.Y + rect.Height / 2, 0, rect.X, rect.Y + rect.Height / 2, rect.Height / 2);
					cg.AddColorStop (0, Styles.DockTabBarShadowGradientStart);
					cg.AddColorStop (1, Styles.DockTabBarShadowGradientEnd);
					ctx.Pattern = cg;
					ctx.Rectangle (rect);
					ctx.Fill ();*/
				}
			}
		}
	}
}
