using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pinta.Core;

public sealed class ToolBarDropDownButton : Gtk.DropDown
{
	private readonly bool show_label;

	private Gtk.Box selected_box;
	private Gtk.Image dropdown_icon;
	private Gtk.Label dropdown_label;

	// We store the index of the previous selection to avoid having to iterate through all items on the list.
	private int previous_index = 0;
	private Gtk.StringList string_list;

	private readonly List<ToolBarItem> items;
	private readonly List<ToolBarItemWidget> toolbar_item_widgets;
	public ReadOnlyCollection<ToolBarItem> Items { get; }

	public ToolBarDropDownButton (bool showLabel = false)
	{
		// We create the widgets inside the dropdown to avoid having to create yet another custom widget
		// for the selectedFactory. Also, we can reference them directly when updated, avoiding
		// .nextSibling hacks.
		selected_box = new ();
		dropdown_icon = new ();
		dropdown_label = new ();
		selected_box.Append (dropdown_icon);
		selected_box.Append (dropdown_label);

		items = [];
		Items = new (items);
		toolbar_item_widgets = [];
		show_label = showLabel;

		string_list = new ();
		SetModel (string_list);

		Gtk.SignalListItemFactory selectedFactory = new ();
		selectedFactory.OnSetup += OnSetupSelectedItem;
		selectedFactory.OnBind += OnBindSelectedItem;
		SetFactory (selectedFactory);

		Gtk.SignalListItemFactory listFactory = new ();
		listFactory.OnBind += OnBindListItem;
		SetListFactory (listFactory);
	}

	private void OnSetupSelectedItem (Gtk.SignalListItemFactory factory, Gtk.SignalListItemFactory.SetupSignalArgs args)
	{
		Gtk.ListItem item = (Gtk.ListItem) args.Object;
		item.SetChild (selected_box);
	}

	private void OnBindSelectedItem (Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
	{
		ToolBarItem toolbar_item = items[(int) Selected];

		dropdown_icon.SetFromIconName (toolbar_item.ImageId);
		if (show_label) { dropdown_label.SetText (toolbar_item.Text); }

		// SetSelectedIndex checks if the index changed, so we don't need to check here again. This check
		// is important because OnBindSelectedItem gets called both when the selected item changes and on
		// widget initialization/setup.
		SetSelectedIndex ((int) Selected);
	}

	private void OnBindListItem (Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
	{
		Gtk.ListItem item = (Gtk.ListItem) args.Object;

		ToolBarItemWidget toolbar_item = toolbar_item_widgets[(int) item.Position];
		item.SetChild (toolbar_item);
	}

	public ToolBarItem AddItem (string text, string imageId)
	{
		return AddItem (text, imageId, null);
	}

	public ToolBarItem AddItem (string text, string imageId, object? tag)
	{
		ToolBarItemWidget widget = new (text, imageId);
		toolbar_item_widgets.Add (widget);
		// We append an empty string because we only need the list's index.
		// Otherwise, we'd need to make ToolBarItem inherit from GObject, which is undesired.
		// Also, we'd need the indexes anyway to update the previous selection, so
		// storing anything else is not required.
		string_list.Append ("");

		ToolBarItem item = new (text, imageId, tag);
		// This is done to ensure the first item has a checkmark if it was selected.
		if (items.Count == 0) { widget.SetCheckmarkVisible (true); }
		items.Add (item);

		return item;
	}

	public new ToolBarItem SelectedItem {
		get => items.Count == 0
			? throw new InvalidOperationException ("Attempted to get SelectedItem from a drop down with no items.")
			: items[previous_index];
		set { SetSelectedIndex (items.IndexOf (value)); }
	}

	public int SelectedIndex {
		get => items.Count == 0 ? -1 : previous_index;
		set { SetSelectedIndex (value); }
	}

	private void SetSelectedIndex (int index)
	{
		if (index < 0 || index >= items.Count || index == previous_index) {
			return;
		}

		toolbar_item_widgets[previous_index].SetCheckmarkVisible (false);
		toolbar_item_widgets[index].SetCheckmarkVisible (true);

		TooltipText = items[index].Text;
		Selected = (uint) index;
		previous_index = index;
		OnSelectedItemChanged ();
	}

	private void OnSelectedItemChanged ()
	{
		SelectedItemChanged?.Invoke (this, EventArgs.Empty);
	}

	public event EventHandler? SelectedItemChanged;
}

public sealed class ToolBarItem
{
	public ToolBarItem (string text, string imageId) : this (text, imageId, null) { }

	public ToolBarItem (string text, string imageId, object? tag)
	{
		Text = text;
		ImageId = imageId;
		Tag = tag;
	}

	public string ImageId { get; }
	public object? Tag { get; }
	public string Text { get; }

	public T GetTagOrDefault<T> (T defaultValue)
		=> Tag is T value ? value : defaultValue;
}

public sealed class ToolBarItemWidget : Gtk.Box
{
	public ToolBarItemWidget (string text, string imageId)
	{
		Gtk.Image image = new ();
		image.SetFromIconName (imageId);
		Gtk.Label label = new ();
		label.SetText (text);

		Append (image);
		Append (label);

		selected_icon = new ();
		selected_icon.SetFromIconName (Resources.StandardIcons.ObjectSelect);
		selected_icon.Visible = false;
		selected_icon.Hexpand = true;
		selected_icon.Halign = Gtk.Align.End;

		Append (selected_icon);
	}

	public void SetCheckmarkVisible (bool visible)
	{
		selected_icon.Visible = visible;
	}

	private Gtk.Image selected_icon;
}
