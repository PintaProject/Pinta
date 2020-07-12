//
// TabStrip.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using System.Linq;
using Gdk;
using Gtk;
using System;
using System.Collections.Generic;
using Cairo;
using MonoDevelop.Components;
using Xwt.Motion;
using System.Runtime.InteropServices;
using System.Reflection;
using Pinta.Docking.AtkCocoaHelper;
using Pinta.Core;

namespace Pinta.Docking.DockNotebook
{
	class TabStrip: EventBox, IAnimatable
	{
        static Gdk.Pixbuf tabbarPrevImage = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "tabbar-prev-12.png");
        static Gdk.Pixbuf tabbarNextImage = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "tabbar-next-12.png");
        static Gdk.Pixbuf tabActiveBackImage = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "tabbar-active.9.png");
        static Gdk.Pixbuf tabBackImage = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "tabbar-inactive.9.png");
        static Gdk.Pixbuf tabbarBackImage = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "tabbar-back.9.png");
        static Gdk.Pixbuf tabCloseImage = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "tab-close-9.png");
        static Gdk.Pixbuf tabDirtyImage = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "tab-dirty-9.png");
        static Gdk.Pixbuf tabPinnedImage = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "tab-pinned-9.png");
        static Gdk.Pixbuf tabUnPinnedImage = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "tab-unpinned-9.png");

        HBox innerBox;

		readonly DockNotebook notebook;
		DockNotebookTab highlightedTab;
		bool overCloseButton;
		bool buttonPressedOnTab;
		int tabStartX, tabEndX;
		bool isActiveNotebook;

		MouseTracker tracker;

		bool draggingTab;
		int dragX;
		int dragOffset;
		double dragXProgress;

		int renderOffset;
		int targetOffset;
		int animationTarget;

		Dictionary<int, DockNotebookTab> closingTabs;

		public Button PreviousButton;
		public Button NextButton;
		public MonoDevelop.Components.MenuButton DropDownButton;

		static readonly int TotalHeight = 32;
#if false
		static readonly Xwt.WidgetSpacing TabPadding;
		static readonly Xwt.WidgetSpacing TabActivePadding;
