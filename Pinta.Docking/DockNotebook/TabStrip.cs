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
using Pinta.Docking;
using System.Reflection;

namespace Pinta.Docking.DockNotebook
{
	class TabStrip: EventBox, IAnimatable
	{
        static Gdk.Pixbuf tabbarPrevImage = new Gdk.Pixbuf (Assembly.GetExecutingAssembly (), "tabbar-prev-12.png");
        static Gdk.Pixbuf tabbarNextImage = new Gdk.Pixbuf (Assembly.GetExecutingAssembly (), "tabbar-next-12.png");

		List<Widget> children = new List<Widget> ();
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
		public MenuButton DropDownButton;

		static readonly double PixelScale = GtkWorkarounds.GetPixelScale ();
		static readonly int TopBarPadding = (int)(3 * PixelScale);
		static readonly int BottomBarPadding = (int)(3 * PixelScale);
		static readonly int LeftRightPadding = (int)(10 * PixelScale);
		static readonly int TopPadding = (int)(8 * PixelScale);
		static readonly int BottomPadding = (int)(8 * PixelScale);
		static readonly int LeftBarPadding = (int)(58 * PixelScale);
		static readonly int VerticalTextSize = (int)(11 * PixelScale);
		const int TabSpacing = -1;
		const int Radius = 2;
		const int LeanWidth = 18;
		const int CloseButtonSize = 14;

		const int TextOffset = 1;

		// Vertically aligns the close image(s) with the tab label.
		const int CloseImageTopOffset = 3;

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
			get { return children.Contains (PreviousButton); }
			set {
				if (value == NavigationButtonsVisible)
					return;
				if (value) {
					children.Add (NextButton);
					children.Add (PreviousButton);
					OnSizeAllocated (Allocation);
					PreviousButton.ShowAll ();
					NextButton.ShowAll ();
				} else {
					children.Remove (PreviousButton);
					children.Remove (NextButton);
					OnSizeAllocated (Allocation);
				}
			}
		}

		public TabStrip (DockNotebook notebook)
		{
			if (notebook == null)
				throw new ArgumentNullException ("notebook");
			TabWidth = 125;
			TargetWidth = 125;
			tracker = new MouseTracker (this);
			GtkWorkarounds.FixContainerLeak (this);

			this.notebook = notebook;
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			Events |= EventMask.PointerMotionMask | EventMask.LeaveNotifyMask | EventMask.ButtonPressMask;

			var arr = new ImageView (tabbarPrevImage);
			arr.HeightRequest = arr.WidthRequest = 10;
			PreviousButton = new Button (arr);
			PreviousButton.Relief = ReliefStyle.None;
			PreviousButton.CanDefault = PreviousButton.CanFocus = false;

			arr = new ImageView (tabbarNextImage);
			arr.HeightRequest = arr.WidthRequest = 10;
			NextButton = new Button (arr);
			NextButton.Relief = ReliefStyle.None;
			NextButton.CanDefault = NextButton.CanFocus = false;

			DropDownButton = new MenuButton ();
			DropDownButton.Relief = ReliefStyle.None;
			DropDownButton.CanDefault = DropDownButton.CanFocus = false;

			PreviousButton.ShowAll ();
			NextButton.ShowAll ();
			DropDownButton.ShowAll ();

			PreviousButton.Name = "MonoDevelop.DockNotebook.BarButton";
			NextButton.Name = "MonoDevelop.DockNotebook.BarButton";
			DropDownButton.Name = "MonoDevelop.DockNotebook.BarButton";

			PreviousButton.Parent = this;
			NextButton.Parent = this;
			DropDownButton.Parent = this;

			children.Add (PreviousButton);
			children.Add (NextButton);
			children.Add (DropDownButton);

			tracker.HoveredChanged += (sender, e) => {
				if (!tracker.Hovered) {
					SetHighlightedTab (null);
					UpdateTabWidth (tabEndX - tabStartX);
					QueueDraw ();
				}
			};

			notebook.PageAdded += (sender, e) => QueueResize ();
			notebook.PageRemoved += (sender, e) => QueueResize ();

			closingTabs = new Dictionary<int, DockNotebookTab> ();
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

		protected override void ForAll (bool include_internals, Callback callback)
		{
			base.ForAll (include_internals, callback);
			foreach (var c in children.ToArray ())
				callback (c);
		}

		protected override void OnRemoved (Widget widget)
		{
			children.Remove (widget);
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			if (NavigationButtonsVisible) {
				tabStartX = /*allocation.X +*/ LeftBarPadding + LeanWidth / 2;
			} else {
				tabStartX = LeanWidth / 2;
			}
			tabEndX = allocation.Width - DropDownButton.SizeRequest ().Width;
			var height = allocation.Height - BottomBarPadding;
			if (height < 0)
				height = 0;

			PreviousButton.SizeAllocate (new Gdk.Rectangle (
				0, // allocation.X,
				0, // allocation.Y,
				LeftBarPadding / 2,
				height
			)
			);
			NextButton.SizeAllocate (new Gdk.Rectangle (
				LeftBarPadding / 2,
				0,
				LeftBarPadding / 2, height)
			);

			DropDownButton.SizeAllocate (new Gdk.Rectangle (
				tabEndX,
				allocation.Y,
				DropDownButton.SizeRequest ().Width,
				height));

			base.OnSizeAllocated (allocation);
			Update ();
		}

		int totalHeight;

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Height = totalHeight;
			requisition.Width = 0;
		}

