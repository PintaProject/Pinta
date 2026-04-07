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

	private Gtk.StringList stringList;
	private ToolBarItem? selected_item;
	private int previous_index = 0;

	private readonly List<ToolBarItem> items;
	public ReadOnlyCollection<ToolBarItem> Items { get; }

	public ToolBarDropDownButton (bool showLabel = false)
	{
		selected_box = new ();
		dropdown_icon = new ();
		dropdown_label = new ();
		selected_box.Append (dropdown_icon);
		selected_box.Append (dropdown_label);

		items = [];
		Items = new (items);
		show_label = showLabel;

		stringList = new ();
		SetModel (stringList);

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
		Gtk.ListItem item = (Gtk.ListItem) args.Object;
		if (item is null) { return; }

		ToolBarItem toolbar_item = items[(int) item.Position];

		dropdown_icon.SetFromIconName (toolbar_item.ImageId);
		if (show_label) { dropdown_label.SetText (toolbar_item.Text); }

		int current_index = (int) Selected;
		if (previous_index != current_index) {
			items[previous_index].SetSelectedIconVisible (false);
			items[current_index].SetSelectedIconVisible (true);
			previous_index = current_index;
			SelectedIndex = current_index;
		}
	}

	private void OnBindListItem (Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
	{
		Gtk.ListItem item = (Gtk.ListItem) args.Object;
		if (item is null) { return; }

		ToolBarItem toolbar_item = items[(int) item.Position];
		item.SetChild (toolbar_item);
	}

	public ToolBarItem AddItem (string text, string imageId)
	{
		return AddItem (text, imageId, null);
	}

	public ToolBarItem AddItem (string text, string imageId, object? tag)
	{
		ToolBarItem item = new (text, imageId, tag);
		stringList.Append ("");

		if (items.Count == 0 && previous_index == 0) { item.SetSelectedIconVisible (true); }
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

public sealed class ToolBarItem : Gtk.Box
{
	public ToolBarItem (string text, string imageId) : this (text, imageId, null) { }

	public ToolBarItem (string text, string imageId, object? tag)
	{
		Text = text;
		ImageId = imageId;
		Tag = tag;

		Gtk.Image image = new ();
		image.SetFromIconName (imageId);
		Gtk.Label label = new ();
		label.SetText (text);

		Append (image);
		Append (label);

		SelectedIcon = new ();
		SelectedIcon.SetFromIconName ("object-select-symbolic");
		SelectedIcon.Visible = false;

		SelectedIcon.Hexpand = true;
		SelectedIcon.Halign = Gtk.Align.End;

		Append (SelectedIcon);
	}

	public void SetSelectedIconVisible (bool visible)
	{
		SelectedIcon.Visible = visible;
	}

	public string ImageId { get; }
	public object? Tag { get; }
	public string Text { get; }

	private Gtk.Image SelectedIcon;

	public T GetTagOrDefault<T> (T defaultValue)
		=> Tag is T value ? value : defaultValue;
}
