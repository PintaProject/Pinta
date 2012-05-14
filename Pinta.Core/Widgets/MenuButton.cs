//
// MenuButton.cs
//
// Author:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (c) 2008 Scott Peterson
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
//

using System;
using Gtk;
using Gdk;

namespace Hyena.Widgets
{
	public class MenuButton : ToolItem
	{
		private ToggleButton toggle_button = new ToggleButton ();
		private HBox box = new HBox ();
		private Alignment alignment;
		private Arrow arrow;
		private Widget button_widget;
		private Menu menu;
		private Widget size_widget;
		protected Gtk.Label label_widget;

		protected MenuButton (IntPtr ptr) : base (ptr) { }

		public MenuButton ()
		{
		}

		public MenuButton (Widget buttonWidget, Menu menu, bool showArrow)
		{
			Construct (buttonWidget, menu, showArrow);
		}

		protected void Construct (Widget buttonWidget, Menu menu, bool showArrow, bool showLabel = false)
		{
			WidgetFlags |= WidgetFlags.NoWindow;

			button_widget = buttonWidget;

			Menu = menu;

			toggle_button.Parent = this;
			toggle_button.FocusOnClick = false;
			toggle_button.Relief = ReliefStyle.None;
			toggle_button.Pressed += delegate { ShowMenu (); toggle_button.Active = true; };
			toggle_button.Activated += delegate { ShowMenu (); };

			box.Parent = this;

			if (showArrow) {
				if (showLabel) {
					box.PackStart (button_widget, true, true, 3);
					label_widget = new Gtk.Label ();
					box.PackStart (label_widget, true, false, 2);
				} else {
					box.PackStart (button_widget, true, true, 5);
				}

				alignment = new Alignment (0f, 0.5f, 0f, 0f);
				arrow = new Arrow (ArrowType.Down, ShadowType.None);
				alignment.Add (arrow);
				box.PackStart (alignment, false, false, 0);
				size_widget = box;
				FocusChain = new Widget[] { toggle_button, box };
				alignment.ShowAll ();
				alignment.NoShowAll = true;
			} else {
				toggle_button.Add (button_widget);
				size_widget = toggle_button;
			}

			ShowAll ();
		}

		public Widget ButtonWidget
		{
			get { return button_widget; }
		}

		public Menu Menu
		{
			get { return menu; }
			set
			{
				if (menu == value)
					return;

				if (menu != null)
					menu.Deactivated -= OnMenuDeactivated;

				menu = value;
				menu.Deactivated += OnMenuDeactivated;
			}
		}

		private void OnMenuDeactivated (object o, EventArgs args)
		{
			toggle_button.Active = false;
		}

		public ToggleButton ToggleButton
		{
			get { return toggle_button; }
		}

		public Arrow Arrow
		{
			get { return arrow; }
		}

		public bool ArrowVisible
		{
			get { return alignment.Visible; }
			set { alignment.Visible = value; }
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition = size_widget.SizeRequest ();
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			box.SizeAllocate (allocation);
			toggle_button.SizeAllocate (allocation);
			base.OnSizeAllocated (allocation);
		}

		protected override void ForAll (bool include_internals, Callback callback)
		{
			callback (toggle_button);
			callback (box);
		}

		protected override void OnAdded (Widget widget)
		{
		}

		protected override void OnRemoved (Widget widget)
		{
		}

		protected void ShowMenu ()
		{
			menu.Popup (null, null, PositionMenu, 1, Gtk.Global.CurrentEventTime);
		}

		private void PositionMenu (Menu menu, out int x, out int y, out bool push_in)
		{
			Gtk.Requisition menu_req = menu.SizeRequest ();
			int monitor_num = Screen.GetMonitorAtWindow (GdkWindow);
			Gdk.Rectangle monitor = Screen.GetMonitorGeometry (monitor_num < 0 ? 0 : monitor_num);

			GdkWindow.GetOrigin (out x, out y);

			y += Allocation.Y;
			x += Allocation.X + (Direction == TextDirection.Ltr
			    ? Math.Max (Allocation.Width - menu_req.Width, 0)
			    : -(menu_req.Width - Allocation.Width));

			if (y + Allocation.Height + menu_req.Height <= monitor.Y + monitor.Height) {
				y += Allocation.Height;
			} else if (y - menu_req.Height >= monitor.Y) {
				y -= menu_req.Height;
			} else if (monitor.Y + monitor.Height - (y + Allocation.Height) > y) {
				y += Allocation.Height;
			} else {
				y -= menu_req.Height;
			}

			push_in = false;
		}
	}
}
