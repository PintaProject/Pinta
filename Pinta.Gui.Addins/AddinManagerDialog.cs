using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mono.Addins;
using Mono.Addins.Setup;
using Pinta.Core;
using Pinta.Resources;

namespace Pinta.Gui.Addins;

public sealed class AddinManagerDialog : Adw.Window
{
	private readonly SetupService setup_service;
	private readonly SystemManager system;

	private readonly AddinListView installed_list;
	private readonly AddinListView updates_list;
	private readonly AddinListView gallery_list;

	private readonly StatusProgressBar progress_bar;

	public AddinManagerDialog (
		Gtk.Window parent,
		SetupService service,
		SystemManager system)
	{
		// TODO - add a dialog for managing the list of repositories.
		// TODO - support searching through the gallery

		// --- Component creation

		Gtk.Button installFileButton = CreateInstallFileButton ();
		Gtk.Button refreshButton = CreateRefreshButton ();

		AddinListView galleryList = CreateAddinList ();
		AddinListView installedList = CreateAddinList ();
		AddinListView updatesList = CreateAddinList ();

		Adw.ViewStack viewStack = CreateViewStack (galleryList, installedList, updatesList);

		Adw.ToastOverlay toastOverlay = new ();
		StatusProgressBar progressBar = new (viewStack, new ToastErrorReporter (toastOverlay));
		toastOverlay.Child = progressBar;

		Adw.ViewSwitcherTitle viewSwitcherTitle = Adw.ViewSwitcherTitle.New ();
		viewSwitcherTitle.Stack = viewStack;

		Adw.HeaderBar headerBar = Adw.HeaderBar.New ();
		headerBar.CenteringPolicy = Adw.CenteringPolicy.Strict;
		headerBar.TitleWidget = viewSwitcherTitle;
		headerBar.PackStart (installFileButton);
		headerBar.PackStart (refreshButton);

		Gtk.Box content = Gtk.Box.New (Gtk.Orientation.Vertical, 0);
		content.Append (headerBar);
		content.Append (toastOverlay);

		// --- Property assignment (GTK window)

		TransientFor = parent;

		// --- Property assignment (Adwaita window)

		Content = content;

		// --- References to keep

		this.setup_service = service;
		this.system = system;

		progress_bar = progressBar;

		gallery_list = galleryList;
		installed_list = installedList;
		updates_list = updatesList;

		// --- Post-initialization

		LoadAll ();
	}

	private static Adw.ViewStack CreateViewStack (
		AddinListView galleryList,
		AddinListView installedList,
		AddinListView updatesList)
	{
		Adw.ViewStack result = Adw.ViewStack.New ();
		result.AddTitledWithIcon (galleryList, null, Translations.GetString ("Gallery"), StandardIcons.SystemSoftwareInstall);
		result.AddTitledWithIcon (installedList, null, Translations.GetString ("Installed"), StandardIcons.ApplicationAddon);
		result.AddTitledWithIcon (updatesList, "updates", Translations.GetString ("Updates"), StandardIcons.SoftwareUpdateAvailable);
		return result;
	}

	private AddinListView CreateAddinList ()
	{
		AddinListView result = new (system);
		result.OnAddinChanged += (_, _) => LoadAll ();
		return result;
	}

	private Gtk.Button CreateRefreshButton ()
	{
		Gtk.Button result = Gtk.Button.NewFromIconName (StandardIcons.ViewRefresh);
		result.TooltipText = Translations.GetString ("Refresh");
		result.OnClicked += (_, _) => LoadAll ();
		return result;
	}

	private Gtk.Button CreateInstallFileButton ()
	{
		Gtk.Button result = Gtk.Button.NewFromIconName (StandardIcons.DocumentOpen);
		result.TooltipText = Translations.GetString ("Install from file...");
		result.OnClicked += (_, _) => HandleInstallFromFileClicked ();
		return result;
	}

	private void LoadAll ()
	{
		LoadInstalled ();

		// First update the available addins in a background thread, since this involves network access.
		progress_bar.ShowProgress ();

		Task.Run (() => {
			setup_service.Repositories.UpdateAllRepositories (progress_bar);
		}).ContinueWith (_ => {
			// Execute UI updates on the main thread.
			GLib.Functions.IdleAdd (
				0,
				() => {
					progress_bar.HideProgress ();
					LoadGallery ();
					LoadUpdates ();
					return false;
				}
			);
		});
	}

	private void LoadInstalled ()
	{
		installed_list.Clear ();

		foreach (Addin ainfo in AddinManager.Registry.GetModules (AddinSearchFlags.IncludeAddins | AddinSearchFlags.LatestVersionsOnly)) {

			if (!Utilities.InApplicationNamespace (setup_service, ainfo.Id) || ainfo.Description.IsHidden)
				continue;

			AddinHeader ah = SetupService.GetAddinHeader (ainfo);

			AddinStatus st = AddinStatus.Installed;
			if (!ainfo.Enabled || Utilities.GetMissingDependencies (ainfo).Any ())
				st |= AddinStatus.Disabled;
#if false // TODO
			if (addininfoInstalled.GetUpdate (ainfo) != null)
				st |= AddinStatus.HasUpdate;
#endif
			installed_list.AddAddin (setup_service, ah, ainfo, st);
		}

	}

