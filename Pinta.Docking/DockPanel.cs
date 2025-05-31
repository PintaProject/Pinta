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
using Pinta.Core;

namespace Pinta.Docking;

public sealed class DockPanel : Gtk.Box
{
	internal sealed class DockPanelItem
	{
		public DockItem Item { get; }
		public Gtk.Paned Pane { get; }
		public Gtk.ToggleButton ReopenButton { get; }
		private readonly Gtk.Popover popover;
		public DockPanelItem (DockItem item)
		{
			Gtk.Paned pane = Gtk.Paned.New (Gtk.Orientation.Vertical);

			Gtk.Image icon = Gtk.Image.NewFromIconName (item.IconName);

			Gtk.ToggleButton reopenButton = Gtk.ToggleButton.New ();
			reopenButton.TooltipText = item.Label;
			reopenButton.SetChild (icon);
			reopenButton.OnToggled += ReopenButton_OnToggled;

			// Autohide is set to false since it seems to cause the popover to close even when clicking inside it, on macOS at least
			// Instead, the reopen button is a toggle button to close the popover.
			Gtk.Popover popover = new () {
				Autohide = false,
				Position = Gtk.PositionType.Left,
			};
			popover.SetParent (reopenButton);

			// --- References to keep

			this.popover = popover;
			Pane = pane;
			ReopenButton = reopenButton;
			Item = item;
		}

		private void ReopenButton_OnToggled (Gtk.ToggleButton _, EventArgs __)
		{
			if (ReopenButton.Active)
				popover.Popup ();
			else
				popover.Popdown ();
		}

		public bool IsMinimized
			=> popover.Child is not null;

		public void UpdateOnMaximize (Gtk.Box dockBar, Action updateConnections)
		{
			// Remove the reopen button from the dock bar.
			// Note that it might not already be in the dock bar, e.g. on startup.
			dockBar.RemoveIfChild (ReopenButton);

			popover.Popdown ();
			popover.Child = null;

			Pane.StartChild = Item;
			Pane.ResizeStartChild = false;
			Pane.ShrinkStartChild = false;

			updateConnections ();
		}

		public void UpdateOnMinimize (Gtk.Box dock_bar, Action updateConnections)
		{
			Pane.StartChild = null;
			popover.Child = Item;

			dock_bar.Append (ReopenButton);
			ReopenButton.Active = false;

			updateConnections ();
		}
	}

	/// <summary>
	/// Contains the buttons to re-open any minimized dock items.
	/// </summary>
	private readonly Gtk.Box dock_bar = Gtk.Box.New (Gtk.Orientation.Vertical, 0);

	/// <summary>
	/// List of the items in this panel, which may be minimized or maximized.
	/// </summary>
	private readonly List<DockPanelItem> items = [];

	public DockPanel ()
	{
		SetOrientation (Gtk.Orientation.Horizontal);
		Append (dock_bar);
	}

	public void AddItem (DockItem item)
	{
		DockPanelItem panelItem = new (item);

		// Connect to the previous pane in the list.
		if (items.Count > 0) {
			Gtk.Paned pane = items.Last ().Pane;
			pane.EndChild = panelItem.Pane;
		} else {
			panelItem.Pane.Hexpand = true;
			panelItem.Pane.Halign = Gtk.Align.Fill;
			Prepend (panelItem.Pane);
		}

		items.Add (panelItem);
		panelItem.UpdateOnMaximize (dock_bar, UpdatePaneConnections);

		item.MinimizeClicked += (_, _) => panelItem.UpdateOnMinimize (dock_bar, UpdatePaneConnections);
		item.MaximizeClicked += (_, _) => panelItem.UpdateOnMaximize (dock_bar, UpdatePaneConnections);
	}

	public void SaveSettings (ISettingsService settings)
	{
		foreach (var panel_item in items) {
			settings.PutSetting (SettingNames.MinimizeKey (panel_item), panel_item.IsMinimized);
#if false
			settings.PutSetting (SplitPosKey (panel_item), panel_item.Pane.Position);
#endif
		}
	}

	public void LoadSettings (ISettingsService settings)
	{
		foreach (var panel_item in items) {

			if (settings.GetSetting<bool> (SettingNames.MinimizeKey (panel_item), false)) {
				panel_item.Item.Minimize ();
			}

#if false
			panel_item.Pane.Position = settings.GetSetting<int> (
				SplitPosKey (panel_item), panel_item.Pane.Position);
#endif
		}
	}

	private void UpdatePaneConnections()
	{
		// Get all maximized (visible) items
		var maximizedItems = items.Where(item => !item.IsMinimized).ToArray();
		
		// Reset all EndChild connections
		foreach (var item in items)
		{
			item.Pane.EndChild = null;
		}
		
		// Remove all panes from the container
		var currentChild = GetFirstChild();
		while (currentChild != null && currentChild != dock_bar)
		{
			var next = currentChild.GetNextSibling();
			Remove(currentChild);
			currentChild = next;
		}
		
		if (maximizedItems.Length > 0)
		{
			// Rebuild the chain connecting only maximized panes
			for (int i = 0; i < maximizedItems.Length - 1; i++)
			{
				maximizedItems[i].Pane.EndChild = maximizedItems[i + 1].Pane;
			}
			
			// Add the root pane (first maximized pane) back to the container
			var rootPane = maximizedItems[0].Pane;
			rootPane.Hexpand = true;
			rootPane.Halign = Gtk.Align.Fill;
			Prepend(rootPane);
		}
	}
}
