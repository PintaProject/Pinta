//
// TabStrip.cs
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

using Gtk; 

using System;

namespace MonoDevelop.Components.Docking
{
	class TabStrip: Notebook
	{
		int currentTab = -1;
		bool ellipsized = true;
		HBox box = new HBox ();
		DockFrame frame;
		Label bottomFiller = new Label ();
		
		public TabStrip (DockFrame frame)
		{
			this.frame = frame;
			frame.ShadedContainer.Add (this);
			VBox vbox = new VBox ();
			box = new HBox ();
			vbox.PackStart (box, false, false, 0);
			vbox.PackStart (bottomFiller, false, false, 0);
			AppendPage (vbox, null);
			ShowBorder = false;
			ShowTabs = false;
			ShowAll ();
			bottomFiller.Hide ();
			BottomPadding = 3;
		}
		
		public int BottomPadding {
			get { return bottomFiller.HeightRequest; }
			set {
				bottomFiller.HeightRequest = value;
				bottomFiller.Visible = value > 0;
			}
		}
		
		public void AddTab (Gtk.Widget page, Gdk.Pixbuf icon, string label)
		{
			Tab tab = new Tab ();
			tab.SetLabel (page, icon, label);
			tab.ShowAll ();
			box.PackStart (tab, true, true, 0);
			if (currentTab == -1)
				CurrentTab = box.Children.Length - 1;
			else {
				tab.Active = false;
				page.Hide ();
			}
			
			tab.ButtonPressEvent += OnTabPress;
		}
		
		public void SetTabLabel (Gtk.Widget page, Gdk.Pixbuf icon, string label)
		{
			foreach (Tab tab in box.Children) {
				if (tab.Page == page) {
					tab.SetLabel (page, icon, label);
					UpdateEllipsize (Allocation);
					break;
				}
			}
		}
		
		public int TabCount {
			get { return box.Children.Length; }
		}
		
		public int CurrentTab {
			get { return currentTab; }
			set {
				if (currentTab == value)
					return;
				if (currentTab != -1) {
					Tab t = (Tab) box.Children [currentTab];
					t.Page.Hide ();
					t.Active = false;
				}
				currentTab = value;
				if (currentTab != -1) {
					Tab t = (Tab) box.Children [currentTab];
					t.Active = true;
					t.Page.Show ();
				}
			}
		}
		
		new public Gtk.Widget CurrentPage {
			get {
				if (currentTab != -1) {
					Tab t = (Tab) box.Children [currentTab];
					return t.Page;
				} else
					return null;
			}
			set {
				if (value != null) {
					Gtk.Widget[] tabs = box.Children;
					for (int n = 0; n < tabs.Length; n++) {
						Tab tab = (Tab) tabs [n];
						if (tab.Page == value) {
							CurrentTab = n;
							return;
						}
					}
				}
				CurrentTab = -1;
			}
		}
		
		public void Clear ()
		{
			ellipsized = true;
			currentTab = -1;
			foreach (Widget w in box.Children) {
				box.Remove (w);
				w.Destroy ();
			}
		}
		
		void OnTabPress (object s, Gtk.ButtonPressEventArgs args)
		{
			CurrentTab = Array.IndexOf (box.Children, s);
			Tab t = (Tab) s;
			DockItem.SetFocus (t.Page);
			QueueDraw ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			UpdateEllipsize (allocation);
			base.OnSizeAllocated (allocation);
		}
		
		void UpdateEllipsize (Gdk.Rectangle allocation)
		{
			int tsize = 0;
			foreach (Tab tab in box.Children)
				tsize += tab.LabelWidth;

			bool ellipsize = tsize > allocation.Width;
			if (ellipsize != ellipsized) {
				foreach (Tab tab in box.Children) {
					tab.SetEllipsize (ellipsize);
					Gtk.Box.BoxChild bc = (Gtk.Box.BoxChild) box [tab];
					bc.Expand = bc.Fill = ellipsize;
				}
				ellipsized = ellipsize;
			}
		}

		public Gdk.Rectangle GetTabArea (int ntab)
		{
			Gtk.Widget[] tabs = box.Children;
			Tab tab = (Tab) tabs[ntab];
			Gdk.Rectangle rect = GetTabArea (tab, ntab);
			int x, y;
			tab.GdkWindow.GetRootOrigin (out x, out y);
			rect.X += x;
			rect.Y += y;
			return rect;
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			frame.ShadedContainer.DrawBackground (this);

			Gtk.Widget[] tabs = box.Children;
			for (int n=tabs.Length - 1; n>=0; n--) {
				Tab tab = (Tab) tabs [n];
				if (n != currentTab)
					DrawTab (evnt, tab, n);
			}
			if (currentTab != -1) {
				Tab ctab = (Tab) tabs [currentTab];
//				GdkWindow.DrawLine (Style.DarkGC (Gtk.StateType.Normal), Allocation.X, Allocation.Y, Allocation.Right, Allocation.Y);
				DrawTab (evnt, ctab, currentTab);
			}
			return base.OnExposeEvent (evnt);
		}

