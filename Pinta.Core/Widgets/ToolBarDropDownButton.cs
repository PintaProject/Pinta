using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pinta.Core;

public sealed class ToolBarDropDownButton : Gtk.MenuButton
{
	private const string action_prefix = "tool";

	private readonly bool show_label;
	private readonly Gio.Menu dropdown;
	private readonly Gio.SimpleActionGroup action_group;
	private ToolBarItem? selected_item;

	private readonly List<ToolBarItem> items;
	public ReadOnlyCollection<ToolBarItem> Items { get; }

	public ToolBarDropDownButton (bool showLabel = false)
	{
		this.show_label = showLabel;

		items = new List<ToolBarItem> ();
		Items = new ReadOnlyCollection<ToolBarItem> (items);
		AlwaysShowArrow = true;

		dropdown = Gio.Menu.New ();
		MenuModel = dropdown;

		action_group = Gio.SimpleActionGroup.New ();
		InsertActionGroup (action_prefix, action_group);
	}

	public ToolBarItem AddItem (string text, string imageId)
	{
		return AddItem (text, imageId, null);
	}

	public ToolBarItem AddItem (string text, string imageId, object? tag)
	{
		ToolBarItem item = new ToolBarItem (text, imageId, tag);
		action_group.AddAction (item.Action);
		dropdown.AppendItem (Gio.MenuItem.New (text, $"{action_prefix}.{item.Action.Name}"));

		items.Add (item);
		item.Action.OnActivate += delegate { SetSelectedItem (item); };

		if (selected_item == null)
			SetSelectedItem (item);

		return item;
	}

	public ToolBarItem SelectedItem {
		get {
			if (selected_item is null)
				throw new InvalidOperationException ("Attempted to get SelectedItem from a drop down with no items.");

			return selected_item;
		}
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
		IconName = item.ImageId;

		selected_item = item;
		TooltipText = item.Text;

		if (show_label)
			Label = item.Text;

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
	public ToolBarItem (string text, string imageId)
	{
		Text = text;
		ImageId = imageId;

		var action_name = string.Concat (Text.Where (c => !char.IsWhiteSpace (c)));
		Action = Gio.SimpleAction.New (action_name, null);
	}

	public ToolBarItem (string text, string imageId, object? tag) : this (text, imageId)
	{
		Tag = tag;
	}

	public string ImageId { get; set; }
	public object? Tag { get; set; }
	public string Text { get; set; }
	public Gio.SimpleAction Action { get; }

	public T GetTagOrDefault<T> (T defaultValue)
	{
		if (Tag is T value)
			return value;

		return defaultValue;
	}
}
