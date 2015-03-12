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

namespace Pinta.Docking
{
	class DockItemContainer: EventBox
	{
		DockItem item;
		Widget widget;
		Container borderFrame;
		Box contentBox;
		VBox mainBox;

		public DockItemContainer (DockFrame frame, DockItem item)
		{
			this.item = item;

			mainBox = new VBox ();
			Add (mainBox);

			mainBox.ResizeMode = Gtk.ResizeMode.Queue;
			mainBox.Spacing = 0;
			
			ShowAll ();
			
			mainBox.PackStart (item.GetToolbar (PositionType.Top).Container, false, false, 0);
			
			HBox hbox = new HBox ();
			hbox.Show ();
			hbox.PackStart (item.GetToolbar (PositionType.Left).Container, false, false, 0);
			
			contentBox = new HBox ();
			contentBox.Show ();
			hbox.PackStart (contentBox, true, true, 0);
			
			hbox.PackStart (item.GetToolbar (PositionType.Right).Container, false, false, 0);
			
			mainBox.PackStart (hbox, true, true, 0);
			
			mainBox.PackStart (item.GetToolbar (PositionType.Bottom).Container, false, false, 0);
		}

		DockVisualStyle visualStyle;

		public DockVisualStyle VisualStyle {
			get { return visualStyle; }
			set { visualStyle = value; UpdateVisualStyle (); }
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
			UpdateVisualStyle ();
		}

		void UpdateVisualStyle ()
		{
			if (VisualStyle != null) {
				if (widget != null)
					SetTreeStyle (widget);

				item.GetToolbar (PositionType.Top).SetStyle (VisualStyle);
				item.GetToolbar (PositionType.Left).SetStyle (VisualStyle);
				item.GetToolbar (PositionType.Right).SetStyle (VisualStyle);
				item.GetToolbar (PositionType.Bottom).SetStyle (VisualStyle);
			}
		}

		void SetTreeStyle (Gtk.Widget w)
		{
			if (w is Gtk.TreeView) {
				if (w.IsRealized)
					OnTreeRealized (w, null);
				else
					w.Realized += OnTreeRealized;
			}
			else {
				var c = w as Gtk.Container;
				if (c != null) {
					foreach (var cw in c.Children)
						SetTreeStyle (cw);
				}
			}
		}

		void OnTreeRealized (object sender, EventArgs e)
		{
			var w = (Gtk.TreeView)sender;
			if (VisualStyle.TreeBackgroundColor != null) {
				w.ModifyBase (StateType.Normal, VisualStyle.TreeBackgroundColor.Value);
				w.ModifyBase (StateType.Insensitive, VisualStyle.TreeBackgroundColor.Value);
			} else {
				w.ModifyBase (StateType.Normal, Parent.Style.Base (StateType.Normal));
				w.ModifyBase (StateType.Insensitive, Parent.Style.Base (StateType.Insensitive));
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (VisualStyle.TabStyle == DockTabStyle.Normal) {
				Gdk.GC gc = new Gdk.GC (GdkWindow);
				gc.RgbFgColor = VisualStyle.PadBackgroundColor.Value;
				evnt.Window.DrawRectangle (gc, true, Allocation);
				gc.Dispose ();
			}
			return base.OnExposeEvent (evnt);
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

		Gdk.Color backgroundColor;
		bool backgroundColorSet;
		
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

		public Gdk.Color BackgroundColor {
			get { return backgroundColor; }
			set { backgroundColor = value; backgroundColorSet = true; }
		}

		protected override void OnAdded (Widget widget)
		{
			base.OnAdded (widget);
			child = widget;
		}
		
		protected override void OnRemoved (Widget widget)
		{
			base.OnRemoved (widget);
			child = null;
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			if (child != null) {
				requisition = child.SizeRequest ();
				requisition.Width += leftMargin + rightMargin + leftPadding + rightPadding;
				requisition.Height += topMargin + bottomMargin + topPadding + bottomPadding;
			} else {
				requisition.Width = 0;
				requisition.Height = 0;
			}
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
			if (child != null)
				child.SizeAllocate (allocation);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			Gdk.Rectangle rect = Allocation;
			
			//Gdk.Rectangle.Right and Bottom are inconsistent
			int right = rect.X + rect.Width, bottom = rect.Y + rect.Height;

			var bcolor = backgroundColorSet ? BackgroundColor : Style.Background (Gtk.StateType.Normal);
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
			
				if (GradientBackround) {
					cr.NewPath ();
					cr.MoveTo (rect.X, rect.Y);
					cr.RelLineTo (rect.Width, 0);
					cr.RelLineTo (0, rect.Height);
					cr.RelLineTo (-rect.Width, 0);
					cr.RelLineTo (0, -rect.Height);
					cr.ClosePath ();
					using (Cairo.Gradient pat = new Cairo.LinearGradient (rect.X, rect.Y, rect.X, bottom)) {
						pat.AddColorStop (0, bcolor.ToCairoColor ());
						Xwt.Drawing.Color gcol = bcolor.ToXwtColor ();
						gcol.Light -= 0.1;
						if (gcol.Light < 0)
							gcol.Light = 0;
						pat.AddColorStop (1, gcol.ToCairoColor ());
						cr.SetSource (pat);
						cr.Fill ();
					}
				} else {
					if (backgroundColorSet) {
						Gdk.GC gc = new Gdk.GC (GdkWindow);
						gc.RgbFgColor = bcolor;
						evnt.Window.DrawRectangle (gc, true, rect.X, rect.Y, rect.Width, rect.Height);
						gc.Dispose ();
					}
				}
			
			}
			base.OnExposeEvent (evnt);

			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				cr.SetSourceColor (Style.Dark (Gtk.StateType.Normal).ToCairoColor ());
				
				double y = rect.Y + topMargin / 2d;
				cr.LineWidth = topMargin;
				cr.Line (rect.X, y, right, y);
				cr.Stroke ();
				
				y = bottom - bottomMargin / 2d;
				cr.LineWidth = bottomMargin;
				cr.Line (rect.X, y, right, y);
				cr.Stroke ();
				
				double x = rect.X + leftMargin / 2d;
				cr.LineWidth = leftMargin;
				cr.Line (x, rect.Y, x, bottom);
				cr.Stroke ();
				
				x = right - rightMargin / 2d;
				cr.LineWidth = rightMargin;
				cr.Line (x, rect.Y, x, bottom);
				cr.Stroke ();

				return false;
			}
		}
	}
}