		public Gdk.Rectangle GetTabArea (Tab tab, int pos)
		{
			Gdk.Rectangle rect = tab.Allocation;

			int xdif = 0;
			if (pos > 0)
				xdif = 2;

			int reqh;
//			StateType st;

			if (tab.Active) {
//				st = StateType.Normal;
				reqh = tab.Allocation.Height;
			}
			else {
				reqh = tab.Allocation.Height - 3;
//				st = StateType.Active;
			}

			if (DockFrame.IsWindows) {
				rect.Height = reqh - 1;
				rect.Width--;
				if (pos > 0) {
					rect.X--;
					rect.Width++;
				}
				return rect;
			}
			else {
				rect.X -= xdif;
				rect.Width += xdif;
				rect.Height = reqh;
				return rect;
			}
		}

		void DrawTab (Gdk.EventExpose evnt, Tab tab, int pos)
		{
			Gdk.Rectangle rect = GetTabArea (tab, pos);
			StateType st;
			if (tab.Active)
				st = StateType.Normal;
			else
				st = StateType.Active;

			if (DockFrame.IsWindows) {
				GdkWindow.DrawRectangle (Style.DarkGC (Gtk.StateType.Normal), false, rect);
				rect.X++;
				rect.Width--;
				if (tab.Active) {
					GdkWindow.DrawRectangle (Style.LightGC (Gtk.StateType.Normal), true, rect);
				}
				else {
					using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
						cr.NewPath ();
						cr.MoveTo (rect.X, rect.Y);
						cr.RelLineTo (rect.Width, 0);
						cr.RelLineTo (0, rect.Height);
						cr.RelLineTo (-rect.Width, 0);
						cr.RelLineTo (0, -rect.Height);
						cr.ClosePath ();
						Cairo.Gradient pat = new Cairo.LinearGradient (rect.X, rect.Y, rect.X, rect.Y + rect.Height);
						Cairo.Color color1 = DockFrame.ToCairoColor (Style.Mid (Gtk.StateType.Normal));
						pat.AddColorStop (0, color1);
						color1.R *= 1.2;
						color1.G *= 1.2;
						color1.B *= 1.2;
						pat.AddColorStop (1, color1);
						cr.Pattern = pat;
						cr.FillPreserve ();
					}
				}
			}
			else
				Gtk.Style.PaintExtension (Style, GdkWindow, st, ShadowType.Out, evnt.Area, this, "tab", rect.X, rect.Y, rect.Width, rect.Height, Gtk.PositionType.Top); 
		}
	}
	
	class Tab: Gtk.EventBox
	{
		bool active;
		Gtk.Widget page;
		Gtk.Label labelWidget;
		int labelWidth;
		
		const int TopPadding = 2;
		const int BottomPadding = 4;
		const int TopPaddingActive = 3;
		const int BottomPaddingActive = 5;
		const int HorzPadding = 5;
		
		public Tab ()
		{
			this.VisibleWindow = false;
		}
		
		public void SetLabel (Gtk.Widget page, Gdk.Pixbuf icon, string label)
		{
			Pango.EllipsizeMode oldMode = Pango.EllipsizeMode.End;
			
			this.page = page;
			if (Child != null) {
				if (labelWidget != null)
					oldMode = labelWidget.Ellipsize;
				Gtk.Widget oc = Child;
				Remove (oc);
				oc.Destroy ();
			}
			
			Gtk.HBox box = new HBox ();
			box.Spacing = 2;
			
			if (icon != null)
				box.PackStart (new Gtk.Image (icon), false, false, 0);

			if (!string.IsNullOrEmpty (label)) {
				labelWidget = new Gtk.Label (label);
				labelWidget.UseMarkup = true;
				box.PackStart (labelWidget, true, true, 0);
			} else {
				labelWidget = null;
			}
			
			Add (box);
			
			// Get the required size before setting the ellipsize property, since ellipsized labels
			// have a width request of 0
			ShowAll ();
			labelWidth = SizeRequest ().Width;
			
			if (labelWidget != null)
				labelWidget.Ellipsize = oldMode;
		}
		
		public void SetEllipsize (bool elipsize)
		{
			if (labelWidget != null) {
				if (elipsize)
					labelWidget.Ellipsize = Pango.EllipsizeMode.End;
				else
					labelWidget.Ellipsize = Pango.EllipsizeMode.None;
			}
		}
		
		public int LabelWidth {
			get { return labelWidth; }
		}
		
		public bool Active {
			get {
				return active;
			}
			set {
				active = value;
				this.QueueResize ();
				QueueDraw ();
			}
		}

		public Widget Page {
			get {
				return page;
			}
		}
		
		protected override void OnSizeRequested (ref Gtk.Requisition req)
		{
			req = Child.SizeRequest ();
			req.Width += HorzPadding * 2;
			if (active)
				req.Height += TopPaddingActive + BottomPaddingActive;
			else
				req.Height += TopPadding + BottomPadding;
		}
					
		protected override void OnSizeAllocated (Gdk.Rectangle rect)
		{
			base.OnSizeAllocated (rect);
			
			rect.X += HorzPadding;
			rect.Width -= HorzPadding * 2;
			
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
}
