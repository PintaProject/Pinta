//
// HistoryTreeView.cs
//
// Copyright (c) 2010 Jonathan Pobst
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
using GObject;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

// GObject subclass for use with Gio.ListStore
[GObject.Subclass<GObject.Object>]
public sealed partial class HistoryListViewItem
{
	private BaseHistoryItem item = new ();

	public static HistoryListViewItem New (BaseHistoryItem item)
	{
		if (string.IsNullOrEmpty (item.Text))
			throw new ArgumentException ($"{nameof (item.Text)} must contain value.");

		if (string.IsNullOrEmpty (item.Icon))
			throw new ArgumentException ($"{nameof (item.Icon)} must contain value.");

		HistoryListViewItem listViewItem = NewWithProperties ([]);
		listViewItem.item = item;
		return listViewItem;
	}

	public string Label => item.Text ?? string.Empty;
	public string IconName => item.Icon ?? string.Empty;
	public bool Active => item.State == HistoryItemState.Undo;
}

[GObject.Subclass<Gtk.Box>]
public sealed partial class HistoryItemWidget
{
	private readonly Gtk.Image image = Gtk.Image.New ();
	private readonly Gtk.Label label = Gtk.Label.New (string.Empty);

	public static HistoryItemWidget New ()
		=> NewWithProperties ([]);

	partial void Initialize ()
	{
		Spacing = 6;

		this.SetAllMargins (2);

		SetOrientation (Gtk.Orientation.Horizontal);

		label.Halign = Gtk.Align.Start;

		Append (image);
		Append (label);
	}

	// Set the widget's contents to the provided history item.
	public void Update (HistoryListViewItem item)
	{
		image.IconName = item.IconName;
		label.SetText (item.Label);

		if (item.Active)
			RemoveCssClass (AdwaitaStyles.DimLabel);
		else
			AddCssClass (AdwaitaStyles.DimLabel);
	}
}
