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

using System.Collections.Generic;
using System.Linq;
using Gtk;
using Pinta.Core;

namespace Pinta.Docking;

public sealed class DockPanel : Box
{
	private sealed class DockPanelItem
	{
		public DockPanelItem (DockItem item)
		{
			Item = item;
			Pane = Paned.New (Orientation.Vertical);

			var icon = Image.NewFromIconName (item.IconName);
			ReopenButton = ToggleButton.New ();
			ReopenButton.TooltipText = item.Label;
			ReopenButton.SetChild (icon);

			// Autohide is set to false since it seems to cause the popover to close even when clicking inside it, on macOS at least
			// Instead, the reopen button is a toggle button to close the popover.
			popover = new Popover () {
				Autohide = false,
				Position = PositionType.Left
			};
			popover.SetParent (ReopenButton);

			ReopenButton.OnToggled += (o, args) => {
				if (ReopenButton.Active)
					popover.Popup ();
				else
					popover.Popdown ();
			};
		}

		public DockItem Item { get; }
		public Paned Pane { get; }
		public ToggleButton ReopenButton { get; }
		private readonly Popover popover;

		public bool IsMinimized => popover.Child != null;

		public void UpdateOnMaximize (Box dock_bar)
		{
			// Remove the reopen button from the dock bar.
			// Note that it might not already be in the dock bar, e.g. on startup.
			dock_bar.RemoveIfChild (ReopenButton);

			popover.Popdown ();
			popover.Child = null;

			Pane.StartChild = Item;
			Pane.ResizeStartChild = false;
			Pane.ShrinkStartChild = false;
		}

		public void UpdateOnMinimize (Box dock_bar)
		{
			Pane.StartChild = null;
			popover.Child = Item;

			dock_bar.Append (ReopenButton);
			ReopenButton.Active = false;
		}
	}

	/// <summary>
	/// Contains the buttons to re-open any minimized dock items.
	/// </summary>
	private readonly Box dock_bar = Box.New (Orientation.Vertical, 0);

	/// <summary>
	/// List of the items in this panel, which may be minimized or maximized.
	/// </summary>
	private readonly List<DockPanelItem> items = [];

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
		panel_item.UpdateOnMaximize (dock_bar);

		item.MinimizeClicked += (o, args) => {
			panel_item.UpdateOnMinimize (dock_bar);
		};
		item.MaximizeClicked += (o, args) => {
			panel_item.UpdateOnMaximize (dock_bar);
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
				panel_item.Item.Minimize ();
			}

#if false
			panel_item.Pane.Position = settings.GetSetting<int> (
				SplitPosKey (panel_item), panel_item.Pane.Position);
#endif
		}
	}

	private static string BaseSettingKey (DockPanelItem item) => $"dock-{item.Item.UniqueName.ToLower ()}";
	private static string MinimizeKey (DockPanelItem item) => BaseSettingKey (item) + "-minimized";
	private static string SplitPosKey (DockPanelItem item) => BaseSettingKey (item) + "-splitpos";
}
