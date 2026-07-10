using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Addins;
using Pinta.Core;

namespace Pinta.Gui.Addins;

[GObject.Subclass<Adw.Bin>]
internal sealed partial class AddinInfoView
{
	private Gtk.Label title_label;
	private Gtk.Label version_label;
	private Gtk.Label size_label;
	private Gtk.Label repo_label;
	private Gtk.Label description_label;

	private Gtk.Button info_button;
	private Gtk.Button install_button;
	private Gtk.Button update_button;
	private Gtk.Button uninstall_button;

	private Gtk.Switch enable_switch;

	private Gtk.Box content_box;

	private Adw.Bin empty_page;

	private Adw.ViewStack view_stack;
	private AddinListViewItem? current_item;

	/// <summary>
	/// Event raised when addins are installed or uninstalled.
	/// </summary>
	public event EventHandler? OnAddinChanged;

	private SystemManager system = null!; // NRT - set by factory method.
	private IChromeService chrome = null!;

	[MemberNotNull (nameof (title_label))]
	[MemberNotNull (nameof (version_label))]
	[MemberNotNull (nameof (size_label))]
	[MemberNotNull (nameof (repo_label))]
	[MemberNotNull (nameof (description_label))]
	[MemberNotNull (nameof (info_button))]
	[MemberNotNull (nameof (install_button))]
	[MemberNotNull (nameof (update_button))]
	[MemberNotNull (nameof (uninstall_button))]
	[MemberNotNull (nameof (enable_switch))]
	[MemberNotNull (nameof (content_box))]
	[MemberNotNull (nameof (empty_page))]
	[MemberNotNull (nameof (view_stack))]
	partial void Initialize ()
	{
		// --- Control creation

		Gtk.Label titleLabel = Gtk.Label.New (null);
		titleLabel.Halign = Gtk.Align.Start;
		titleLabel.AddCssClass (AdwaitaStyles.Title4);

		Gtk.Label versionLabel = Gtk.Label.New (null);
		versionLabel.Halign = Gtk.Align.Start;
		versionLabel.AddCssClass (AdwaitaStyles.Heading);

		Gtk.Label sizeLabel = Gtk.Label.New (null);
		sizeLabel.Halign = Gtk.Align.Start;
		sizeLabel.AddCssClass (AdwaitaStyles.Heading);

		Gtk.Label repoLabel = Gtk.Label.New (null);
		repoLabel.Halign = Gtk.Align.Start;
		repoLabel.AddCssClass (AdwaitaStyles.Heading);

		Gtk.Label descriptionLabel = CreateDescriptionLabel ();

		Adw.Bin emptyPage = Adw.Bin.New ();

		Gtk.Button infoButton = CreateInfoButton ();
		Gtk.Button installButton = CreateInstallButton ();
		Gtk.Button updateButton = CreateUpdateButton ();
		Gtk.Button uninstallButton = CreateUninstallButton ();

		Gtk.Switch enableSwitch = CreateEnableSwitch ();

		BoxStyle spacedHorizontal = new (
			orientation: Gtk.Orientation.Horizontal,
			spacing: 6,
			cssClass: AdwaitaStyles.Toolbar);
		Gtk.Box hbox = GtkExtensions.Box (
			spacedHorizontal,
			[
				enableSwitch,
				installButton,
				updateButton,
				infoButton,
				uninstallButton
			]
		);

		BoxStyle spacedVertical = new (
			orientation: Gtk.Orientation.Vertical,
			spacing: 10);
		Gtk.Box contentBox = GtkExtensions.Box (
			spacedVertical,
			[
				titleLabel,
				versionLabel,
				sizeLabel,
				repoLabel,
				descriptionLabel,
				hbox
			]
		);
		contentBox.SetAllMargins (10);

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
	}

	internal void Configure (SystemManager system, IChromeService chrome)
	{
		this.system = system;
		this.chrome = chrome;
	}

	public static new AddinInfoView New () => NewWithProperties ([]);

	private Gtk.Switch CreateEnableSwitch ()
	{
		Gtk.Switch result = Gtk.Switch.New ();
		result.Visible = false;
		result.OnStateSet += (_, _) => {
			HandleEnableSwitched ();
			return false;
		};
		return result;
	}

	private static Gtk.Label CreateDescriptionLabel ()
	{
		Gtk.Label result = Gtk.Label.New (null);
		result.Halign = Gtk.Align.Start;
		result.Hexpand = true;
		result.Valign = Gtk.Align.Start;
		result.Vexpand = true;
		result.Xalign = 0;
		result.Wrap = true;
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

	private async void HandleInfoButtonClicked ()
	{
		await system.LaunchUri (current_item!.Url);
	}

	private void HandleInstallButtonClicked ()
	{
		if (current_item is null)
			throw new InvalidOperationException ($"{nameof (current_item)} is null");

		if (current_item.RepositoryEntry is null)
			throw new InvalidOperationException ("The install button should not be available unless there is a repository entry");

		InstallDialog dialog = InstallDialog.New (chrome.MainWindow, current_item.Service);
		dialog.OnSuccess += (_, _) => OnAddinChanged?.Invoke (this, EventArgs.Empty);
		dialog.InitForInstall ([current_item.RepositoryEntry]);
		dialog.Show ();
	}

	private void HandleUpdateButtonClicked ()
	{
		if (current_item is null)
			throw new InvalidOperationException ($"{nameof (current_item)} is null");

		if (current_item.RepositoryEntry is null)
			throw new InvalidOperationException ("The update button should not be available unless there is a repository entry");

		InstallDialog dialog = InstallDialog.New (chrome.MainWindow, current_item.Service);
		dialog.OnSuccess += (_, _) => OnAddinChanged?.Invoke (this, EventArgs.Empty);
		dialog.InitForInstall ([current_item.RepositoryEntry]);
		dialog.Show ();
	}

	private void HandleUninstallButtonClicked ()
	{
		if (current_item is null)
			throw new InvalidOperationException ($"{nameof (current_item)} is null");

		if (current_item.Addin is null)
			throw new InvalidOperationException ("The uninstall button should not be available unless there is an installed addin");

		InstallDialog dialog = InstallDialog.New (chrome.MainWindow, current_item.Service);
		dialog.OnSuccess += (_, _) => OnAddinChanged?.Invoke (this, EventArgs.Empty);
		dialog.InitForUninstall ([current_item.Addin]);
		dialog.Show ();
	}
}