	private void LoadGallery ()
	{
		gallery_list.Clear ();

		IReadOnlyList<AddinRepositoryEntry> reps = setup_service.Repositories.GetAvailableAddins (RepositorySearchFlags.None);
		reps = FilterToLatestCompatibleVersion (reps);

		foreach (var arep in reps) {

			if (!Utilities.InApplicationNamespace (setup_service, arep.Addin.Id))
				continue;

			AddinStatus status = AddinStatus.NotInstalled;

			// Find whatever version is installed
			Addin? sinfo = AddinManager.Registry.GetAddin (Addin.GetIdName (arep.Addin.Id));

			if (sinfo != null) {

				status |= AddinStatus.Installed;

				if (!sinfo.Enabled || Utilities.GetMissingDependencies (sinfo).Any ())
					status |= AddinStatus.Disabled;

				if (Addin.CompareVersions (sinfo.Version, arep.Addin.Version) > 0)
					status |= AddinStatus.HasUpdate;
			}

			gallery_list.AddAddinRepositoryEntry (setup_service, arep.Addin, arep, status);
		}
	}

	private void LoadUpdates ()
	{
		updates_list.Clear ();

		IReadOnlyList<AddinRepositoryEntry> reps = setup_service.Repositories.GetAvailableAddins (RepositorySearchFlags.None);
		reps = FilterToLatestCompatibleVersion (reps);

		foreach (var arep in reps) {
			if (!Utilities.InApplicationNamespace (setup_service, arep.Addin.Id))
				continue;

			// Check if this addin is installed and is an earlier version.
			Addin? installed = AddinManager.Registry.GetAddin (Addin.GetIdName (arep.Addin.Id));
			if (installed is null || !installed.Enabled || Addin.CompareVersions (installed.Version, arep.Addin.Version) <= 0)
				continue;

			AddinStatus status = AddinStatus.Installed | AddinStatus.HasUpdate;
			if (!installed.Enabled || Utilities.GetMissingDependencies (installed).Any ())
				status |= AddinStatus.Disabled;

			updates_list.AddAddinRepositoryEntry (setup_service, arep.Addin, arep, status);
		}
	}

	// Similar to RepositoryRegistry.FilterOldVersions(), but also filters out newer versions that require an
	// updated version of the application.
	private static IReadOnlyList<AddinRepositoryEntry> FilterToLatestCompatibleVersion (IReadOnlyList<AddinRepositoryEntry> addins)
	{
		Dictionary<string, string> latest_versions = new ();
		foreach (var a in addins) {
			if (!Utilities.IsCompatibleWithAddinRoots (a))
				continue;

			Addin.GetIdParts (a.Addin.Id, out string id, out string version);
			if (!latest_versions.TryGetValue (id, out string? last) || Addin.CompareVersions (last, version) > 0)
				latest_versions[id] = version;
		}

		var filtered_addins = addins.Where (a => {
			Addin.GetIdParts (a.Addin.Id, out string id, out string version);
			return latest_versions.TryGetValue (id, out string? latest_version) && latest_version == version;
		}).ToArray ();

		Array.Sort (filtered_addins);

		return filtered_addins;
	}

	private async void HandleInstallFromFileClicked ()
	{
		using Gtk.FileFilter mpackFilter = CreateMpackFilter ();
		using Gtk.FileFilter catchAllFilter = CreateCatchAllFilter ();

		using Gtk.FileChooserNative dialog = Gtk.FileChooserNative.New (
			Translations.GetString ("Install Extension Package"),
			this,
			Gtk.FileChooserAction.Open,
			Translations.GetString ("Open"),
			Translations.GetString ("Cancel"));
		dialog.Modal = true;
		dialog.SelectMultiple = true;
		dialog.AddFilter (mpackFilter);
		dialog.AddFilter (catchAllFilter);

		Gtk.ResponseType response = await dialog.ShowAsync ();

		if (response != Gtk.ResponseType.Accept)
			return;

		IReadOnlyList<string> files =
			dialog.GetFileList ()
			.Select (f => f.GetPath () ?? string.Empty)
			.Where (f => !string.IsNullOrEmpty (f))
			.ToArray ();

		InstallDialog install_dialog = new (this, setup_service); // TODO: dispose properly after making async
		if (install_dialog.InitForInstall (files)) {
			install_dialog.OnSuccess += (_, _) => LoadAll ();
			install_dialog.Show ();
		}

		// --- Utility methods

		static Gtk.FileFilter CreateMpackFilter ()
		{
			Gtk.FileFilter result = Gtk.FileFilter.New ();
			result.AddPattern ("*.mpack");
			result.Name = Translations.GetString ("Extension packages");
			return result;
		}

		static Gtk.FileFilter CreateCatchAllFilter ()
		{
			Gtk.FileFilter result = Gtk.FileFilter.New ();
			result.AddPattern ("*");
			result.Name = Translations.GetString ("All files");
			return result;
		}
	}
}

internal sealed class ToastErrorReporter : IErrorReporter
{
	private readonly Adw.ToastOverlay toast_overlay;

	public ToastErrorReporter (Adw.ToastOverlay toast_overlay)
	{
		this.toast_overlay = toast_overlay;
	}

	public void ReportError (string message, Exception exception)
	{
		Console.WriteLine ($"Error: {message}\n{exception}");

		GLib.Functions.IdleAdd (
			0,
			() => {
				toast_overlay.AddToast (Adw.Toast.New (message));
				return false;
			}
		);
	}

	public void ReportWarning (string message)
	{
		Console.WriteLine ($"Warning: {message}");

		GLib.Functions.IdleAdd (
			0,
			() => {
				toast_overlay.AddToast (Adw.Toast.New (message));
				return false;
			}
		);
	}
}
