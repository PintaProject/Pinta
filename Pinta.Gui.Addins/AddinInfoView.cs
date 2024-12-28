using System;
using Mono.Addins;
using Pinta.Core;

namespace Pinta.Gui.Addins;

internal sealed class AddinInfoView : Adw.Bin
{
	private readonly Gtk.Label title_label;
	private readonly Gtk.Label version_label;
	private readonly Gtk.Label size_label;
	private readonly Gtk.Label repo_label;
	private readonly Gtk.Label description_label;

	private readonly Gtk.Button info_button;
	private readonly Gtk.Button install_button;
	private readonly Gtk.Button update_button;
	private readonly Gtk.Button uninstall_button;

	private readonly Gtk.Switch enable_switch;

	private readonly Gtk.Box content_box;

	private readonly Adw.Bin empty_page;

	private readonly Adw.ViewStack view_stack;
	private AddinListViewItem? current_item;

	/// <summary>
	/// Event raised when addins are installed or uninstalled.
	/// </summary>
	public event EventHandler? OnAddinChanged;

	private readonly SystemManager system;

	public AddinInfoView (SystemManager system)
	{
		// --- Control creation

		Gtk.Label titleLabel = new () { Halign = Gtk.Align.Start };
		titleLabel.AddCssClass (AdwaitaStyles.Title4);

		Gtk.Label versionLabel = new () { Halign = Gtk.Align.Start };
		versionLabel.AddCssClass (AdwaitaStyles.Heading);

		Gtk.Label sizeLabel = new () { Halign = Gtk.Align.Start };
		sizeLabel.AddCssClass (AdwaitaStyles.Heading);

		Gtk.Label repoLabel = new () { Halign = Gtk.Align.Start };
		repoLabel.AddCssClass (AdwaitaStyles.Heading);

		Gtk.Label descriptionLabel = CreateDescriptionLabel ();

		Adw.Bin emptyPage = new ();

		Gtk.Button infoButton = CreateInfoButton ();
		Gtk.Button installButton = CreateInstallButton ();
		Gtk.Button updateButton = CreateUpdateButton ();
		Gtk.Button uninstallButton = CreateUninstallButton ();

		Gtk.Switch enableSwitch = CreateEnableSwitch ();

		Gtk.Box hbox = Gtk.Box.New (Gtk.Orientation.Horizontal, 6);
		hbox.AddCssClass (AdwaitaStyles.Toolbar);
		hbox.Append (enableSwitch);
		hbox.Append (installButton);
		hbox.Append (updateButton);
		hbox.Append (infoButton);
		hbox.Append (uninstallButton);

		Gtk.Box contentBox = Gtk.Box.New (Gtk.Orientation.Vertical, 10);
		contentBox.SetAllMargins (10);
		contentBox.Append (titleLabel);
		contentBox.Append (versionLabel);
		contentBox.Append (sizeLabel);
		contentBox.Append (repoLabel);
		contentBox.Append (descriptionLabel);
		contentBox.Append (hbox);

		Adw.ViewStack viewStack = Adw.ViewStack.New ();
		viewStack.Add (emptyPage);
		viewStack.Add (contentBox);
		viewStack.SetVisibleChild (emptyPage);

		// --- Gtk.Widget initialization

		WidthRequest = 300;

		// --- Adwaita.Bin initialization

		Child = viewStack;

		// --- References to keep

		title_label = titleLabel;
		version_label = versionLabel;
		size_label = sizeLabel;
		repo_label = repoLabel;
		description_label = descriptionLabel;

		info_button = infoButton;
		install_button = installButton;
		update_button = updateButton;
		uninstall_button = uninstallButton;

		enable_switch = enableSwitch;

		content_box = contentBox;

		empty_page = emptyPage;

		view_stack = viewStack;

		this.system = system;
	}

	private Gtk.Switch CreateEnableSwitch ()
	{
		Gtk.Switch result = new () {
			Visible = false
		};
		result.OnNotify += (o, e) => {
			if (e.Pspec.GetName () == "active")
				HandleEnableSwitched ();
		};
		return result;
	}