#endif
		static readonly int LeftBarPadding = 44;
		static readonly int RightBarPadding = 22;
		static readonly int VerticalTextSize = 11;
		const int TabSpacing = 0;
		const int LeanWidth = 12;
		const double CloseButtonMarginRight = 1;
		const double CloseButtonMarginBottom = -1.0;
		const double PinButtonMarginRight = 10.5;

		int TabWidth { get; set; }

		int LastTabWidthAdjustment { get; set; }

		int targetWidth;

		int TargetWidth {
			get { return targetWidth; }
			set {
				targetWidth = value;
				if (TabWidth != value) {
					this.Animate ("TabWidth",
						f => TabWidth = (int)f,
						TabWidth,
						value,
						easing: Easing.CubicOut);
				}
			}
		}

		public bool  NavigationButtonsVisible {
			get { return NextButton.Visible; }
			set {
				if (value == NavigationButtonsVisible)
					return;

				NextButton.Visible = value;
				PreviousButton.Visible = value;

				OnSizeAllocated (Allocation);
			}
		}

		static TabStrip ()
		{
#if false
			Xwt.Drawing.NinePatchImage tabBackImage9;
			if (tabBackImage is Xwt.Drawing.ThemedImage) {
				var img = ((Xwt.Drawing.ThemedImage)tabBackImage).GetImage (Xwt.Drawing.Context.GlobalStyles);
				tabBackImage9 = img as Xwt.Drawing.NinePatchImage;
			} else
				tabBackImage9 = tabBackImage as Xwt.Drawing.NinePatchImage;
			TabPadding = tabBackImage9.Padding;


			Xwt.Drawing.NinePatchImage tabActiveBackImage9;
			if (tabActiveBackImage is Xwt.Drawing.ThemedImage) {
				var img = ((Xwt.Drawing.ThemedImage)tabActiveBackImage).GetImage (Xwt.Drawing.Context.GlobalStyles);
				tabActiveBackImage9 = img as Xwt.Drawing.NinePatchImage;
			} else
				tabActiveBackImage9 = tabActiveBackImage as Xwt.Drawing.NinePatchImage;
			TabActivePadding = tabActiveBackImage9.Padding;
#endif
		}

		public TabStrip (DockNotebook notebook)
		{
			if (notebook == null)
				throw new ArgumentNullException ("notebook");

			Accessible.SetCommonAttributes ("Document.Tabstrip",
			                                Translations.GetString ("Document Navigation Bar"),
			                                Translations.GetString ("Contains controls to select which document is being edited"));
			Accessible.SetRole (AtkCocoa.Roles.AXTabGroup);

			// Handle focus for the tabs.
			CanFocus = true;

			TabWidth = 125;
			TargetWidth = 125;
			tracker = new MouseTracker (this);
			GtkWorkarounds.FixContainerLeak (this);

			innerBox = new HBox (false, 0);
			innerBox.Accessible.SetShouldIgnore (true);
			Add (innerBox);

			this.notebook = notebook;
			AppPaintable = true;
			Events |= EventMask.PointerMotionMask | EventMask.LeaveNotifyMask | EventMask.ButtonPressMask;

			var arr = new Xwt.ImageView (tabbarPrevImage);
			arr.HeightRequest = arr.WidthRequest = 10;

			var alignment = new Alignment (0.5f, 0.5f, 0.0f, 0.0f);
			alignment.Add (arr.ToGtkWidget ());
			PreviousButton = new Button (alignment);
			PreviousButton.TooltipText = Translations.GetString ("Switch to previous document");
			PreviousButton.Relief = ReliefStyle.None;
			PreviousButton.CanDefault = false;
			PreviousButton.CanFocus = true;
			PreviousButton.Accessible.Name = "DockNotebook.Tabstrip.PreviousButton";
			PreviousButton.Accessible.SetTitle (Translations.GetString ("Previous document"));
			PreviousButton.Accessible.Description = Translations.GetString ("Switch to previous document");

			arr = new Xwt.ImageView (tabbarNextImage);
			arr.HeightRequest = arr.WidthRequest = 10;

			alignment = new Alignment (0.5f, 0.5f, 0.0f, 0.0f);
			alignment.Add (arr.ToGtkWidget ());
			NextButton = new Button (alignment);
			NextButton.TooltipText = Translations.GetString ("Switch to next document");
			NextButton.Relief = ReliefStyle.None;
			NextButton.CanDefault = false;
			NextButton.CanFocus = true;
			NextButton.Accessible.Name = "DockNotebook.Tabstrip.NextButton";
			NextButton.Accessible.SetTitle (Translations.GetString ("Next document"));
			NextButton.Accessible.Description = Translations.GetString ("Switch to next document");

			DropDownButton = new MonoDevelop.Components.MenuButton ();
			DropDownButton.TooltipText = Translations.GetString ("Document List");
			DropDownButton.Relief = ReliefStyle.None;
			DropDownButton.CanDefault = false;
			DropDownButton.CanFocus = true;
			DropDownButton.Accessible.Name = "DockNotebook.Tabstrip.DocumentListButton";
			DropDownButton.Accessible.SetTitle (Translations.GetString ("Document list"));
			DropDownButton.Accessible.Description = Translations.GetString ("Display the document list menu");

			PreviousButton.ShowAll ();
			PreviousButton.NoShowAll = true;
			NextButton.ShowAll ();
			NextButton.NoShowAll = true;
			DropDownButton.ShowAll ();

			PreviousButton.Name = "MonoDevelop.DockNotebook.BarButton";
			NextButton.Name = "MonoDevelop.DockNotebook.BarButton";
			DropDownButton.Name = "MonoDevelop.DockNotebook.BarButton";

			innerBox.PackStart (PreviousButton, false, false, 0);
			innerBox.PackStart (NextButton, false, false, 0);
			innerBox.PackEnd (DropDownButton, false, false, 0);

			tracker.HoveredChanged += (sender, e) => {
				if (!tracker.Hovered) {
					SetHighlightedTab (null);
					UpdateTabWidth (tabEndX - tabStartX);
					QueueDraw ();
				}
			};
			
			foreach (var tab in notebook.Tabs) {
				if (tab.Accessible != null) {
					Accessible.AddAccessibleElement (tab.Accessible);

					tab.AccessibilityPressTab += OnAccessibilityPressTab;
					tab.AccessibilityPressCloseButton += OnAccessibilityPressCloseButton;
					tab.AccessibilityShowMenu += OnAccessibilityShowMenu;
				}
			}
			UpdateAccessibilityTabs ();
			notebook.PageAdded += PageAddedHandler;
			notebook.PageRemoved += PageRemovedHandler;
			notebook.TabsReordered += PageReorderedHandler;

			closingTabs = new Dictionary<int, DockNotebookTab> ();
		}

		protected override void OnDestroyed ()
		{
			this.AbortAnimation ("TabWidth");
			this.AbortAnimation ("EndDrag");
			this.AbortAnimation ("ScrollTabs");
			base.OnDestroyed ();
		}

		void PageAddedHandler (object sender, TabEventArgs args)
		{
			var tab = args.Tab;

			if (tab.Accessible != null) {
				Accessible.AddAccessibleElement (tab.Accessible);
				Accessible.AddAccessibleElement (tab.CloseButtonAccessible);

				tab.AccessibilityPressTab += OnAccessibilityPressTab;
				tab.AccessibilityPressCloseButton += OnAccessibilityPressCloseButton;
				tab.AccessibilityShowMenu += OnAccessibilityShowMenu;
			}

			QueueResize ();

			UpdateAccessibilityTabs ();
		}

		void PageRemovedHandler (object sender, TabEventArgs args)
		{
			var tab = args.Tab;

			if (tab.Accessible != null) {
				tab.AccessibilityPressTab -= OnAccessibilityPressTab;
				tab.AccessibilityPressCloseButton -= OnAccessibilityPressCloseButton;
				tab.AccessibilityShowMenu -= OnAccessibilityShowMenu;

				Accessible.RemoveAccessibleElement (tab.Accessible);
				Accessible.RemoveAccessibleElement (tab.CloseButtonAccessible);
			}

			tab.Dispose ();

			QueueResize ();

			UpdateAccessibilityTabs ();
		}

		void PageReorderedHandler (DockNotebookTab tab, int oldPlacement, int newPlacement)
		{
			QueueResize ();

			UpdateAccessibilityTabs ();
		}

		void UpdateAccessibilityTabs ()
		{
			var tabs = notebook.Tabs.Where (x => x.Accessible != null).OrderBy (x => x.Index).Select (x => x.Accessible).ToArray ();
			Accessible.SetTabs (tabs);
		}

		void IAnimatable.BatchBegin ()
		{
		}

		void IAnimatable.BatchCommit ()
		{
			QueueDraw ();
		}

		public bool IsActiveNotebook {
			get {
				return this.isActiveNotebook;
			}
			set {
				isActiveNotebook = value;
				QueueDraw ();
			}
		}

		public void StartOpenAnimation (DockNotebookTab tab)
		{
			tab.WidthModifier = 0;
			new Animation (f => tab.WidthModifier = f)
				.AddConcurrent (new Animation (f => tab.Opacity = f), 0.0d, 0.2d)
				.Commit (tab, "Open", easing: Easing.CubicInOut);
		}

		public void StartCloseAnimation (DockNotebookTab tab)
		{
			closingTabs [tab.Index] = tab;
			new Animation (f => tab.WidthModifier = f, tab.WidthModifier, 0)
				.AddConcurrent (new Animation (f => tab.Opacity = f, tab.Opacity, 0), 0.8d)
				.Commit (tab, "Closing",
				easing: Easing.CubicOut,
				finished: (f, a) => {
					if (!a)
						closingTabs.Remove (tab.Index);
				});
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			if (NavigationButtonsVisible) {
				tabStartX = /*allocation.X +*/ LeftBarPadding + LeanWidth / 2;
			} else {
				tabStartX = LeanWidth / 2;
			}
			tabEndX = allocation.Width - RightBarPadding;

			base.OnSizeAllocated (allocation);
			Update ();
		}

		// TODO-GTK3
