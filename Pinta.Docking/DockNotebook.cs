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
using System.ComponentModel;
using System.Linq;
using Gtk;

namespace Pinta.Docking
{
	public class TabClosedEventArgs : CancelEventArgs
	{
		public TabClosedEventArgs (IDockNotebookItem item) { Item = item; }

		public IDockNotebookItem Item { get; private set; }
	}

	public class TabEventArgs : EventArgs
	{
		public TabEventArgs (IDockNotebookItem? item) { Item = item; }

		public IDockNotebookItem? Item { get; private set; }
	}

	public class DockNotebook : Gtk.Notebook
	{
		private HashSet<IDockNotebookItem> items = new HashSet<IDockNotebookItem> ();
		private bool enable_tabs = true;

		public DockNotebook ()
		{
			// Emit an event when the current tab is changed.
			SwitchPage += (o, args) => {
				var widget = args.Page;
				IDockNotebookItem? item = items.Where (i => i.Widget == widget).FirstOrDefault ();
				ActiveTabChanged?.Invoke (this, new TabEventArgs (item));
			};
		}

		/// <summary>
		/// Emitted when a tab is closed by the user.
		/// </summary>
		public event EventHandler<TabClosedEventArgs>? TabClosed;

		/// <summary>
		/// Emitted when switching to a different tab.
		/// </summary>
		public event EventHandler<TabEventArgs>? ActiveTabChanged;

		/// <summary>
		/// The items currently in the notebook.
		/// </summary>
		public IEnumerable<IDockNotebookItem> Items { get { return items; } }

		/// <summary>
		/// Whether to show the tab bar.
		/// </summary>
		public bool EnableTabs {
			get => enable_tabs;
			set {
				enable_tabs = value;
				if (items.Count > 0) {
					ShowTabs = value;
				}
			}
		}

		/// <summary>
		/// Returns the active notebook item.
		/// </summary>
		public IDockNotebookItem? ActiveItem {
			get {
				var current = CurrentPageWidget;
				return items.Where (i => i.Widget == current).FirstOrDefault ();
			}
			set {
				var idx = PageNum (value!.Widget);
				if (idx >= 0)
					CurrentPage = idx;
			}
		}

		public void InsertTab (IDockNotebookItem item, int position)
		{
			var tab_layout = new HBox ();
			var label_widget = new Label (item.Label);
			item.LabelChanged += (o, args) => { label_widget.Text = item.Label; };

			var close_button = new Button ("window-close-symbolic", IconSize.SmallToolbar) {
				Relief = ReliefStyle.None
			};
			close_button.Clicked += (sender, args) => {
				CloseTab (item);
			};

			tab_layout.PackStart (label_widget, false, false, 0);
			tab_layout.PackStart (close_button, false, false, 0);

			// Use an event box to grab mouse events.
			var tab_box = new EventBox () {
				Events = Gdk.EventMask.ButtonReleaseMask,
				VisibleWindow = false
			};
			tab_box.Add (tab_layout);
			tab_box.ShowAll ();

			// Allow closing via MMB-click.
			tab_box.ButtonReleaseEvent += (o, e) => {
				if (e.Event.Button == 2) {
					CloseTab (item);
				}
			};

			InsertPage (item.Widget, tab_box, position);
			SetTabReorderable (item.Widget, true);

			items.Add (item);

			ShowTabs = EnableTabs;
		}

		public void RemoveTab (IDockNotebookItem item)
		{
			int idx = PageNum (item.Widget);
			if (idx < 0)
				throw new ArgumentException ("The item is not in the notebook", nameof (item));

			RemovePage (idx);

			items.Remove (item);

			// Hide the tab bar when the notebook is empty to avoid extra borders
			// This also seems to avoid the white background issue from bug #1956030
			if (items.Count == 0)
				ShowTabs = false;
		}

		/// <summary>
		/// Prompts the user to save unsaved changes before closing.
		/// </summary>
		private bool CloseTab (IDockNotebookItem item)
		{
			var e = new TabClosedEventArgs (item);

			TabClosed?.Invoke (this, e);
			if (!e.Cancel && PageNum (item.Widget) >= 0) {
				RemoveTab (item);
				return true;
			}

			return false;
		}
	}
}
