using System;
using Pinta.Core;

namespace Pinta.Gui.Addins
{
	internal class AddinInfoView : Gtk.Box
	{
		private Gtk.Label title_label = new ();
		private Gtk.Label version_label = new ();
		private Gtk.Label desc_label = new ();
		private Gtk.Switch enable_switch = new ();

		private AddinListViewItem? current_item;

		public AddinInfoView ()
		{
			this.SetAllMargins (10);
			SetOrientation (Gtk.Orientation.Vertical);
			Spacing = 10;
			WidthRequest = 300;

			title_label.Halign = Gtk.Align.Start;
			title_label.AddCssClass (AdwaitaStyles.Title4);
			Append (title_label);

			version_label.Halign = Gtk.Align.Start;
			version_label.AddCssClass (AdwaitaStyles.Heading);
			Append (version_label);

			desc_label.Halign = Gtk.Align.Start;
			desc_label.Hexpand = true;
			desc_label.Valign = Gtk.Align.Start;
			desc_label.Vexpand = true;
			desc_label.Wrap = true;
			desc_label.AddCssClass (AdwaitaStyles.Body);
			Append (desc_label);

			var hbox = Gtk.Box.New (Gtk.Orientation.Horizontal, 6);
			hbox.AddCssClass (AdwaitaStyles.Toolbar);
			hbox.Append (enable_switch);
			Append (hbox);

			enable_switch.OnNotify += (o, e) => {
				if (e.Pspec.GetName () != "active")
					return;

				HandleEnableSwitched ();
			};
		}

		public void Update (AddinListViewItem item)
		{
			title_label.SetLabel (item.Name);
			version_label.SetLabel (Translations.GetString ("Version: {0}", item.Version));
			desc_label.SetLabel (item.Description);

			enable_switch.Visible = item.CanDisable;
			if (item.CanDisable)
				enable_switch.Active = item.Enabled;

			current_item = item;
		}

		private void HandleEnableSwitched ()
		{
			if (current_item is not null && current_item.CanDisable)
				current_item.Enabled = enable_switch.Active;
		}
	}
}
