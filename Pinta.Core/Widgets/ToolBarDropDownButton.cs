using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hyena.Widgets;
using Gtk;

namespace Pinta.Core
{
	public class ToolBarDropDownButton : MenuButton
	{
		private Menu dropdown;
		private Image image;
		private ToolBarItem selected_item;

		public List<ToolBarItem> Items { get; private set; }

		public ToolBarDropDownButton (bool showLabel = false)
		{
			Items = new List<ToolBarItem> ();

			dropdown = new Menu ();
			image = new Image ();

			Construct (image, dropdown, true, showLabel);
		}

		public ToolBarItem AddItem (string text, string imageId)
		{
			return AddItem (text, imageId, null);
		}

		public ToolBarItem AddItem (string text, string imageId, object tag)
		{
			ToolBarItem item = new ToolBarItem (text, imageId, tag);
			dropdown.Add (item.Action.CreateMenuItem ());

			Items.Add (item);
			item.Action.Activated += delegate { SetSelectedItem (item); };

			if (selected_item == null)
				SetSelectedItem (item);

			return item;
		}

		public ToolBarItem SelectedItem {
			get { return selected_item; }
			set {
				if (selected_item != value)
					SetSelectedItem (value);
			}
		}

		protected void SetSelectedItem (ToolBarItem item)
		{
			Gdk.Pixbuf pb = PintaCore.Resources.GetIcon (item.Action.StockId);
			image.Pixbuf = pb;

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

		public event EventHandler SelectedItemChanged;
	}

	public class ToolBarItem
	{
		public ToolBarItem ()
		{

		}

		public ToolBarItem (string text, string imageId)
		{
			Text = text;
			ImageId = imageId;

			Action = new Gtk.Action (Text, Text, string.Empty, imageId);
		}

		public ToolBarItem (string text, string imageId, object tag) : this (text, imageId)
		{
			Tag = tag;
		}

		public string ImageId { get; set; }
		public object Tag { get; set; }
		public string Text { get; set; }
		public Gtk.Action Action { get; private set; }
	}
}
