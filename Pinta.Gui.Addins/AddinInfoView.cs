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
	[Gtk.Connect (nameof (category_row))]
	private Adw.ActionRow category_row;
	[Gtk.Connect (nameof (version_row))]
	private Adw.ActionRow version_row;
	[Gtk.Connect (nameof (author_row))]
	private Adw.ActionRow author_row;
	[Gtk.Connect (nameof (size_row))]
	private Adw.ActionRow size_row;
	[Gtk.Connect (nameof (repo_row))]
	private Adw.ActionRow repo_row;
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
		category_row.Subtitle = item.Category;
		version_row.Subtitle = item.Version;
		author_row.Subtitle = item.Author;
		description_label.SetLabel (item.Description);

		size_row.Visible = item.DownloadSize != null;
		size_row.Subtitle = item.DownloadSize ?? string.Empty;

		repo_row.Visible = item.RepositoryName != null;
		repo_row.Subtitle = item.RepositoryName ?? string.Empty;

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
