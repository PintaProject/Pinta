using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Addins;
using Pinta.Core;

namespace Pinta.Gui.Addins;

[GObject.Subclass<Adw.Bin> (qualifiedName: nameof (AddinInfoView))]
[Gtk.Template<Gtk.AssemblyResource> ("AddinInfoView.ui")]
internal sealed partial class AddinInfoView
{
	[Gtk.Connect (nameof (title_label))]
	private Gtk.Label title_label;
	[Gtk.Connect (nameof (category_label))]
	private Gtk.Label category_label;
	[Gtk.Connect (nameof (author_label))]
	private Gtk.Label author_label;
	[Gtk.Connect (nameof (version_label))]
	private Gtk.Label version_label;
	[Gtk.Connect (nameof (size_label))]
	private Gtk.Label size_label;
	[Gtk.Connect (nameof (repo_label))]
	private Gtk.Label repo_label;
	[Gtk.Connect (nameof (description_label))]
	private Gtk.Label description_label;

	[Gtk.Connect (nameof (info_button))]
	private Gtk.Button info_button;
	[Gtk.Connect (nameof (install_button))]
	private Gtk.Button install_button;
	[Gtk.Connect (nameof (update_button))]
	private Gtk.Button update_button;
	[Gtk.Connect (nameof (uninstall_button))]
	private Gtk.Button uninstall_button;

	[Gtk.Connect (nameof (enable_switch))]
	private Gtk.Switch enable_switch;

	[Gtk.Connect (nameof (content_box))]
	private Gtk.Box content_box;

	[Gtk.Connect (nameof (empty_page))]
	private Adw.Bin empty_page;

	[Gtk.Connect (nameof (view_stack))]
	private Adw.ViewStack view_stack;

	private AddinListViewItem? current_item;

	/// <summary>
	/// Event raised when addins are installed or uninstalled.
	/// </summary>
	public event EventHandler? OnAddinChanged;

	private SystemManager system = null!; // NRT - set by factory method.
	private IChromeService chrome = null!;

	partial void Initialize ()
	{
		info_button.OnClicked += (_, _) => HandleInfoButtonClicked ();
		install_button.OnClicked += (_, _) => HandleInstallButtonClicked ();
		update_button.OnClicked += (_, _) => HandleUpdateButtonClicked ();
		uninstall_button.OnClicked += (_, _) => HandleUninstallButtonClicked ();

		enable_switch.OnStateSet += (_, _) => {
			HandleEnableSwitched ();
			return false;
		};
	}

	internal void Configure (SystemManager system, IChromeService chrome)
	{
		this.system = system;
		this.chrome = chrome;
	}

	public static new AddinInfoView New () => NewWithProperties ([]);

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
		category_label.SetLabel (Translations.GetString ("Category: {0}", item.Category));
		author_label.SetLabel (Translations.GetString ("Author: {0}", item.Author));
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
