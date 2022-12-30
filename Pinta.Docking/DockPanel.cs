//  
// Author:
//       Cameron White <cameronwhite91@gmail.com>
// 
// Copyright (c) 2020 Cameron White
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

using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using Pinta.Core;

namespace Pinta.Docking
{
	public class DockPanel : Box
	{
		private class DockPanelItem
		{
			public DockPanelItem (DockItem item)
			{
				Item = item;
				Pane = Paned.New (Orientation.Vertical);

				var label = Label.New (item.Label);
				// TODO-GTK4 - figure out how to set angle to 270 degrees for vertical text
				ReopenButton = Button.New ();
				ReopenButton.SetChild (label);

				popover = new Popover () { Child = ReopenButton };
				popover.Position = PositionType.Left;

				ReopenButton.OnClicked += (o, args) => {
					popover.Show ();
					popover.Popup ();
				};
			}

			public DockItem Item { get; private set; }
			public Paned Pane { get; private set; }
			public Button ReopenButton { get; private set; }
			private Popover popover;

			public bool IsMinimized => popover.Child != null;

			public void Maximize (Box dock_bar)
			{
				dock_bar.Remove (ReopenButton);
				popover.Hide ();
				popover.Child = null;

				Pane.StartChild = Item;
				Pane.ResizeStartChild = false;
				Pane.ShrinkStartChild = false;

				Item.Maximize ();
			}

			public void Minimize (Box dock_bar)
			{
				Pane.StartChild = null;
				popover.Child = Item;

				dock_bar.Append (ReopenButton);
				ReopenButton.Show ();

				Item.Minimize ();
			}
		}

		/// <summary>
		/// Contains the buttons to re-open any minimized dock items.
		/// </summary>
		private Box dock_bar = Box.New (Orientation.Vertical, 0);

		/// <summary>
		/// List of the items in this panel, which may be minimized or maximized.
		/// </summary>
		private List<DockPanelItem> items = new List<DockPanelItem> ();

		public DockPanel ()
		{
			SetOrientation (Orientation.Horizontal);
			Append (dock_bar);
		}

		public void AddItem (DockItem item)
		{
			var panel_item = new DockPanelItem (item);

			// Connect to the previous pane in the list.
			if (items.Count > 0) {
				var pane = items.Last ().Pane;
				pane.EndChild = panel_item.Pane;
			} else {
				panel_item.Pane.Hexpand = true;
				panel_item.Pane.Halign = Align.Fill;
				Prepend (panel_item.Pane);
			}

			items.Add (panel_item);
			panel_item.Maximize (dock_bar);

			item.MinimizeClicked += (o, args) => {
				panel_item.Minimize (dock_bar);
			};
			item.MaximizeClicked += (o, args) => {
				panel_item.Maximize (dock_bar);
			};
		}

		public void SaveSettings (ISettingsService settings)
		{
			foreach (var panel_item in items) {
				settings.PutSetting (MinimizeKey (panel_item), panel_item.IsMinimized);
#if false
				settings.PutSetting (SplitPosKey (panel_item), panel_item.Pane.Position);
#endif
			}
		}

		public void LoadSettings (ISettingsService settings)
		{
			foreach (var panel_item in items) {
				if (settings.GetSetting<bool> (MinimizeKey (panel_item), false)) {
					panel_item.Minimize (dock_bar);
				}

#if false
				panel_item.Pane.Position = settings.GetSetting<int> (
					SplitPosKey (panel_item), panel_item.Pane.Position);
#endif
			}
		}

		private string BaseSettingKey (DockPanelItem item) => $"dock-{item.Item.UniqueName.ToLower ()}";
		private string MinimizeKey (DockPanelItem item) => BaseSettingKey (item) + "-minimized";
		private string SplitPosKey (DockPanelItem item) => BaseSettingKey (item) + "-splitpos";
	}
}