	private static Gtk.Label CreateDescriptionLabel ()
	{
		Gtk.Label result = new () {
			Halign = Gtk.Align.Start,
			Hexpand = true,
			Valign = Gtk.Align.Start,
			Vexpand = true,
			Xalign = 0,
			Wrap = true,
		};
		result.AddCssClass (AdwaitaStyles.Body);
		return result;
	}

	private Gtk.Button CreateInfoButton ()
	{
		Gtk.Button result = Gtk.Button.NewWithLabel (Translations.GetString ("More Information..."));
		result.OnClicked += (_, _) => HandleInfoButtonClicked ();
		result.Visible = false;
		return result;
	}

	private Gtk.Button CreateInstallButton ()
	{
		Gtk.Button result = Gtk.Button.NewWithLabel (Translations.GetString ("Install..."));
		result.AddCssClass (AdwaitaStyles.SuggestedAction);
		result.OnClicked += (_, _) => HandleInstallButtonClicked ();
		result.Visible = false;
		return result;
	}

	private Gtk.Button CreateUpdateButton ()
	{
		Gtk.Button result = Gtk.Button.NewWithLabel (Translations.GetString ("Update..."));
		result.AddCssClass (AdwaitaStyles.SuggestedAction);
		result.OnClicked += (_, _) => HandleUpdateButtonClicked ();
		result.Visible = false;
		return result;
	}

	private Gtk.Button CreateUninstallButton ()
	{
		Gtk.Button result = Gtk.Button.NewWithLabel (Translations.GetString ("Uninstall..."));
		result.AddCssClass (AdwaitaStyles.DestructiveAction);
		result.OnClicked += (_, _) => HandleUninstallButtonClicked ();
		result.Visible = false;

		result.Hexpand = true;
		result.Halign = Gtk.Align.End;

		return result;
	}

	public void Update (AddinListViewItem? item)
	{
		if (item is null)
			ViewEmptyItem ();
		else
			ViewExistingItem (item);

		current_item = item;
	}

	private void ViewEmptyItem ()
	{
		view_stack.SetVisibleChild (empty_page);
	}

	private void ViewExistingItem (AddinListViewItem item)
	{
		view_stack.SetVisibleChild (content_box);

		title_label.SetLabel (item.Name);
		version_label.SetLabel (Translations.GetString ("Version: {0}", item.Version));
		description_label.SetLabel (item.Description);

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

	private void HandleEnableSwitched ()
	{
		if (current_item is not null && current_item.CanDisable)
			current_item.Enabled = enable_switch.Active;
	}

	private void HandleInfoButtonClicked ()
	{
		system.LaunchUri (current_item!.Url);
	}

	private void HandleInstallButtonClicked ()
	{
		if (current_item is null)
			throw new InvalidOperationException ($"{nameof (current_item)} is null");

		if (current_item.RepositoryEntry is null)
			throw new InvalidOperationException ("The install button should not be available unless there is a repository entry");

		InstallDialog dialog = new (PintaCore.Chrome.MainWindow, current_item.Service);
		dialog.OnSuccess += (_, _) => OnAddinChanged?.Invoke (this, EventArgs.Empty);
		dialog.InitForInstall (new[] { current_item.RepositoryEntry });
		dialog.Show ();
	}

	private void HandleUpdateButtonClicked ()
	{
		if (current_item is null)
			throw new InvalidOperationException ($"{nameof (current_item)} is null");

		if (current_item.RepositoryEntry is null)
			throw new InvalidOperationException ("The update button should not be available unless there is a repository entry");

		InstallDialog dialog = new (PintaCore.Chrome.MainWindow, current_item.Service);
		dialog.OnSuccess += (_, _) => OnAddinChanged?.Invoke (this, EventArgs.Empty);
		dialog.InitForInstall (new[] { current_item.RepositoryEntry });
		dialog.Show ();
	}

	private void HandleUninstallButtonClicked ()
	{
		if (current_item is null)
			throw new InvalidOperationException ($"{nameof (current_item)} is null");

		if (current_item.Addin is null)
			throw new InvalidOperationException ("The uninstall button should not be available unless there is an installed addin");

		InstallDialog dialog = new (PintaCore.Chrome.MainWindow, current_item.Service);
		dialog.OnSuccess += (_, _) => OnAddinChanged?.Invoke (this, EventArgs.Empty);
		dialog.InitForUninstall (new[] { current_item.Addin });
		dialog.Show ();
	}
}