#if false
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Height = TotalHeight;
			requisition.Width = 0;
		}
#endif

		internal void InitSize ()
		{
			return;
		}

		public int BarHeight {
			get { return TotalHeight; }
		}

		int lastDragX;

		void SetHighlightedTab (DockNotebookTab tab)
		{
			if (highlightedTab == tab)
				return;

			if (highlightedTab != null) {
				var tmp = highlightedTab;
				tmp.Animate ("Glow",
					f => tmp.GlowStrength = f,
					start: tmp.GlowStrength,
					end: 0);
			}

			if (tab != null) {
				tab.Animate ("Glow",
					f => tab.GlowStrength = f,
					start: tab.GlowStrength,
					end: 1);
			}

			highlightedTab = tab;
			QueueDraw ();
		}

		class PadTitleWindow: Gtk.Window
		{
			public PadTitleWindow (DockFrame frame, DockItem draggedItem) : base (Gtk.WindowType.Popup)
			{
				SkipTaskbarHint = true;
				Decorated = false;
				TransientFor = (Gtk.Window)frame.Toplevel;
				TypeHint = WindowTypeHint.Utility;

				var mainBox = new VBox ();

				var box = new HBox (false, 3);
				if (draggedItem.Icon != null) {
					var img = new Xwt.ImageView (draggedItem.Icon);
					box.PackStart (img.ToGtkWidget (), false, false, 0);
				}
				var la = new Label ();
				la.Markup = draggedItem.Label;
				box.PackStart (la, false, false, 0);

				mainBox.PackStart (box, false, false, 0);

				var f = new CustomFrame ();
				f.SetPadding (12, 12, 12, 12);
				f.SetMargins (1, 1, 1, 1);
				f.Add (mainBox);

				Add (f);
				ShowAll ();
			}
		}

		PlaceholderWindow placeholderWindow;
		bool mouseHasLeft;

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			if (draggingTab && placeholderWindow == null && !mouseHasLeft)
				mouseHasLeft = true;
			return base.OnLeaveNotifyEvent (evnt);
		}

		void CreatePlaceholderWindow ()
		{
			var tab = notebook.CurrentTab;
			placeholderWindow = new PlaceholderWindow (tab);

			int x, y;
			Gdk.Display.Default.GetPointer (out x, out y);
			placeholderWindow.MovePosition (x, y);
			placeholderWindow.Show ();

			placeholderWindow.Destroyed += delegate {
				placeholderWindow = null;
				buttonPressedOnTab = false;
			};
		}

		Gdk.Rectangle GetScreenRect ()
		{
			int ox, oy;
			ParentWindow.GetOrigin (out ox, out oy);
			var alloc = notebook.Allocation;
			alloc.X += ox;
			alloc.Y += oy;
			return alloc;
		}

		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			if (draggingTab && mouseHasLeft) {
				var sr = GetScreenRect ();
				sr.Height = BarHeight;
				sr.Inflate (30, 30);

				int x, y;
				Gdk.Display.Default.GetPointer (out x, out y);

				if (x < sr.Left || x > sr.Right || y < sr.Top || y > sr.Bottom) {
					draggingTab = false;
					mouseHasLeft = false;
					CreatePlaceholderWindow ();
				}
			}

			string newTooltip = null;
			if (placeholderWindow != null) {
				int x, y;
				Gdk.Display.Default.GetPointer (out x, out y);
				placeholderWindow.MovePosition (x, y);
				return base.OnMotionNotifyEvent (evnt);
			}

			if (!draggingTab) {
				var t = FindTab ((int)evnt.X, (int)evnt.Y);

				// If the user clicks and drags on the 'x' which closes the current
				// tab we can end up with a null tab here
				if (t == null) {
					TooltipText = null;
					return base.OnMotionNotifyEvent (evnt);
				}
				SetHighlightedTab (t);

				var newOver = IsOverCloseButton (t, (int)evnt.X, (int)evnt.Y);
				if (newOver != overCloseButton) {
					overCloseButton = newOver;
					QueueDraw ();
				}
				if (!overCloseButton && !draggingTab && buttonPressedOnTab) {
					draggingTab = true;
					mouseHasLeft = false;
					dragXProgress = 1.0f;
					int x = (int)evnt.X;
					dragOffset = x - t.Allocation.X;
					dragX = x - dragOffset;
					lastDragX = (int)evnt.X;
				} else if (t != null)
					newTooltip = t.Tooltip;
			} else if (evnt.State.HasFlag (ModifierType.Button1Mask)) {
				dragX = (int)evnt.X - dragOffset;
				QueueDraw ();

				var t = FindTab ((int)evnt.X, (int)TabPadding.Top + 3);
				if (t == null) {
					var last = (DockNotebookTab)notebook.Tabs.Last ();
					if (dragX > last.Allocation.Right)
						t = last;
					if (dragX < 0)
						t = (DockNotebookTab)notebook.Tabs.First ();
				}
				if (t != null && t != notebook.CurrentTab && (
				        ((int)evnt.X > lastDragX && t.Index > notebook.CurrentTab.Index) ||
				        ((int)evnt.X < lastDragX && t.Index < notebook.CurrentTab.Index))) {
					t.SaveAllocation ();
					t.SaveStrength = 1;
					notebook.ReorderTab ((DockNotebookTab)notebook.CurrentTab, t);

					t.Animate ("TabMotion",
						f => t.SaveStrength = f,
						1.0f,
						0.0f,
						easing: Easing.CubicInOut);
				}
				lastDragX = (int)evnt.X;
			}

			if (newTooltip != null && TooltipText != null && TooltipText != newTooltip)
				TooltipText = null;
			else
				TooltipText = newTooltip;

			return base.OnMotionNotifyEvent (evnt);
		}

		bool overPinOnPress;
		bool overCloseOnPress;
		bool allowDoubleClick;

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			var t = FindTab ((int)evnt.X, (int)evnt.Y);
			if (t != null) {
				if (evnt.IsContextMenuButton ()) {
					DockNotebook.ActiveNotebook = notebook;
					notebook.CurrentTab = t;
					notebook.DoPopupMenu (notebook, t.Index, evnt);
					return true;
				}
				// Don't select the tab if we are clicking the close button
				if (IsOverCloseButton (t, (int)evnt.X, (int)evnt.Y)) {
					overCloseOnPress = true;
					return true;
				}
				overCloseOnPress = false;

				// Don't select the tab if we are clicking the pin button
				if (IsOverPinButton (t, (int)evnt.X, (int)evnt.Y)) {
					overPinOnPress = true;
					return true;
				}
				overPinOnPress = false;

				if (evnt.Type == EventType.TwoButtonPress) {
					if (allowDoubleClick) {
						notebook.OnActivateTab (t);
						buttonPressedOnTab = false;
					}
					return true;
				}
				if (evnt.Button == 2) {
					notebook.OnCloseTab (t);
					return true;
				}

				DockNotebook.ActiveNotebook = notebook;
				buttonPressedOnTab = true;
				notebook.CurrentTab = t;
				return true;
			}
			buttonPressedOnTab = true;
			QueueDraw ();
			return base.OnButtonPressEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			buttonPressedOnTab = false;

			if (placeholderWindow != null) {
				placeholderWindow.PlaceWindow (notebook);
				return base.OnButtonReleaseEvent (evnt);
			}

			if (!draggingTab) {
				var t = FindTab ((int)evnt.X, (int)evnt.Y);
				if (t != null && overCloseOnPress && IsOverCloseButton (t, (int)evnt.X, (int)evnt.Y)) {
					notebook.OnCloseTab (t);
					allowDoubleClick = false;
					return true;
				} else if (t != null && overPinOnPress && IsOverPinButton (t, (int)evnt.X, (int)evnt.Y)) {
					t.IsPinned = !t.IsPinned;
					notebook.OnPinTab (t);
					allowDoubleClick = false;
					QueueDraw ();
					return true;
				}
			}
			overCloseOnPress = false;
			overPinOnPress = false;
			allowDoubleClick = true;
			if (dragX != 0)
				this.Animate ("EndDrag",
					f => dragXProgress = f,
					1.0d,
					0.0d,
					easing: Easing.CubicOut,
					finished: (f, a) => draggingTab = false);
			QueueDraw ();
			return base.OnButtonReleaseEvent (evnt);
		}

		void OnAccessibilityPressTab (object sender, EventArgs args)
		{
			DockNotebook.ActiveNotebook = notebook;
			notebook.CurrentTab = sender as DockNotebookTab;

			QueueDraw ();
		}

		void OnAccessibilityPressCloseButton (object sender, EventArgs args)
		{
			notebook.OnCloseTab (sender as DockNotebookTab);

			QueueDraw ();
		}

		void OnAccessibilityShowMenu (object sender, EventArgs args)
		{
			var tab = sender as DockNotebookTab;
			DockNotebook.ActiveNotebook = notebook;
			notebook.CurrentTab = tab;

			int x = tab.Allocation.X + (tab.Allocation.Width / 2);
			int y = tab.Allocation.Y + (tab.Allocation.Height / 2);

			// Fake an event, but all we need is the x and y.
			// Ugly but the only way without messing up the public API.
			var nativeEvent = new NativeEventButtonStruct {
				x = x,
				y = y,
			};

			IntPtr ptr = GLib.Marshaller.StructureToPtrAlloc (nativeEvent);
			try {
				Gdk.EventButton evnt = new Gdk.EventButton (ptr);
				notebook.DoPopupMenu (notebook, tab.Index, evnt);
			} finally {
				Marshal.FreeHGlobal (ptr);
			}
		}

		Widget currentFocus = null;
		int currentFocusTab = -1;
		bool currentFocusCloseButton;

		protected override void OnActivate ()
		{
			if (currentFocusTab == -1) {
				return;
			}
			var tab = notebook.Tabs [currentFocusTab];
			if (currentFocusCloseButton) {
				notebook.OnCloseTab (tab);

				currentFocusTab--;
				currentFocusCloseButton = true;

				// Focus the next tab
				OnFocused (DirectionType.TabForward);
			} else {
				notebook.CurrentTab = tab;
			}
		}

		enum FocusWidget
		{
			None,
			BackButton,
			NextButton,
			Tabs,
			TabCloseButton,
			MenuButton
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
			case FocusWidget.BackButton:
				currentFocus = PreviousButton;
				return PreviousButton.ChildFocus (direction);

			case FocusWidget.NextButton:
				currentFocus = NextButton;
				return NextButton.ChildFocus (direction);

			case FocusWidget.MenuButton:
				currentFocus = DropDownButton;
				return DropDownButton.ChildFocus (direction);

			case FocusWidget.Tabs:
			case FocusWidget.TabCloseButton:
				GrabFocus ();
				currentFocus = null;
				QueueDraw ();
				return true;

			case FocusWidget.None:
				break;
			}
			return false;
		}

		FocusWidget GetNextWidgetToFocus (FocusWidget widget, DirectionType direction)
		{
			switch (widget) {
			case FocusWidget.BackButton:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
					if (NextButton.Sensitive && NextButton.Visible) {
						return FocusWidget.NextButton;
					} else if (notebook.Tabs.Count > 0) {
						currentFocusTab = 0;
						return FocusWidget.Tabs;
					} else if (DropDownButton.Sensitive && DropDownButton.Visible) {
						return FocusWidget.MenuButton;
					} else {
						return FocusWidget.None;
					}

				case DirectionType.TabBackward:
				case DirectionType.Left:
					return FocusWidget.None;
				}
				break;

			case FocusWidget.NextButton:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
					if (notebook.Tabs.Count > 0) {
						currentFocusTab = 0;
						return FocusWidget.Tabs;
					} else if (DropDownButton.Sensitive && DropDownButton.Visible) {
						return FocusWidget.MenuButton;
					} else {
						return FocusWidget.None;
					}

				case DirectionType.TabBackward:
				case DirectionType.Left:
					if (PreviousButton.Sensitive && PreviousButton.Visible) {
						return FocusWidget.BackButton;
					} else {
						return FocusWidget.None;
					}
				}
				break;

			case FocusWidget.Tabs:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
					currentFocusCloseButton = true;
					return FocusWidget.TabCloseButton;
					/*
					if (currentFocusTab < notebook.Tabs.Count - 1) {
						currentFocusTab++;
						return FocusWidget.Tabs;
					} else if (DropDownButton.Sensitive && DropDownButton.Visible) {
						currentFocusTab = -1;
						return FocusWidget.MenuButton;
					} else {
						return FocusWidget.None;
					}
					*/

				case DirectionType.TabBackward:
				case DirectionType.Left:
					if (currentFocusTab > 0) {
						currentFocusTab--;
						currentFocusCloseButton = true;
						return FocusWidget.TabCloseButton;
					} else if (NextButton.Sensitive && NextButton.Visible) {
						currentFocusTab = -1;
						return FocusWidget.NextButton;
					} else if (PreviousButton.Sensitive && PreviousButton.Visible) {
						currentFocusTab = -1;
						return FocusWidget.BackButton;
					} else {
						currentFocusTab = -1;
						return FocusWidget.None;
					}
				}
				break;

			case FocusWidget.TabCloseButton:
				currentFocusCloseButton = false;
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
					if (currentFocusTab < notebook.Tabs.Count - 1) {
						currentFocusTab++;
						return FocusWidget.Tabs;
					} else if (DropDownButton.Sensitive && DropDownButton.Visible) {
						currentFocusTab = -1;
						return FocusWidget.MenuButton;
					} else {
						return FocusWidget.None;
					}

				case DirectionType.TabBackward:
				case DirectionType.Left:
					return FocusWidget.Tabs;
				}
				break;

			case FocusWidget.MenuButton:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
					return FocusWidget.None;

				case DirectionType.TabBackward:
				case DirectionType.Left:
					if (notebook.Tabs.Count > 0) {
						currentFocusTab = notebook.Tabs.Count - 1;
						currentFocusCloseButton = true;
						return FocusWidget.TabCloseButton;
					} else if (NextButton.Sensitive && NextButton.Visible) {
						return FocusWidget.NextButton;
					} else if (PreviousButton.Sensitive && PreviousButton.Visible) {
						return FocusWidget.BackButton;
					} else {
						return FocusWidget.None;
					}
				}
				break;

			case FocusWidget.None:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
					if (PreviousButton.Sensitive && PreviousButton.Visible) {
						return FocusWidget.BackButton;
					}else if (NextButton.Sensitive && NextButton.Visible) {
						return FocusWidget.NextButton;
					} else if (notebook.Tabs.Count > 0) {
						currentFocusTab = 0;
						return FocusWidget.Tabs;
					} else if (DropDownButton.Sensitive && DropDownButton.Visible) {
						return FocusWidget.MenuButton;
					} else {
						return FocusWidget.None;
					}

				case DirectionType.TabBackward:
				case DirectionType.Left:
					if (DropDownButton.Sensitive && DropDownButton.Visible) {
						return FocusWidget.MenuButton;
					} else if (notebook.Tabs.Count > 0) {
						currentFocusTab = notebook.Tabs.Count - 1;
						currentFocusCloseButton = true;
						return FocusWidget.TabCloseButton;
					} else if (NextButton.Sensitive && NextButton.Visible) {
						return FocusWidget.NextButton;
					} else if (PreviousButton.Sensitive && PreviousButton.Visible) {
						return FocusWidget.BackButton;
					} else {
						return FocusWidget.None;
					}
				}
				break;
			}

			return FocusWidget.None;
		}

		protected override bool OnFocused (DirectionType direction)
		{
			if (!FocusCurrentWidget (direction)) {
				FocusWidget focus = FocusWidget.None;

				if (currentFocus == PreviousButton) {
					focus = FocusWidget.BackButton;
				} else if (currentFocus == NextButton) {
					focus = FocusWidget.NextButton;
				} else if (currentFocus == DropDownButton) {
					focus = FocusWidget.MenuButton;
				} else if (IsFocus) {
					focus = currentFocusCloseButton ? FocusWidget.TabCloseButton : FocusWidget.Tabs;
				}

				while ((focus = GetNextWidgetToFocus (focus, direction)) != FocusWidget.None) {
					if (MoveFocusToWidget (focus, direction)) {
						return true;
					}
				}

				currentFocus = null;
				currentFocusTab = -1;
				return false;
			}

			return base.OnFocused (direction);
		}

		protected override void OnUnrealized ()
		{
			// Cancel drag operations and animations
			buttonPressedOnTab = false;
			overCloseOnPress = false;
			overPinOnPress = false;
			allowDoubleClick = true;
			draggingTab = false;
			dragX = 0;
			this.AbortAnimation ("EndDrag");
			base.OnUnrealized ();
		}

		DockNotebookTab FindTab (int x, int y)
		{
			var current = notebook.CurrentTab as DockNotebookTab;
			if (current != null) {
				var allocWithLean = current.Allocation;
				allocWithLean.X -= LeanWidth / 2;
				allocWithLean.Width += LeanWidth;
				if (allocWithLean.Contains (x, y))
					return current;
			}

			for (int n = 0; n < notebook.Tabs.Count; n++) {
				var tab = (DockNotebookTab)notebook.Tabs [n];
				if (tab.Allocation.Contains (x, y))
					return tab;
			}
			return null;
		}

		static bool IsOverCloseButton (DockNotebookTab tab, int x, int y)
		{
			return tab != null && tab.CloseButtonActiveArea.Contains (x, y);
		}

		static bool IsOverPinButton (DockNotebookTab tab, int x, int y)
		{
			return tab != null && tab.PinButtonActiveArea.Contains (x, y);
		}

		public void Update ()
		{
			if (!tracker.Hovered) {
				UpdateTabWidth (tabEndX - tabStartX);
			} else if (closingTabs.ContainsKey (notebook.Tabs.Count)) {
				UpdateTabWidth (closingTabs [notebook.Tabs.Count].Allocation.Right - tabStartX, true);
			}
			QueueDraw ();
		}

		void UpdateTabWidth (int width, bool adjustLast = false)
		{
			if (notebook.Tabs.Any ())
				TargetWidth = Clamp (width / notebook.Tabs.Count, 50, 200);

			if (adjustLast) {
				// adjust to align close buttons properly
				LastTabWidthAdjustment = width - (TargetWidth * notebook.Tabs.Count) + 1;
				LastTabWidthAdjustment = Math.Abs (LastTabWidthAdjustment) < 50 ? LastTabWidthAdjustment : 0;
			} else {
				LastTabWidthAdjustment = 0;
			}
			if (!IsRealized)
				TabWidth = TargetWidth;
		}

		static int Clamp (int val, int min, int max)
		{
			return Math.Max (min, Math.Min (max, val));
		}

		int GetRenderOffset ()
		{
			int tabArea = tabEndX - tabStartX;
			if (notebook.CurrentTabIndex >= 0) {
				int normalizedArea = (tabArea / TargetWidth) * TargetWidth;
				int maxOffset = Math.Max (0, (notebook.Tabs.Count * TargetWidth) - normalizedArea);

				int distanceToTabEdge = TargetWidth * notebook.CurrentTabIndex;
				int window = normalizedArea - TargetWidth;
				targetOffset = Math.Min (maxOffset, Clamp (renderOffset, distanceToTabEdge - window, distanceToTabEdge));

				if (targetOffset != animationTarget) {
					this.Animate ("ScrollTabs",
						easing: Easing.CubicOut,
						start: renderOffset,
						end: targetOffset,
						callback: f => renderOffset = (int)f);
					animationTarget = targetOffset;
				}
			}

			return tabStartX - renderOffset;
		}

		Action<Context> DrawClosingTab (int index, Gdk.Rectangle region, out int width)
		{
			width = 0;
			if (closingTabs.ContainsKey (index)) {
				DockNotebookTab closingTab = closingTabs [index];
				width = (int)(closingTab.WidthModifier * TabWidth);
				int tmp = width;
				return c => DrawTab (c, closingTab, Allocation, new Gdk.Rectangle (region.X, region.Y, tmp, region.Height), false, false, false, CreateTabLayout (closingTab), false);
			}
			return c => {
			};
		}

		void Draw (Context ctx)
		{
			int tabArea = tabEndX - tabStartX;
			int x = GetRenderOffset ();
			const int y = 0;
			int n = 0;
			Action<Context> drawActive = null;
			var drawCommands = new List<Action<Context>> ();

			Gdk.Rectangle focusRect = new Gdk.Rectangle (0, 0, 0, 0);

			for (; n < notebook.Tabs.Count; n++) {
				if (x + TabWidth < tabStartX) {
					x += TabWidth;
					continue;
				}

				if (x > tabEndX)
					break;

				int closingWidth;
				var cmd = DrawClosingTab (n, new Gdk.Rectangle (x, y, 0, Allocation.Height), out closingWidth);
				drawCommands.Add (cmd);
				x += closingWidth;

				var tab = (DockNotebookTab)notebook.Tabs [n];
				bool active = tab == notebook.CurrentTab;
				bool focused = (n == currentFocusTab);

				int width = Math.Min (TabWidth, Math.Max (50, tabEndX - x - 1));
				if (tab == notebook.Tabs.Last ())
					width += LastTabWidthAdjustment;
				width = (int)(width * tab.WidthModifier);

				if (active) {
					int tmp = x;
					drawActive = c => DrawTab (c, tab, Allocation, new Gdk.Rectangle (tmp, y, width, Allocation.Height), true, true, draggingTab, CreateTabLayout (tab, true), focused);
					tab.Allocation = new Gdk.Rectangle (tmp, Allocation.Y, width, Allocation.Height);
				} else {
					int tmp = x;
					bool highlighted = tab == highlightedTab;

					if (tab.SaveStrength > 0.0f) {
						tmp = (int)(tab.SavedAllocation.X + (tmp - tab.SavedAllocation.X) * (1.0f - tab.SaveStrength));
					}

					drawCommands.Add (c => DrawTab (c, tab, Allocation, new Gdk.Rectangle (tmp, y, width, Allocation.Height), highlighted, false, false, CreateTabLayout (tab), focused));
					tab.Allocation = new Gdk.Rectangle (tmp, Allocation.Y, width, Allocation.Height);
				}

				if (focused) {
					if (currentFocusCloseButton) {
						focusRect = new Gdk.Rectangle ((int)tab.CloseButtonActiveArea.X, (int)tab.CloseButtonActiveArea.Y, (int)tab.CloseButtonActiveArea.Width, (int)tab.CloseButtonActiveArea.Height);
					} else {
						focusRect = new Gdk.Rectangle (tab.Allocation.X + 5, tab.Allocation.Y + 10, tab.Allocation.Width - 30, tab.Allocation.Height - 15);
					}
				}
				x += width;
			}

			var allocation = Allocation;
			int tabWidth;
			drawCommands.Add (DrawClosingTab (n, new Gdk.Rectangle (x, y, 0, allocation.Height), out tabWidth));
			drawCommands.Reverse ();

			ctx.DrawImage (this, tabbarBackImage.WithSize (allocation.Width, allocation.Height), 0, 0);

			// Draw breadcrumb bar header
//			if (notebook.Tabs.Count > 0) {
//				ctx.Rectangle (0, allocation.Height - BottomBarPadding, allocation.Width, BottomBarPadding);
//				ctx.SetSourceColor (Styles.BreadcrumbBackgroundColor);
//				ctx.Fill ();
//			}

			ctx.Rectangle (tabStartX - LeanWidth / 2, allocation.Y, tabArea + LeanWidth, allocation.Height);
			ctx.Clip ();

			foreach (var cmd in drawCommands)
				cmd (ctx);

			ctx.ResetClip ();

			// Redraw the dragging tab here to be sure its on top. We drew it before to get the sizing correct, this should be fixed.
			drawActive?.Invoke (ctx);

			// Pinta TODO
#if false
			if (HasFocus) {
				Gtk.Style.PaintFocus (Style, GdkWindow, State, focusRect, this, "tab", focusRect.X, focusRect.Y, focusRect.Width, focusRect.Height);
			}
#endif
		}

		// TODO-GTK3
