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
using Pinta.Core;

namespace Pinta.Docking;

public sealed class TabClosedEventArgs : CancelEventArgs
{
	public TabClosedEventArgs (IDockNotebookItem item) { Item = item; }

	public IDockNotebookItem Item { get; }
}

public sealed class TabEventArgs : EventArgs
{
	public TabEventArgs (IDockNotebookItem? item) { Item = item; }

	public IDockNotebookItem? Item { get; }
}

public sealed class DockNotebook : Box
{
	private readonly Adw.TabView tab_view = new ();
	private readonly Adw.TabBar tab_bar = new ();
	private readonly HashSet<IDockNotebookItem> items = [];

	public DockNotebook ()
	{
		SetOrientation (Orientation.Vertical);

		tab_bar.SetView (tab_view);
		tab_bar.Autohide = true;
		tab_bar.AddCssClass (AdwaitaStyles.Inline);
		tab_bar.ExpandTabs = true;

		tab_view.Vexpand = true;
		tab_view.Valign = Align.Fill;

		Append (tab_bar);
		Append (tab_view);

		// Emit an event when the current tab is changed.
		Adw.TabView.SelectedPagePropertyDefinition.Notify (tab_view, (_, _) => {
			Adw.TabPage? page = tab_view.SelectedPage;
			IDockNotebookItem? item = FindItemForPage (page);
			ActiveTabChanged?.Invoke (this, new TabEventArgs (item));
		});

		// Prompt the user to save unsaved changes before closing.
		tab_view.OnClosePage += (o, e) => {
			var page = e.Page;
			IDockNotebookItem item = FindItemForPage (page)!;

			var close_args = new TabClosedEventArgs (item);
			TabClosed?.Invoke (this, close_args);

			tab_view.ClosePageFinish (page, !close_args.Cancel);
			if (!close_args.Cancel)
				items.Remove (item);

			// Prevent the default close handler from running.
			return Gdk.Constants.EVENT_STOP;
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
	public IEnumerable<IDockNotebookItem> Items => items;

	/// <summary>
	/// Whether to show the tab bar.
	/// </summary>
	public bool EnableTabs {
		get => tab_bar.IsVisible ();
		set {
			if (value)
				tab_bar.Show ();
			else
				tab_bar.Hide ();
		}
	}

	/// <summary>
	/// Returns the active notebook item.
	/// </summary>
	public IDockNotebookItem? ActiveItem {
		get => FindItemForPage (tab_view.SelectedPage);
		set => tab_view.SelectedPage = tab_view.GetPage (value!.Widget);
	}

	/// <summary>
	/// Returns the index of the active item.
	/// </summary>
	public int ActiveItemIndex => tab_view.SelectedPage switch {
		null => -1,
		var page => tab_view.GetPagePosition (page)
	};

	public void InsertTab (IDockNotebookItem item, int position)
	{
		items.Add (item);

		Adw.TabPage page = tab_view.Insert (item.Widget, position);
		page.Title = item.Label;
		// Update the tab's label when the document's title changes.
		item.LabelChanged += (o, args) => { page.Title = item.Label; };
	}

	public void RemoveTab (IDockNotebookItem item)
	{
		var page = tab_view.GetPage (item.Widget);
		tab_view.ClosePage (page);
		items.Remove (item);
	}

	private IDockNotebookItem? FindItemForPage (Adw.TabPage? page) => items.Where (i => i.Widget == page?.Child).FirstOrDefault ();
}
