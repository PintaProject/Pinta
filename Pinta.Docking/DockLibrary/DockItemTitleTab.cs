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
using System.Reflection;
using MonoDevelop.Components;
using Pinta.Docking.AtkCocoaHelper;
using Pinta.Core;
using Cairo;

namespace Pinta.Docking
{

	class DockItemTitleTab : Gtk.EventBox
	{
#if false
		static Xwt.Drawing.Image dockTabActiveBackImage = Xwt.Drawing.Image.FromResource("padbar-active.9.png");
		static Xwt.Drawing.Image dockTabBackImage = Xwt.Drawing.Image.FromResource("padbar-inactive.9.png");
#endif

		bool active;
		Gtk.Widget page;
		Label labelWidget;
		int labelWidth;
		int minWidth;
		DockVisualStyle visualStyle;
		ImageView tabIcon;
		Gtk.HBox box;
		DockFrame frame;
		string label;
		ImageButton btnDock;
		ImageButton btnClose;
		Gtk.Alignment al;
		DockItem item;
		bool allowPlaceholderDocking;
		bool mouseOver;
		Widget currentFocus = null; // Currently focused child

		IDisposable subscribedLeaveEvent;

		static Gdk.Cursor fleurCursor = new Gdk.Cursor (Gdk.CursorType.Fleur);

		static Gdk.Pixbuf pixClose;
		static Gdk.Pixbuf pixAutoHide;
		static Gdk.Pixbuf pixDock;

		const int TopPadding = 9;
		const int BottomPadding = 7;
		const int TopPaddingActive = 8;
		const int BottomPaddingActive = 8;
		const int LeftPadding = 8;
		const int RightPadding = 8;

		internal event EventHandler<EventArgs> TabPressed;

		static DockItemTitleTab ()
		{
			pixClose = GdkExtensions.FromResource("pad-close-9.png");
			pixAutoHide = GdkExtensions.FromResource("pad-minimize-9.png");
			pixDock = GdkExtensions.FromResource("pad-dock-9.png");

#if false
			Xwt.Drawing.NinePatchImage tabBackImage9;
			if (dockTabBackImage is Xwt.Drawing.ThemedImage) {
				var img = ((Xwt.Drawing.ThemedImage)dockTabBackImage).GetImage (Xwt.Drawing.Context.GlobalStyles);
				tabBackImage9 = img as Xwt.Drawing.NinePatchImage;
			} else
				tabBackImage9 = dockTabBackImage as Xwt.Drawing.NinePatchImage;
			TabPadding = tabBackImage9.Padding;


			Xwt.Drawing.NinePatchImage tabActiveBackImage9;
			if (dockTabActiveBackImage is Xwt.Drawing.ThemedImage) {
				var img = ((Xwt.Drawing.ThemedImage)dockTabActiveBackImage).GetImage (Xwt.Drawing.Context.GlobalStyles);
				tabActiveBackImage9 = img as Xwt.Drawing.NinePatchImage;
			} else
				tabActiveBackImage9 = dockTabActiveBackImage as Xwt.Drawing.NinePatchImage;
			TabActivePadding = tabActiveBackImage9.Padding;
#endif
		}

		public DockItemTitleTab (DockItem item, DockFrame frame)
		{
			var actionHandler = new ActionDelegate (this);
			actionHandler.PerformPress += HandlePress;
			actionHandler.PerformShowMenu += HandleShowMenu;

			UpdateRole (false, null);

			CanFocus = true;
			this.item = item;
			this.frame = frame;
			this.VisibleWindow = false;
			UpdateVisualStyle ();
			NoShowAll = true;


			Events |= Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask | Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask;
			KeyPressEvent += HeaderKeyPress;
			KeyReleaseEvent += HeaderKeyRelease;

			subscribedLeaveEvent = this.SubscribeLeaveEvent (OnLeave);
		}

		internal void UpdateRole (bool isTab, TabStrip strip)
		{
			Atk.Object fromAccessible = null, toAccessible = null;

			if (!isTab) {
				Accessible.SetRole (AtkCocoa.Roles.AXGroup, "pad header");
				Accessible.SetSubRole ("XAPadHeader");

				// Take the button accessibles back from the strip
				if (strip != null) {
					fromAccessible = strip.Accessible;
					toAccessible = Accessible;
				}
			} else {
				Accessible.SetRole (AtkCocoa.Roles.AXRadioButton, "tab");
				Accessible.SetSubRole ("");

				// Give the button accessibles to the strip
				if (strip != null) {
					fromAccessible = Accessible;
					toAccessible = strip.Accessible;
				}
			}

			if (fromAccessible != null && toAccessible != null) {
				fromAccessible.TransferAccessibleChild (toAccessible, btnDock.Accessible);
				fromAccessible.TransferAccessibleChild (toAccessible, btnClose.Accessible);
			}
		}

