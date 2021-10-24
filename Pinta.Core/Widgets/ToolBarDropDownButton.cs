using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gtk;

namespace Pinta.Core
{
	public class ToolBarDropDownButton : Gtk.ToolItem
	{
        private const string action_prefix = "tool";

        private Gtk.MenuButton menu_button;
		private Label? label_widget;
		private GLib.Menu dropdown;
		private GLib.SimpleActionGroup action_group;
		private Image image;
		private ToolBarItem? selected_item;

		public List<ToolBarItem> Items { get; private set; }

		public ToolBarDropDownButton (bool showLabel = false)
		{
			Items = new List<ToolBarItem> ();

			menu_button = new Gtk.MenuButton();
			image = new Image ();

			dropdown = new GLib.Menu();
			menu_button.MenuModel = dropdown;
			menu_button.UsePopover = false;

			action_group = new GLib.SimpleActionGroup();
			menu_button.InsertActionGroup(action_prefix, action_group);

			var box = new HBox();
            if (showLabel)
			{
                box.PackStart (image, true, true, 3);
                label_widget = new Gtk.Label ();
                box.PackStart (label_widget, true, false, 2);
            }
			else
                box.PackStart (image, true, true, 5);

            var alignment = new Alignment (0f, 0.5f, 0f, 0f);
            var arrow = new Arrow (ArrowType.Down, ShadowType.None);
            alignment.Add (arrow);
            box.PackStart (alignment, false, false, 0);

			menu_button.Add(box);
			Add(menu_button);
			ShowAll();
		}

		public ToolBarItem AddItem (string text, string imageId)
		{
			return AddItem (text, imageId, null);
		}

		public ToolBarItem AddItem (string text, string imageId, object? tag)
		{
			ToolBarItem item = new ToolBarItem (text, imageId, tag);
			action_group.AddAction(item.Action);
			dropdown.AppendItem(new GLib.MenuItem(text, string.Format("{0}.{1}", action_prefix, item.Action.Name)));

			Items.Add (item);
			item.Action.Activated += delegate { SetSelectedItem (item); };

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
			get => selected_item is null ? -1 : Items.IndexOf (selected_item);
			set {
				if (value < 0 || value >= Items.Count)
					return;

				var item = Items[value];

				if (item != selected_item)
					SetSelectedItem (item);
			}
		}

		protected void SetSelectedItem (ToolBarItem item)
		{
			image.Pixbuf = IconTheme.Default.LoadIcon(item.ImageId, 16);

			selected_item = item;
			TooltipText = item.Text;

			if (label_widget != null)
				label_widget.Text = item.Text;

			OnSelectedItemChanged ();
		}

		protected void OnSelectedItemChanged ()
		{
			if (SelectedItemChanged != null)
				SelectedItemChanged (this, EventArgs.Empty);
		}

		public event EventHandler? SelectedItemChanged;
	}

	public class ToolBarItem
	{
		public ToolBarItem (string text, string imageId)
		{
			Text = text;
			ImageId = imageId;

            var action_name = string.Concat(Text.Where(c => !char.IsWhiteSpace(c)));
            Action = new GLib.SimpleAction(action_name, null);
		}

		public ToolBarItem (string text, string imageId, object? tag) : this (text, imageId)
		{
			Tag = tag;
		}

		public string ImageId { get; set; }
		public object? Tag { get; set; }
		public string Text { get; set; }
		public GLib.SimpleAction Action { get; private set; }

		public T GetTagOrDefault<T> (T defaultValue)
		{
			if (Tag is T value)
				return value;

			return defaultValue;
		}
	}
}
