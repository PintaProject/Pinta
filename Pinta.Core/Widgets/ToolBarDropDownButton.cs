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

	private Gtk.StringList string_list;
	private ToolBarItem? selected_item;
	private int previous_index = 0;

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
		if (item is null) { return; }
		item.SetChild (selected_box);
	}

	private void OnBindSelectedItem (Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
	{
		ToolBarItem toolbar_item = items[(int) Selected];

		dropdown_icon.SetFromIconName (toolbar_item.ImageId);
		if (show_label) { dropdown_label.SetText (toolbar_item.Text); }

		// We store the index of the previous selection to avoid having to iterate through all items on the list.
		// Also we check if the index changed because OnBindSelectedItem gets called both when the selected item changes
		// and on the widget initialization/setup.
		int current_index = (int) Selected;
		if (previous_index != current_index) {
			toolbar_item_widgets[previous_index].SetSelectedIconVisible (false);
			toolbar_item_widgets[current_index].SetSelectedIconVisible (true);
			previous_index = current_index;
			SelectedIndex = current_index;
		}
	}

	private void OnBindListItem (Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
	{
		Gtk.ListItem item = (Gtk.ListItem) args.Object;
		if (item is null) { return; }

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
		if (items.Count == 0 && previous_index == 0) { widget.SetSelectedIconVisible (true); }
		items.Add (item);

		if (selected_item == null)
			SetSelectedItem (item);

		return item;
	}

	public new ToolBarItem SelectedItem {
		get =>
			selected_item is not null
			? selected_item
			: throw new InvalidOperationException ("Attempted to get SelectedItem from a drop down with no items.");
		set {
			if (selected_item != value)
				SetSelectedItem (value);
		}
	}

	public int SelectedIndex {
		get => selected_item is null ? -1 : items.IndexOf (selected_item);
		set {
			if (value < 0 || value >= items.Count)
				return;

			var item = items[value];

			if (item != selected_item)
				SetSelectedItem (item);
		}
	}

	private void SetSelectedItem (ToolBarItem item)
	{
		selected_item = item;
		TooltipText = item.Text;
		Selected = (uint) items.IndexOf (selected_item);

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

	public void SetSelectedIconVisible (bool visible)
	{
		selected_icon.Visible = visible;
	}

	private Gtk.Image selected_icon;
}