		public DockVisualStyle VisualStyle {
			get { return visualStyle; }
			set {
				visualStyle = value;
				UpdateVisualStyle ();
				QueueDraw ();
			}
		}

		protected override void OnDestroyed ()
		{
			subscribedLeaveEvent.Dispose ();
			base.OnDestroyed ();
		}

		void UpdateVisualStyle ()
		{
			double inactiveIconAlpha;

#if false
			if (IdeApp.Preferences == null || IdeApp.Preferences.UserInterfaceTheme == Theme.Light)
				inactiveIconAlpha = 0.8;
			else
				inactiveIconAlpha = 0.6;
#else
			inactiveIconAlpha = 0.8;
#endif

			if (labelWidget?.Visible == true && label != null) {
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

			if (tabIcon != null) {
				tabIcon.Image = tabIcon.Image.WithAlpha (active ? 1.0 : inactiveIconAlpha);
				tabIcon.Visible = visualStyle.ShowPadTitleIcon.Value;
			}

			// TODO-GTK3
#if false
			if (IsRealized && labelWidget?.Visible == true) {
				var font = IdeServices.FontService.SansFont.CopyModified (null, Pango.Weight.Bold);
				font.AbsoluteSize = Pango.Units.FromPixels (11);
				labelWidget.ModifyFont (font);
				labelWidget.ModifyText (StateType.Normal, (active ? visualStyle.PadTitleLabelColor.Value : visualStyle.InactivePadTitleLabelColor.Value).ToGdkColor ());
			}
#endif

			var r = WidthRequest;
			WidthRequest = -1;
			labelWidth = SizeRequest ().Width + 1;
			WidthRequest = r;

			if (visualStyle != null)
				HeightRequest = visualStyle.PadTitleHeight != null ? (int)(visualStyle.PadTitleHeight.Value) : -1;
		}

		public void SetLabel (Gtk.Widget page, Gdk.Pixbuf icon, string label)
		{
			string labelNoSpaces = label != null ? label.Replace (' ', '-') : null;
			this.label = label;
			this.page = page;

#if false
			if (icon == null)
				icon = ImageService.GetIcon ("md-empty");
#else
			if (icon == null)
				icon = GdkExtensions.FromResource("empty-16.png");
#endif

			if (box == null) {
				box = new HBox ();
				box.Accessible.SetShouldIgnore (true);
				box.Spacing = -2;

				tabIcon = new ImageView ();
				tabIcon.Accessible.SetShouldIgnore (true);
				tabIcon.Show ();
				box.PackStart (tabIcon, false, false, 3);

				labelWidget = new Label (label);
				// Ignore the label because the title tab already contains its name
				labelWidget.Accessible.SetShouldIgnore (true);
				labelWidget.UseMarkup = true;
				var alignLabel = new Alignment (0.0f, 0.5f, 1, 1);
				alignLabel.Accessible.SetShouldIgnore (true);
				alignLabel.BottomPadding = 0;
				alignLabel.RightPadding = 15;
				alignLabel.Add (labelWidget);
				box.PackStart (alignLabel, false, false, 0);

				btnDock = new ImageButton ();
				btnDock.Image = pixAutoHide;
				btnDock.TooltipText = Translations.GetString ("Auto Hide");
				btnDock.CanFocus = true;
				//			btnDock.WidthRequest = btnDock.HeightRequest = 17;
				btnDock.Clicked += OnClickDock;
				btnDock.ButtonPressEvent += (o, args) => args.RetVal = true;
				btnDock.WidthRequest = btnDock.SizeRequest ().Width;
				UpdateDockButtonAccessibilityLabels ();

				btnClose = new ImageButton ();
				btnClose.Image = pixClose;
				btnClose.TooltipText = Translations.GetString ("Close");
				btnClose.CanFocus = true;
				//			btnClose.WidthRequest = btnClose.HeightRequest = 17;
				btnClose.WidthRequest = btnDock.SizeRequest ().Width;
				btnClose.Clicked += delegate {
					item.Visible = false;
				};
				btnClose.ButtonPressEvent += (o, args) => args.RetVal = true;

				al = new Alignment (0, 0.5f, 1, 1);
				al.Accessible.SetShouldIgnore (true);
				HBox btnBox = new HBox (false, 0);
				btnBox.Accessible.SetShouldIgnore (true);
				btnBox.PackStart (btnDock, false, false, 3);
				btnBox.PackStart (btnClose, false, false, 1);
				al.Add (btnBox);
				box.PackEnd (al, false, false, 3);

				Add (box);
			}

			tabIcon.Image = icon;

			string realLabel, realHelp;
			if (!string.IsNullOrEmpty (label)) {
				labelWidget.Parent.Show ();
				labelWidget.Name = label;
				btnDock.Name = string.Format ("btnDock_{0}", labelNoSpaces ?? string.Empty);
				btnClose.Name = string.Format ("btnClose_{0}", labelNoSpaces ?? string.Empty);
				realLabel = Translations.GetString ("Close {0}", label);
				realHelp = Translations.GetString ("Close the {0} pad", label);
			} else {
				labelWidget.Parent.Hide ();
				realLabel = Translations.GetString ("Close pad");
				realHelp = Translations.GetString ("Close the pad");
			}

			btnClose.Accessible.SetLabel (realLabel);
			btnClose.Accessible.Description = realHelp;

			if (label != null) {
				Accessible.Name = $"DockTab.{labelNoSpaces}";
				Accessible.Description = Translations.GetString ("Switch to the {0} tab", label);
				Accessible.SetTitle (label);
				Accessible.SetLabel (label);
			}

			// Get the required size before setting the ellipsize property, since ellipsized labels
			// have a width request of 0
			box.ShowAll ();
			Show ();

			minWidth = tabIcon.SizeRequest ().Width + al.SizeRequest ().Width + 10;

			UpdateBehavior ();
			UpdateVisualStyle ();
		}

		void UpdateDockButtonAccessibilityLabels ()
		{
			string realLabel;
			string realHelp;
			bool dockable = item.Status != DockItemStatus.Dockable;

			if (string.IsNullOrEmpty (label)) {
				if (dockable) {
					realLabel = Translations.GetString ("Dock pad");
					realHelp = Translations.GetString ("Dock the pad into the UI so it will not hide automatically");
				} else {
					realLabel = Translations.GetString ("Autohide pad");
					realHelp = Translations.GetString ("Automatically hide the pad when it loses focus");
				}
			} else {
				if (dockable) {
					realLabel = Translations.GetString ("Dock {0}", label);
					realHelp = Translations.GetString ("Dock the {0} pad into the UI so it will not hide automatically", label);
				} else {
					realLabel = Translations.GetString ("Autohide {0}", label);
					realHelp = Translations.GetString ("Automatically hide the {0} pad when it loses focus", label);
				}
			}
			btnDock.Accessible.SetLabel (realLabel);
			btnDock.Accessible.Description = realHelp;
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

		public int MinWidth {
			get { return minWidth; }
		}

		public bool Active {
			get {
				return active;
			}
			set {
				if (active != value) {
					active = value;
					UpdateVisualStyle ();
					QueueResize ();
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
					btnDock.TooltipText = Translations.GetString ("Dock");
				} else {
					btnDock.Image = pixAutoHide;
					btnDock.TooltipText = Translations.GetString ("Auto Hide");
				}
			} else {
				btnDock.Image = null;
				btnClose.Image = null;
			}

			UpdateDockButtonAccessibilityLabels ();
		}

		bool tabPressed, tabActivated;
		double pressX, pressY;

		protected override void OnActivate ()
		{
			TabPressed?.Invoke (this, EventArgs.Empty);

			base.OnActivate ();
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.TriggersContextMenu ()) {
				item.ShowDockPopupMenu (this, evnt);
				return false;
			} else if (evnt.Button == 1) {
				if (evnt.Type == Gdk.EventType.ButtonPress) {
					tabPressed = true;
					pressX = evnt.X;
					pressY = evnt.Y;
					TabPressed?.Invoke (this, EventArgs.Empty);
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
			} else if (!evnt.TriggersContextMenu () && evnt.Button == 1) {
				frame.DockInPlaceholder (item);
				frame.HidePlaceholder ();
				if (Window != null)
					Window.Cursor = null;
				frame.Toplevel.KeyPressEvent -= HeaderKeyPress;
				frame.Toplevel.KeyReleaseEvent -= HeaderKeyRelease;
			}
			tabPressed = false;
			return base.OnButtonReleaseEvent (evnt);
		}

		protected override bool OnFocusInEvent (Gdk.EventFocus evnt)
		{
			mouseOver = true;
			UpdateBehavior ();
			return base.OnFocusInEvent (evnt);
		}

		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			if (currentFocus == null) {
				mouseOver = false;
				UpdateBehavior ();
			}
			return base.OnFocusOutEvent (evnt);
		}

		void HandlePress (object sender, EventArgs args)
		{
			TabPressed?.Invoke (this, EventArgs.Empty);
		}

		void HandleShowMenu (object sender, EventArgs args)
		{
			// Show the menu at the middle of the widget
			item.ShowDockPopupMenu (this, Allocation.Width / 2, Allocation.Height / 2);
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			if (tabPressed && !item.Behavior.HasFlag (DockItemBehavior.NoGrip) && Math.Abs (evnt.X - pressX) > 3 && Math.Abs (evnt.Y - pressY) > 3) {
				frame.ShowPlaceholder (item);
				Window.Cursor = fleurCursor;
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

		enum FocusWidget
		{
			None,
			Widget,
			DockButton,
			CloseButton
		};

		bool FocusCurrentWidget (DirectionType direction)
		{
			if (currentFocus == null) {
				return false;
			}

			return currentFocus.ChildFocus (direction);
		}

		bool MoveFocusToWidget (FocusWidget widget, DirectionType direction)
		{
			switch (widget) {
			case FocusWidget.Widget:
				GrabFocus ();
				currentFocus = null;
				return true;

			case FocusWidget.DockButton:
				currentFocus = btnDock;
				return btnDock.ChildFocus (direction);

			case FocusWidget.CloseButton:
				currentFocus = btnClose;
				return btnClose.ChildFocus (direction);

			case FocusWidget.None:
				break;
			}

			return false;
		}

		FocusWidget GetNextWidgetToFocus (FocusWidget widget, DirectionType direction)
		{
			FocusWidget nextSite;

			switch (widget) {
			case FocusWidget.CloseButton:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
				case DirectionType.Down:
					return FocusWidget.None;

				case DirectionType.TabBackward:
				case DirectionType.Left:
				case DirectionType.Up:
					if (btnDock.Image != null) {
						nextSite = FocusWidget.DockButton;
					} else {
						nextSite = FocusWidget.Widget;
					}
					return nextSite;
				}

				break;

			case FocusWidget.DockButton:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
				case DirectionType.Down:
					return btnClose.Image == null ? FocusWidget.None : FocusWidget.CloseButton;

				case DirectionType.TabBackward:
				case DirectionType.Left:
				case DirectionType.Up:
					return FocusWidget.Widget;
				}

				break;

			case FocusWidget.Widget:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
				case DirectionType.Down:
					if (btnDock.Image != null) {
						nextSite = FocusWidget.DockButton;
					} else if (btnClose.Image != null) {
						nextSite = FocusWidget.CloseButton;
					} else {
						nextSite = FocusWidget.None;
					}
					return nextSite;

				case DirectionType.TabBackward:
				case DirectionType.Left:
				case DirectionType.Up:
					return FocusWidget.None;
				}

				break;
			case FocusWidget.None:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
				case DirectionType.Down:
					return FocusWidget.Widget;

				case DirectionType.TabBackward:
				case DirectionType.Left:
				case DirectionType.Up:
					if (btnClose.Image != null) {
						nextSite = FocusWidget.CloseButton;
					} else if (btnDock.Image != null) {
						nextSite = FocusWidget.DockButton;
					} else {
						nextSite = FocusWidget.Widget;
					}
					return nextSite;
				}

				break;
			}

