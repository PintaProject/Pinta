using System;
using Mono.Addins;
using Pinta.Core;

namespace Pinta.Gui.Addins;

internal sealed class AddinInfoView : Adw.Bin
{
	private readonly Adw.ViewStack view_stack;
	private readonly Gtk.Box content_box;
	private readonly Adw.Bin empty_page = new ();

	private readonly Gtk.Label title_label;
	private readonly Gtk.Label version_label;
	private readonly Gtk.Label size_label;
	private readonly Gtk.Label repo_label;
	private readonly Gtk.Label desc_label;
	private readonly Gtk.Button info_button;
	private readonly Gtk.Button install_button;
	private readonly Gtk.Button update_button;
	private readonly Gtk.Button uninstall_button;
	private readonly Gtk.Switch enable_switch = new ();

	private AddinListViewItem? current_item;

	/// <summary>
	/// Event raised when addins are installed or uninstalled.
	/// </summary>
	public event EventHandler? OnAddinChanged;

	public AddinInfoView ()
	{
		WidthRequest = 300;

		view_stack = Adw.ViewStack.New ();
		view_stack.Add (empty_page);

		content_box = Gtk.Box.New (Gtk.Orientation.Vertical, 10);
		content_box.SetAllMargins (10);
		view_stack.Add (content_box);

		title_label = new Gtk.Label () {
			Halign = Gtk.Align.Start
		};
		title_label.AddCssClass (AdwaitaStyles.Title4);
		content_box.Append (title_label);

		version_label = new Gtk.Label () {
			Halign = Gtk.Align.Start
		};
		version_label.AddCssClass (AdwaitaStyles.Heading);
		content_box.Append (version_label);

		size_label = new Gtk.Label () {
			Halign = Gtk.Align.Start
		};
		size_label.AddCssClass (AdwaitaStyles.Heading);
		content_box.Append (size_label);

		repo_label = new Gtk.Label () {
			Halign = Gtk.Align.Start
		};
		repo_label.AddCssClass (AdwaitaStyles.Heading);
		content_box.Append (repo_label);

		desc_label = new Gtk.Label () {
			Halign = Gtk.Align.Start,
			Hexpand = true,
			Valign = Gtk.Align.Start,
			Vexpand = true,
			Xalign = 0,
			Wrap = true,
		};
		desc_label.AddCssClass (AdwaitaStyles.Body);
		content_box.Append (desc_label);

		info_button = Gtk.Button.NewWithLabel (Translations.GetString ("More Information..."));
		info_button.OnClicked += (_, _) => HandleInfoButtonClicked ();
		info_button.Visible = false;

		install_button = Gtk.Button.NewWithLabel (Translations.GetString ("Install..."));
		install_button.AddCssClass (AdwaitaStyles.SuggestedAction);
		install_button.OnClicked += (_, _) => HandleInstallButtonClicked ();
		install_button.Visible = false;

		update_button = Gtk.Button.NewWithLabel (Translations.GetString ("Update..."));
		update_button.AddCssClass (AdwaitaStyles.SuggestedAction);
		update_button.OnClicked += (_, _) => HandleUpdateButtonClicked ();
		update_button.Visible = false;

		uninstall_button = Gtk.Button.NewWithLabel (Translations.GetString ("Uninstall..."));
		uninstall_button.AddCssClass (AdwaitaStyles.DestructiveAction);
		uninstall_button.OnClicked += (_, _) => HandleUninstallButtonClicked ();
		uninstall_button.Visible = false;

		enable_switch.Visible = false;

		var hbox = Gtk.Box.New (Gtk.Orientation.Horizontal, 6);
		hbox.AddCssClass (AdwaitaStyles.Toolbar);
		hbox.Append (enable_switch);
		hbox.Append (install_button);
		hbox.Append (update_button);
		hbox.Append (info_button);
		hbox.Append (uninstall_button);
		uninstall_button.Hexpand = true;
		uninstall_button.Halign = Gtk.Align.End;
		content_box.Append (hbox);

		enable_switch.OnNotify += (o, e) => {
			if (e.Pspec.GetName () != "active")
				return;

			HandleEnableSwitched ();
		};

		view_stack.SetVisibleChild (empty_page);
		Child = view_stack;
	}

	public void Update (AddinListViewItem? item)
	{
		if (item is null) {
			view_stack.SetVisibleChild (empty_page);
		} else {
			view_stack.SetVisibleChild (content_box);

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
			update_button.Visible = item.Addin is not null && Addin.CompareVersions (item.Addin.Version, item.Version) > 0;
			uninstall_button.Visible = item.CanUninstall;

			enable_switch.Visible = item.Installed && item.CanDisable;
			if (item.CanDisable)
				enable_switch.Active = item.Enabled;
		}

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
		ArgumentNullException.ThrowIfNull (current_item);
		if (current_item.RepositoryEntry is null)
			throw new Exception ("The install button should not be available unless there is a repository entry");

		var dialog = new InstallDialog (PintaCore.Chrome.MainWindow, current_item.Service);
		dialog.OnSuccess += (_, _) => OnAddinChanged?.Invoke (this, EventArgs.Empty);
		dialog.InitForInstall (new[] { current_item.RepositoryEntry });
		dialog.Show ();
	}

	private void HandleUpdateButtonClicked ()
	{
		ArgumentNullException.ThrowIfNull (current_item);
		if (current_item.RepositoryEntry is null)
			throw new Exception ("The update button should not be available unless there is a repository entry");

		var dialog = new InstallDialog (PintaCore.Chrome.MainWindow, current_item.Service);
		dialog.OnSuccess += (_, _) => OnAddinChanged?.Invoke (this, EventArgs.Empty);
		dialog.InitForInstall (new[] { current_item.RepositoryEntry });
		dialog.Show ();
	}

	private void HandleUninstallButtonClicked ()
	{
		ArgumentNullException.ThrowIfNull (current_item);
		if (current_item.Addin is null)
			throw new Exception ("The uninstall button should not be available unless there is an installed addin");

		var dialog = new InstallDialog (PintaCore.Chrome.MainWindow, current_item.Service);
		dialog.OnSuccess += (_, _) => OnAddinChanged?.Invoke (this, EventArgs.Empty);
		dialog.InitForUninstall (new[] { current_item.Addin });
		dialog.Show ();
	}
}
