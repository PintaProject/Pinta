using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Pinta.Core;

[GObject.Subclass<Gtk.MenuButton>]
public sealed partial class ToolBarDropDownButton
{
	private const string ACTION_PREFIX = "tool";

	private bool show_label;
	private Gio.Menu dropdown;
	private Gio.SimpleActionGroup action_group;
	private ToolBarItem? selected_item;

	private List<ToolBarItem> items;
	public ReadOnlyCollection<ToolBarItem> Items { get; private set; }

	public static ToolBarDropDownButton New(bool showLabel = false)
	{
		var obj = NewWithProperties([]);
		obj.show_label = showLabel;

		return obj;
	}

	[MemberNotNull(nameof(items), nameof(Items), nameof(action_group), nameof(dropdown))]
	partial void Initialize()
	{
		items = [];
		Items = new ReadOnlyCollection<ToolBarItem> (items);
		AlwaysShowArrow = true;

		dropdown = Gio.Menu.New ();
		MenuModel = dropdown;

		action_group = Gio.SimpleActionGroup.New ();
		InsertActionGroup (ACTION_PREFIX, action_group);
	}

	public ToolBarItem AddItem (string text, string imageId)
	{
		return AddItem (text, imageId, null);
	}

	public ToolBarItem AddItem (string text, string imageId, object? tag)
	{
		ToolBarItem item = new ToolBarItem (text, imageId, tag);
		action_group.AddAction (item.Action);
		dropdown.AppendItem (Gio.MenuItem.New (text, $"{ACTION_PREFIX}.{item.Action.Name}"));

		items.Add (item);
		item.Action.OnActivate += delegate { SetSelectedItem (item); };

		if (selected_item == null)
			SetSelectedItem (item);

		return item;
	}

	public ToolBarItem SelectedItem {
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
	public ToolBarItem (string text, string imageId) : this (text, imageId, null) { }

	public ToolBarItem (string text, string imageId, object? tag)
	{
		var actionName = AdjustName (text);

		Text = text;
		ImageId = imageId;
		Action = Gio.SimpleAction.New (actionName, null);
		Tag = tag;
	}

	private static string AdjustName (string baseName)
		=> string.Concat (baseName.Where (c => !char.IsWhiteSpace (c)));

	public string ImageId { get; }
	public object? Tag { get; }
	public string Text { get; }
	public Gio.SimpleAction Action { get; }

	public T GetTagOrDefault<T> (T defaultValue)
		=> Tag is T value ? value : defaultValue;
}
