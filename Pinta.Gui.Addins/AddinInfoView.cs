using System;
using Pinta.Core;

namespace Pinta.Gui.Addins
{
	internal class AddinInfoView : Gtk.Box
	{
		private Gtk.Label title_label;
		private Gtk.Label version_label;
		private Gtk.Label size_label;
		private Gtk.Label repo_label;
		private Gtk.Label desc_label;
		private Gtk.Button info_button;
		private Gtk.Button install_button;
		private Gtk.Button uninstall_button;
		private Gtk.Switch enable_switch = new ();

		private AddinListViewItem? current_item;

		public AddinInfoView ()
		{
			this.SetAllMargins (10);
			SetOrientation (Gtk.Orientation.Vertical);
			Spacing = 10;
			WidthRequest = 300;

			title_label = new Gtk.Label () {
				Halign = Gtk.Align.Start
			};
			title_label.AddCssClass (AdwaitaStyles.Title4);
			Append (title_label);

			version_label = new Gtk.Label () {
				Halign = Gtk.Align.Start
			};
			version_label.AddCssClass (AdwaitaStyles.Heading);
			Append (version_label);

			size_label = new Gtk.Label () {
				Halign = Gtk.Align.Start
			};
			size_label.AddCssClass (AdwaitaStyles.Heading);
			Append (size_label);

			repo_label = new Gtk.Label () {
				Halign = Gtk.Align.Start
			};
			repo_label.AddCssClass (AdwaitaStyles.Heading);
			Append (repo_label);

			desc_label = new Gtk.Label () {
				Halign = Gtk.Align.Start,
				Hexpand = true,
				Valign = Gtk.Align.Start,
				Vexpand = true,
				Xalign = 0,
				Wrap = true,
			};
			desc_label.AddCssClass (AdwaitaStyles.Body);
			Append (desc_label);

			info_button = Gtk.Button.NewWithLabel (Translations.GetString ("More Information..."));
			info_button.OnClicked += (_, _) => HandleInfoButtonClicked ();

			install_button = Gtk.Button.NewWithLabel (Translations.GetString ("Install..."));
			install_button.AddCssClass (AdwaitaStyles.SuggestedAction);
			install_button.OnClicked += (_, _) => HandleInstallButtonClicked ();

			uninstall_button = Gtk.Button.NewWithLabel (Translations.GetString ("Uninstall..."));
			uninstall_button.AddCssClass (AdwaitaStyles.DestructiveAction);
			uninstall_button.OnClicked += (_, _) => HandleUninstallButtonClicked ();

			// TODO - add an update button

			var hbox = Gtk.Box.New (Gtk.Orientation.Horizontal, 6);
			hbox.AddCssClass (AdwaitaStyles.Toolbar);
			hbox.Append (uninstall_button);
			hbox.Append (install_button);
			hbox.Append (info_button);
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

			string? download_size = item.DownloadSize;
			size_label.Visible = download_size != null;
			if (download_size is not null)
				size_label.SetLabel (Translations.GetString ("Download size: {0}", download_size));

			string? repo_name = item.RepositoryName;
			repo_label.Visible = repo_name != null;
			if (repo_name is not null)
				repo_label.SetLabel (Translations.GetString ("Available in repository: {0}", repo_name));

			info_button.Visible = !string.IsNullOrEmpty (item.Url);
			install_button.Visible = !item.Installed;
			uninstall_button.Visible = item.CanUninstall;

			enable_switch.Visible = item.Installed && item.CanDisable;
			if (item.CanDisable)
				enable_switch.Active = item.Enabled;

			current_item = item;
		}

		private void HandleEnableSwitched ()
		{
			if (current_item is not null && current_item.CanDisable)
				current_item.Enabled = enable_switch.Active;
		}

		private void HandleInfoButtonClicked ()
		{
			Gtk.Functions.ShowUri (null, current_item!.Url, /* GDK_CURRENT_TIME */ 0);
		}

		private void HandleInstallButtonClicked ()
		{
			// TODO
			throw new NotImplementedException ();
		}

		private void HandleUninstallButtonClicked ()
		{
			// TODO
			throw new NotImplementedException ();
		}
	}
}