#if false
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			using (var context = CairoHelper.Create (evnt.Window)) {
				Draw (context);
			}
			return base.OnExposeEvent (evnt);
		}
#endif

		void DrawTab (Context ctx, DockNotebookTab tab, Gdk.Rectangle allocation, Gdk.Rectangle tabBounds, bool highlight, bool active, bool dragging, Pango.Layout la, bool focused)
		{
			// This logic is stupid to have here, should be in the caller!
			if (dragging) {
				tabBounds.X = (int)(tabBounds.X + (dragX - tabBounds.X) * dragXProgress);
				tabBounds.X = Clamp (tabBounds.X, tabStartX, tabEndX - tabBounds.Width);
			}
			// TODO-GTK3
#if false
			double rightPadding = (active ? TabActivePadding.Right : TabPadding.Right) - (LeanWidth / 2);
			rightPadding = (rightPadding * Math.Min (1.0, Math.Max (0.5, (tabBounds.Width - 30) / 70.0)));
			double leftPadding = (active ? TabActivePadding.Left : TabPadding.Left) - (LeanWidth / 2);
			leftPadding = (leftPadding * Math.Min (1.0, Math.Max (0.5, (tabBounds.Width - 30) / 70.0)));
			double bottomPadding = active ? TabActivePadding.Bottom : TabPadding.Bottom;
#endif

			DrawTabBackground (this, ctx, allocation, tabBounds.Width, tabBounds.X, active, tab.IsPinned);

			ctx.LineWidth = 1;
			ctx.NewPath ();

			// Render Close Button (do this first so we can tell how much text to render)

			var closeButtonAlloation = new Cairo.Rectangle (tabBounds.Right - rightPadding - (tabCloseImage.Width / 2) - CloseButtonMarginRight,
			                                 tabBounds.Height - bottomPadding - tabCloseImage.Height - CloseButtonMarginBottom,
			                                 tabCloseImage.Width, tabCloseImage.Height);
			
			tab.CloseButtonActiveArea = closeButtonAlloation.Inflate (2, 2);

			var spinButtonX = closeButtonAlloation.X - Math.Max (tabPinnedImage.Width + CloseButtonMarginRight, rightPadding);
			var spinButtonAllocation = new Cairo.Rectangle (spinButtonX,
									closeButtonAlloation.Y,
									tabPinnedImage.Width, tabPinnedImage.Height);

			tab.PinButtonActiveArea = spinButtonAllocation.Inflate (2, 2);

			bool closeButtonHovered = tracker.Hovered && tab.CloseButtonActiveArea.Contains (tracker.MousePosition);
			bool pinButtonHovered = tracker.Hovered && tab.PinButtonActiveArea.Contains (tracker.MousePosition);
			bool tabHovered = tracker.Hovered && tab.Allocation.Contains (tracker.MousePosition);
			bool drawCloseButton = tab.IsPinned || (active || tabHovered || focused);
			bool drawPinButton = tab.IsPinned || tabHovered;

			if (!closeButtonHovered && tab.DirtyStrength > 0.5) {
				ctx.DrawImage (this, tabDirtyImage, closeButtonAlloation.X, closeButtonAlloation.Y);
				drawCloseButton = false;
			}

			if (drawCloseButton)
				ctx.DrawImage (this, tabCloseImage.WithAlpha ((closeButtonHovered ? 1.0 : 0.5) * tab.Opacity), closeButtonAlloation.X, closeButtonAlloation.Y);
			
			if (drawPinButton)
				ctx.DrawImage (this, (tab.IsPinned ? tabPinnedImage : tabUnPinnedImage).WithAlpha ((pinButtonHovered ? 1.0 : 0.5) * tab.Opacity), spinButtonAllocation.X, spinButtonAllocation.Y);

			// Render Text
			double tw = tabBounds.Width - (leftPadding + rightPadding);
			if (drawCloseButton || tab.DirtyStrength > 0.5)
				tw -= closeButtonAlloation.Width / 2;

			if (drawPinButton || tab.DirtyStrength > 0.5)
				tw -= spinButtonAllocation.Width / 2 + rightPadding;

			double tx = tabBounds.X + leftPadding;
			var baseline = la.GetLine (0).Layout.GetPixelBaseline ();
			double ty = tabBounds.Height - bottomPadding - baseline;

			ctx.MoveTo (tx, ty);
			if (!Platform.IsMac && !Platform.IsWindows) {
				// This is a work around for a linux specific problem.
				// A bug in the proprietary ATI driver caused TAB text not to draw.
				// If that bug get's fixed remove this HACK asap.
				la.Ellipsize = Pango.EllipsizeMode.End;
				la.Width = (int)(tw * Pango.Scale.PangoScale);
				ctx.SetSourceColor ((tab.Notify ? Styles.TabBarNotifyTextColor : (active ? Styles.TabBarActiveTextColor : Styles.TabBarInactiveTextColor)).ToCairoColor ());
				Pango.CairoHelper.ShowLayout (ctx, la.GetLine (0).Layout);
			} else {

				// ellipses are for space wasting ..., we cant afford that
				using (var lg = new LinearGradient (tx + tw - (drawPinButton ? DefaultGradientWidth : DefaultGradientPinWidth), 0, tx + tw, 0)) {
					var color = (tab.Notify ? Styles.TabBarNotifyTextColor : (active ? Styles.TabBarActiveTextColor : Styles.TabBarInactiveTextColor)).ToCairoColor ();
					color = color.MultiplyAlpha (tab.Opacity);
					lg.AddColorStop (0, color);
					color.A = 0;
					lg.AddColorStop (1, color);
					ctx.SetSource (lg);
					Pango.CairoHelper.ShowLayout (ctx, la.GetLine (0).Layout);
				}
			}
            la.Dispose ();
		}

		const double DefaultGradientWidth = 10;
		const double DefaultGradientPinWidth = 13;

		static void DrawTabBackground (Widget widget, Context ctx, Gdk.Rectangle allocation, int contentWidth, int px, bool active = true, bool isPinned = false)
		{
			int lean = Math.Min (LeanWidth, contentWidth / 2);
			int halfLean = lean / 2;

			double x = px + TabSpacing - halfLean;
			double y = 0;
			double height = allocation.Height;
			double width = contentWidth - (TabSpacing * 2) + lean;

			var image = active ? tabActiveBackImage : tabBackImage;
			image = image.WithSize (width, height);

			ctx.DrawImage (widget, image, x, y);
		}

		Pango.Layout CreateSizedLayout (bool active)
		{
			var la = new Pango.Layout (PangoContext);
#if false
			la.FontDescription = IdeServices.FontService.SansFont.Copy ();
			if (!Core.Platform.IsWindows)
				la.FontDescription.Weight = Pango.Weight.Bold;
#else
			la.FontDescription = Pango.FontDescription.FromString("normal");
#endif
			la.FontDescription.AbsoluteSize = Pango.Units.FromPixels (VerticalTextSize);

			return la;
		}

		Pango.Layout CreateTabLayout (DockNotebookTab tab, bool active = false)
		{
			Pango.Layout la = CreateSizedLayout (active);
			if (!string.IsNullOrEmpty (tab.Markup))
				la.SetMarkup (tab.Markup);
			else if (!string.IsNullOrEmpty (tab.Text))
				la.SetText (tab.Text);
			return la;
		}
	}
}