			return FocusWidget.None;
		}

		protected override bool OnFocused (DirectionType direction)
		{
			if (!FocusCurrentWidget (direction)) {
				FocusWidget focus = FocusWidget.None;

				if (currentFocus == btnClose) {
					focus = FocusWidget.CloseButton;
				} else if (currentFocus == btnDock) {
					focus = FocusWidget.DockButton;
				} else if (IsFocus) {
					focus = FocusWidget.Widget;
				}

				while ((focus = GetNextWidgetToFocus (focus, direction)) != FocusWidget.None) {
					if (MoveFocusToWidget (focus, direction)) {
						return true;
					}
				}

				// Clean up the icons because OnFocusOutEvent has already been called
				// so we need the icons to hide again
				mouseOver = false;
				UpdateBehavior ();

				currentFocus = null;
				return false;
			}

			return true;
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

        protected override void OnGetPreferredHeight(out int minimum_height, out int natural_height)
        {
			if (Child != null)
            {
				Child.GetPreferredHeight(out minimum_height, out natural_height);

				int padding;
				if (active)
					padding = TopPaddingActive + BottomPaddingActive;
				else
					padding = TopPadding + BottomPadding;

				minimum_height += padding;
				natural_height += padding;
            }
			else
				base.OnGetPreferredHeight(out minimum_height, out natural_height);
        }

        protected override void OnGetPreferredWidth(out int minimum_width, out int natural_width)
        {
			if (Child != null)
            {
				Child.GetPreferredWidth(out minimum_width, out natural_width);

				int padding = LeftPadding + RightPadding;
				minimum_width += padding;
				natural_width += padding;
            }
			else
				base.OnGetPreferredWidth(out minimum_width, out natural_width);
        }

        protected override void OnSizeAllocated (Gdk.Rectangle rect)
		{
			base.OnSizeAllocated (rect);

			int leftPadding = LeftPadding;
			int rightPadding = RightPadding;
			
			rect.X += leftPadding;
			rect.Width -= leftPadding + rightPadding;
			if (rect.Width < 1) {
				rect.Width = 1;
			}

			if (Child != null) {
				var bottomPadding = active ? BottomPaddingActive : BottomPadding;
				var topPadding = active ? TopPaddingActive : TopPadding;
				int centerY = topPadding + ((rect.Height - bottomPadding - topPadding) / 2);
				var height = Child.SizeRequest ().Height;
				rect.Y += centerY - (height / 2);
				rect.Height = height;
				Child.SizeAllocate (rect);
			}
		}

        protected override bool OnDrawn(Context ctx)
        {
			if (VisualStyle.TabStyle == DockTabStyle.Normal)
				DrawAsBrowser(ctx);
			else
				DrawNormal(ctx);

#if false
			if (HasFocus)
			{
				var alloc = labelWidget.Allocation;
				Gtk.Style.PaintFocus(Style, Window, State, alloc, this, "label",
									  alloc.X, alloc.Y, alloc.Width, alloc.Height);
			}
#endif
			return base.OnDrawn(ctx);
        }

        void DrawAsBrowser (Context ctx)
		{
			bool first = true;
			bool last = true;

			if (Parent is TabStrip.TabStripBox) {
				var tsb = (TabStrip.TabStripBox) Parent;
				var cts = tsb.Children;
				first = cts[0] == this;
				last = cts[cts.Length - 1] == this;
			}

			{
				int radius = 3;

				if (first && last) {
					ctx.Rectangle (0, 0, AllocatedWidth, AllocatedHeight);
					ctx.SetSourceColor (VisualStyle.PadBackgroundColor.Value.ToCairoColor ());
					ctx.Fill ();
				} else if (Active) {
					var x = Allocation.X;
					var y = Allocation.Y + TopPadding - 6;
					ctx.RoundedRectangle(x, y, Allocation.Width - 1, Allocation.Height - y, radius, CairoCorners.TopLeft | CairoCorners.TopRight);
					ctx.SetSourceColor(VisualStyle.PadBackgroundColor.Value.ToCairoColor());
					ctx.Fill();
				}
				else {
					var x = Allocation.X;
					var y = Allocation.Y + TopPadding - 4;
					ctx.RoundedRectangle(x, y, Allocation.Width - 1, Allocation.Height - y - 1, radius, CairoCorners.TopLeft | CairoCorners.TopRight);
					ctx.SetSourceColor(VisualStyle.InactivePadBackgroundColor.Value.ToCairoColor());
					ctx.Fill();
				}
			}
		}

		void DrawNormal (Context ctx)
        {
            var x = Allocation.X;
            var y = Allocation.Y;

            ctx.Rectangle(x, y + 1, Allocation.Width, Allocation.Height - 1);
            ctx.SetSourceColor(Styles.DockBarBackground.ToCairoColor());
            ctx.Fill();

            /*
            if (active) {
                double offset = Allocation.Height * 0.25;
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
                ctx.Fill ();
            }
            */

        }
    }
}