		internal void InitSize ()
		{
			Pango.Layout la = CreateSizedLayout ();
			la.SetText ("H");
			int w, h;
			la.GetPixelSize (out w, out h);

			totalHeight = h + TopPadding + BottomPadding;
			la.Dispose ();
		}

		public int BarHeight {
			get { return totalHeight - BottomBarPadding + 1; }
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
					var img = new ImageView (draggedItem.Icon);
					box.PackStart (img, false, false, 0);
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
				if (t == null)
					return base.OnMotionNotifyEvent (evnt);
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

				var t = FindTab ((int)evnt.X, TopPadding + 3);
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

		bool overCloseOnPress;
		bool allowDoubleClick;

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			var t = FindTab ((int)evnt.X, (int)evnt.Y);
			if (t != null) {
				if (evnt.IsContextMenuButton ()) {
					DockNotebook.ActiveNotebook = notebook;
					notebook.CurrentTab = t;

                    if (notebook.DoPopupMenu != null)
					    notebook.DoPopupMenu (notebook, t.Index, evnt);
					return true;
				}
				// Don't select the tab if we are clicking the close button
				if (IsOverCloseButton (t, (int)evnt.X, (int)evnt.Y)) {
					overCloseOnPress = true;
					return true;
				}
				overCloseOnPress = false;

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

			if (!draggingTab && overCloseOnPress) {
				var t = FindTab ((int)evnt.X, (int)evnt.Y);
				if (t != null && IsOverCloseButton (t, (int)evnt.X, (int)evnt.Y)) {
					notebook.OnCloseTab (t);
					allowDoubleClick = false;
					return true;
				}
			}
			overCloseOnPress = false;
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

		protected override void OnUnrealized ()
		{
			// Cancel drag operations and animations
			buttonPressedOnTab = false;
			overCloseOnPress = false;
			allowDoubleClick = true;
			draggingTab = false;
			dragX = 0;
			this.AbortAnimation ("EndDrag");
			base.OnUnrealized ();
		}

		DockNotebookTab FindTab (int x, int y)
		{
			// we will not actually draw anything, just do bounds checking
			using (var context = CairoHelper.Create (GdkWindow)) {
				var current = notebook.CurrentTab as DockNotebookTab;
				if (current != null) {
					LayoutTabBorder (context, Allocation, current.Allocation.Width, current.Allocation.X, 0, false);
					if (context.InFill (x, y))
						return current;
				}

				context.NewPath ();
				for (int n = 0; n < notebook.Tabs.Count; n++) {
					var tab = (DockNotebookTab)notebook.Tabs [n];
					LayoutTabBorder (context, Allocation, tab.Allocation.Width, tab.Allocation.X, 0, false);
					if (context.InFill (x, y))
						return tab;
					context.NewPath ();
				}
			}
			return null;
		}

		static bool IsOverCloseButton (DockNotebookTab tab, int x, int y)
		{
			return tab != null && tab.CloseButtonAllocation.Contains (x, y);
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

		void DrawBackground (Context ctx, Gdk.Rectangle region)
		{
			var h = region.Height;
			ctx.Rectangle (0, 0, region.Width, h);
			using (var gr = new LinearGradient (0, 0, 0, h)) {
				if (isActiveNotebook) {
					gr.AddColorStop (0, Styles.TabBarActiveGradientStartColor);
					gr.AddColorStop (1, Styles.TabBarActiveGradientEndColor);
				} else {
					gr.AddColorStop (0, Styles.TabBarGradientStartColor);
					gr.AddColorStop (1, Styles.TabBarGradientEndColor);
				}
				ctx.SetSource (gr);
				ctx.Fill ();
			}

			ctx.MoveTo (region.X, 0.5);
			ctx.LineTo (region.Right + 1, 0.5);
			ctx.LineWidth = 1;
			ctx.SetSourceColor (Styles.TabBarGradientShadowColor);
			ctx.Stroke ();
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
				return c => DrawTab (c, closingTab, Allocation, new Gdk.Rectangle (region.X, region.Y, tmp, region.Height), false, false, false, CreateTabLayout (closingTab));
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
			Action<Context> drawActive = c => {
			};
			var drawCommands = new List<Action<Context>> ();
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

				int width = Math.Min (TabWidth, Math.Max (50, tabEndX - x - 1));
				if (tab == notebook.Tabs.Last ())
					width += LastTabWidthAdjustment;
				width = (int)(width * tab.WidthModifier);

				if (active) {
					int tmp = x;
					drawActive = c => DrawTab (c, tab, Allocation, new Gdk.Rectangle (tmp, y, width, Allocation.Height), true, true, draggingTab, CreateTabLayout (tab));
					tab.Allocation = new Gdk.Rectangle (tmp, Allocation.Y, width, Allocation.Height);
				} else {
					int tmp = x;
					bool highlighted = tab == highlightedTab;

					if (tab.SaveStrength > 0.0f) {
						tmp = (int)(tab.SavedAllocation.X + (tmp - tab.SavedAllocation.X) * (1.0f - tab.SaveStrength));
					}

					drawCommands.Add (c => DrawTab (c, tab, Allocation, new Gdk.Rectangle (tmp, y, width, Allocation.Height), highlighted, false, false, CreateTabLayout (tab)));
					tab.Allocation = new Gdk.Rectangle (tmp, Allocation.Y, width, Allocation.Height);
				}

				x += width;
			}

			var allocation = Allocation;
			int tabWidth;
			drawCommands.Add (DrawClosingTab (n, new Gdk.Rectangle (x, y, 0, allocation.Height), out tabWidth));
			drawCommands.Reverse ();

			DrawBackground (ctx, allocation);

			// Draw breadcrumb bar header
			if (notebook.Tabs.Count > 0) {
				ctx.Rectangle (0, allocation.Height - BottomBarPadding, allocation.Width, BottomBarPadding);
				ctx.SetSourceColor (Styles.BreadcrumbBackgroundColor);
				ctx.Fill ();
			}

			ctx.Rectangle (tabStartX - LeanWidth / 2, allocation.Y, tabArea + LeanWidth, allocation.Height);
			ctx.Clip ();

			foreach (var cmd in drawCommands)
				cmd (ctx);

			ctx.ResetClip ();

			// Redraw the dragging tab here to be sure its on top. We drew it before to get the sizing correct, this should be fixed.
			drawActive (ctx);
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			using (var context = CairoHelper.Create (evnt.Window)) {
				Draw (context);
			}
			return base.OnExposeEvent (evnt);
		}

		static void DrawCloseButton (Context context, Gdk.Point center, bool hovered, double opacity, double animationProgress)
		{
			if (hovered) {
				const double radius = 6;
				context.Arc (center.X, center.Y, radius, 0, Math.PI * 2);
				context.SetSourceRGBA (.6, .6, .6, opacity);
				context.Fill ();

				context.SetSourceRGBA (0.95, 0.95, 0.95, opacity);
				context.LineWidth = 2;

				context.MoveTo (center.X - 3, center.Y - 3);
				context.LineTo (center.X + 3, center.Y + 3);
				context.MoveTo (center.X - 3, center.Y + 3);
				context.LineTo (center.X + 3, center.Y - 3);
				context.Stroke ();
			} else {
				double lineColor = .63 - .1 * animationProgress;
				const double fillColor = .74;

				double heightMod = Math.Max (0, 1.0 - animationProgress * 2);
				context.MoveTo (center.X - 3, center.Y - 3 * heightMod);
				context.LineTo (center.X + 3, center.Y + 3 * heightMod);
				context.MoveTo (center.X - 3, center.Y + 3 * heightMod);
				context.LineTo (center.X + 3, center.Y - 3 * heightMod);

				context.LineWidth = 2;
				context.SetSourceRGBA (lineColor, lineColor, lineColor, opacity);
				context.Stroke ();

				if (animationProgress > 0.5) {
					double partialProg = (animationProgress - 0.5) * 2;
					context.MoveTo (center.X - 3, center.Y);
					context.LineTo (center.X + 3, center.Y);

					context.LineWidth = 2 - partialProg;
					context.SetSourceRGBA (lineColor, lineColor, lineColor, opacity);
					context.Stroke ();

					double radius = partialProg * 3.5;

					// Background
					context.Arc (center.X, center.Y, radius, 0, Math.PI * 2);
					context.SetSourceRGBA (fillColor, fillColor, fillColor, opacity);
					context.Fill ();

					// Inset shadow
					using (var lg = new LinearGradient (0, center.Y - 5, 0, center.Y)) {
						context.Arc (center.X, center.Y + 1, radius, 0, Math.PI * 2);
						lg.AddColorStop (0, new Cairo.Color (0, 0, 0, 0.2 * opacity));
						lg.AddColorStop (1, new Cairo.Color (0, 0, 0, 0));
						context.SetSource (lg);
						context.Stroke ();
					}

					// Outline
					context.Arc (center.X, center.Y, radius, 0, Math.PI * 2);
					context.SetSourceRGBA (lineColor, lineColor, lineColor, opacity);
					context.Stroke ();

				}
			}
		}

		void DrawTab (Context ctx, DockNotebookTab tab, Gdk.Rectangle allocation, Gdk.Rectangle tabBounds, bool highlight, bool active, bool dragging, Pango.Layout la)
		{
			// This logic is stupid to have here, should be in the caller!
			if (dragging) {
				tabBounds.X = (int)(tabBounds.X + (dragX - tabBounds.X) * dragXProgress);
				tabBounds.X = Clamp (tabBounds.X, tabStartX, tabEndX - tabBounds.Width);
			}
			int padding = LeftRightPadding;
			padding = (int)(padding * Math.Min (1.0, Math.Max (0.5, (tabBounds.Width - 30) / 70.0)));

			ctx.LineWidth = 1;
			LayoutTabBorder (ctx, allocation, tabBounds.Width, tabBounds.X, 0, active);
			ctx.ClosePath ();
			using (var gr = new LinearGradient (tabBounds.X, TopBarPadding, tabBounds.X, allocation.Bottom)) {
				if (active) {
					gr.AddColorStop (0, Styles.BreadcrumbGradientStartColor.MultiplyAlpha (tab.Opacity));
					gr.AddColorStop (1, Styles.BreadcrumbBackgroundColor.MultiplyAlpha (tab.Opacity));
				} else {
					gr.AddColorStop (0, CairoExtensions.ParseColor ("f4f4f4").MultiplyAlpha (tab.Opacity));
					gr.AddColorStop (1, CairoExtensions.ParseColor ("cecece").MultiplyAlpha (tab.Opacity));
				}
				ctx.SetSource (gr);
			}
			ctx.Fill ();

			ctx.SetSourceColor (new Cairo.Color (1, 1, 1, .5).MultiplyAlpha (tab.Opacity));
			LayoutTabBorder (ctx, allocation, tabBounds.Width, tabBounds.X, 1, active);
			ctx.Stroke ();

			ctx.SetSourceColor (Styles.BreadcrumbBorderColor.MultiplyAlpha (tab.Opacity));
			LayoutTabBorder (ctx, allocation, tabBounds.Width, tabBounds.X, 0, active);
			ctx.StrokePreserve ();

			if (tab.GlowStrength > 0) {
				Gdk.Point mouse = tracker.MousePosition;
				using (var rg = new RadialGradient (mouse.X, tabBounds.Bottom, 0, mouse.X, tabBounds.Bottom, 100)) {
					rg.AddColorStop (0, new Cairo.Color (1, 1, 1, 0.4 * tab.Opacity * tab.GlowStrength));
					rg.AddColorStop (1, new Cairo.Color (1, 1, 1, 0));

					ctx.SetSource (rg);
					ctx.Fill ();
				}
			} else {
				ctx.NewPath ();
			}

			// Render Close Button (do this first so we can tell how much text to render)

			var ch = allocation.Height - TopBarPadding - BottomBarPadding + CloseImageTopOffset;
			var crect = new Gdk.Rectangle (tabBounds.Right - padding - CloseButtonSize + 3,
				            tabBounds.Y + TopBarPadding + (ch - CloseButtonSize) / 2,
				            CloseButtonSize, CloseButtonSize);
			tab.CloseButtonAllocation = crect;
			tab.CloseButtonAllocation.Inflate (2, 2);

			bool closeButtonHovered = tracker.Hovered && tab.CloseButtonAllocation.Contains (tracker.MousePosition) && tab.WidthModifier >= 1.0f;
			bool drawCloseButton = tabBounds.Width > 60 || highlight || closeButtonHovered;
			if (drawCloseButton) {
				DrawCloseButton (ctx, new Gdk.Point (crect.X + crect.Width / 2, crect.Y + crect.Height / 2), closeButtonHovered, tab.Opacity, tab.DirtyStrength);
			}

			// Render Text
			int w = tabBounds.Width - (padding * 2 + CloseButtonSize);
			if (!drawCloseButton)
				w += CloseButtonSize;

			int textStart = tabBounds.X + padding;

			ctx.MoveTo (textStart, tabBounds.Y + TopPadding + TextOffset + VerticalTextSize);
			if (!Platform.IsMac && !Platform.IsWindows) {
				// This is a work around for a linux specific problem.
				// A bug in the proprietary ATI driver caused TAB text not to draw.
				// If that bug get's fixed remove this HACK asap.
				la.Ellipsize = Pango.EllipsizeMode.End;
				la.Width = (int)(w * Pango.Scale.PangoScale);
				ctx.SetSourceColor (tab.Notify ? new Cairo.Color (0, 0, 1) : Styles.TabBarActiveTextColor);
				Pango.CairoHelper.ShowLayoutLine (ctx, la.GetLine (0));
			} else {
				// ellipses are for space wasting ..., we cant afford that
				using (var lg = new LinearGradient (textStart + w - 5, 0, textStart + w + 3, 0)) {
					var color = tab.Notify ? new Cairo.Color (0, 0, 1) : Styles.TabBarActiveTextColor;
					color = color.MultiplyAlpha (tab.Opacity);
					lg.AddColorStop (0, color);
					color.A = 0;
					lg.AddColorStop (1, color);
					ctx.SetSource (lg);
					Pango.CairoHelper.ShowLayoutLine (ctx, la.GetLine (0));
				}
			}
			la.Dispose ();
		}

		static void LayoutTabBorder (Context ctx, Gdk.Rectangle allocation, int contentWidth, int px, int margin, bool active = true)
		{
			double x = 0.5 + (double)px;
			double y = (double)allocation.Height + 0.5 - BottomBarPadding + margin;
			double height = allocation.Height - TopBarPadding - BottomBarPadding;

			x += TabSpacing + margin;
			contentWidth -= (TabSpacing + margin) * 2;

			double rightx = x + contentWidth;

			int lean = Math.Min (LeanWidth, contentWidth / 2);
			int halfLean = lean / 2;
			const int smoothing = 2;
			if (active) {
				ctx.MoveTo (0, y + 0.5);
				ctx.LineTo (0, y);
				ctx.LineTo (x - halfLean, y);
			} else {
				ctx.MoveTo (x - halfLean, y + 0.5);
				ctx.LineTo (x - halfLean, y);
			}
			ctx.CurveTo (new PointD (x + smoothing, y),
				new PointD (x - smoothing, y - height),
				new PointD (x + halfLean, y - height));
			ctx.LineTo (rightx - halfLean, y - height);
			ctx.CurveTo (new PointD (rightx + smoothing, y - height),
				new PointD (rightx - smoothing, y),
				new PointD (rightx + halfLean, y));

			if (active) {
				ctx.LineTo (allocation.Width, y);
				ctx.LineTo (allocation.Width, y + 0.5);
			} else {
				ctx.LineTo (rightx + halfLean, y + 0.5);
			}
		}

		Pango.Layout CreateSizedLayout ()
		{
			var la = new Pango.Layout (PangoContext);
			la.FontDescription = Pango.FontDescription.FromString ("normal");
			la.FontDescription.AbsoluteSize = Pango.Units.FromPixels (VerticalTextSize);

			return la;
		}

		Pango.Layout CreateTabLayout (DockNotebookTab tab)
		{
			Pango.Layout la = CreateSizedLayout ();
			if (!string.IsNullOrEmpty (tab.Markup))
				la.SetMarkup (tab.Markup);
			else if (!string.IsNullOrEmpty (tab.Text))
				la.SetText (tab.Text);
			return la;
		}
	}
}
