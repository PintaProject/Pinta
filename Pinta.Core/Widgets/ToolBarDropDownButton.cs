using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pinta.Core;

public sealed class ToolBarDropDownButton : Gtk.DropDown
{
	private const string ACTION_PREFIX = "tool";
	private readonly bool show_label;

	private Gtk.Box selected_box;
	private Gtk.Image dropdown_icon;
	private Gtk.Label dropdown_label;

	private Gtk.StringList stringList;
	private ToolBarItem? selected_item;

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
		Items = new ReadOnlyCollection<ToolBarItem> (items);
		show_label = showLabel;

		stringList = new Gtk.StringList ();
		SetModel (stringList);

		Gtk.SignalListItemFactory selectedFactory = new ();
		selectedFactory.OnSetup += OnSetupSelectedItem;
		selectedFactory.OnBind += OnBindSelectedItem;
		SetFactory (selectedFactory);

		Gtk.SignalListItemFactory listFactory = new ();
		listFactory.OnSetup += OnSetupListItem;
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

		var string_object = (Gtk.StringObject) item.GetItem ();
		if (string_object is null) { return; }

		System.String[] strings = string_object.String.Split ("|");
		if (strings[1] is null) { return; }

		dropdown_icon.SetFromIconName (strings[1]);
		if (show_label) { dropdown_label.SetText (strings[0]); }

		SelectedIndex = (int) Selected;
	}

	private void OnSetupListItem (Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.SetupSignalArgs args)
	{
		Gtk.ListItem item = (Gtk.ListItem) args.Object;
		if (item is null) { return; }
		Gtk.Box box = new ();

		Gtk.Image image = new ();
		Gtk.Label label = new ();
		Gtk.Image selected_icon = new ();
		selected_icon.SetFromIconName ("object-select-symbolic");
		item.BindProperty (Gtk.ListItem.SelectedPropertyDefinition.UnmanagedName, selected_icon, Gtk.Image.VisiblePropertyDefinition.UnmanagedName, GObject.BindingFlags.SyncCreate);
		selected_icon.Hexpand = true;
		selected_icon.Halign = Gtk.Align.End;

		box.Append (image);
		box.Append (label);
		box.Append (selected_icon);

		item.SetChild (box);
	}

	private void OnBindListItem (Gtk.SignalListItemFactory sender, Gtk.SignalListItemFactory.BindSignalArgs args)
	{
		Gtk.ListItem item = (Gtk.ListItem) args.Object;
		if (item is null) { return; }
		Gtk.Box box = (Gtk.Box) item.GetChild ();
		if (box is null) { return; }

		Gtk.StringObject string_object = (Gtk.StringObject) item.GetItem ();
		if (string_object is null) { return; }

		System.String[] strings = string_object.String.Split ("|");
		if (strings[1] is null) { return; }

		Gtk.Image image = (Gtk.Image) box.GetFirstChild ();
		if (image is null) { return; }
		image.SetFromIconName (strings[1]);

		Gtk.Label label = (Gtk.Label) image.GetNextSibling ();
		if (label is null) { return; }
		label.SetText (strings[0]);
	}

	public ToolBarItem AddItem (string text, string imageId)
	{
		return AddItem (text, imageId, null);
	}

	public ToolBarItem AddItem (string text, string imageId, object? tag)
	{
		ToolBarItem item = new ToolBarItem (text, imageId, tag);
		stringList.Append (text + "|" + imageId + "|" + $"{ACTION_PREFIX}.{item.Action.Name}");

		items.Add (item);

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
		selected_item = item;
		TooltipText = item.Text;

		// OnSelectedItemChanged ();
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
