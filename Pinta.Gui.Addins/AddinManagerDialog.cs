using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mono.Addins;
using Mono.Addins.Description;
using Mono.Addins.Setup;
using Pinta.Core;
using Pinta.Resources;

namespace Pinta.Gui.Addins
{
	public class AddinManagerDialog : Adw.Window
	{
		private readonly SetupService service;
		private readonly AddinListView installed_list;
		private readonly AddinListView updates_list;
		private readonly AddinListView gallery_list;

		private readonly Adw.ToastOverlay toast_overlay = new ();
		private readonly StatusProgressBar progress_bar;

		private readonly Gtk.Button install_file_button;
		private readonly Gtk.Button refresh_button;

		public AddinManagerDialog (Gtk.Window parent, SetupService service)
		{
			this.service = service;

			TransientFor = parent;

			var view_stack = Adw.ViewStack.New ();
			var view_switcher_title = Adw.ViewSwitcherTitle.New ();
			view_switcher_title.Stack = view_stack;

			var header_bar = Adw.HeaderBar.New ();
			header_bar.CenteringPolicy = Adw.CenteringPolicy.Strict;
			header_bar.TitleWidget = view_switcher_title;

			install_file_button = Gtk.Button.NewFromIconName (StandardIcons.DocumentOpen);
			install_file_button.TooltipText = Translations.GetString ("Install from file...");
			install_file_button.OnClicked += (_, _) => HandleInstallFromFileClicked ();
			header_bar.PackStart (install_file_button);

			refresh_button = Gtk.Button.NewFromIconName (StandardIcons.ViewRefresh);
			refresh_button.TooltipText = Translations.GetString ("Refresh");
			refresh_button.OnClicked += (_, _) => LoadAll ();
			header_bar.PackStart (refresh_button);

			// TODO - add a dialog for managing the list of repositories.
			// TODO - support searching through the gallery

			var content = Gtk.Box.New (Gtk.Orientation.Vertical, 0);
			content.Append (header_bar);
			progress_bar = new StatusProgressBar (view_stack, new ToastErrorReporter (toast_overlay));
			toast_overlay.Child = progress_bar;
			content.Append (toast_overlay);
			Content = content;

			installed_list = new AddinListView ();
			view_stack.AddTitledWithIcon (installed_list, null, Translations.GetString ("Installed"), StandardIcons.ApplicationAddon);
			updates_list = new AddinListView ();
			view_stack.AddTitledWithIcon (updates_list, "updates", Translations.GetString ("Updates"), StandardIcons.SoftwareUpdateAvailable);
			gallery_list = new AddinListView ();
			view_stack.AddTitledWithIcon (gallery_list, null, Translations.GetString ("Gallery"), StandardIcons.SystemSoftwareInstall);

			installed_list.OnAddinChanged += (_, _) => LoadAll ();
			updates_list.OnAddinChanged += (_, _) => LoadAll ();
			gallery_list.OnAddinChanged += (_, _) => LoadAll ();

			LoadAll ();
		}

		private void LoadAll ()
		{
			LoadInstalled ();

			// First update the available addins in a background thread, since this involves network access.
			progress_bar.ShowProgress ();
			Task.Run (() => {
				service.Repositories.UpdateAllRepositories (progress_bar);
			}).ContinueWith ((_) => {
				// Execute UI updates on the main thread.
				GLib.Functions.IdleAdd (0, () => {

					progress_bar.HideProgress ();
					LoadGallery ();
					LoadUpdates ();

					return false;
				});
			});
		}

		private void LoadInstalled ()
		{
			installed_list.Clear ();

			foreach (Addin ainfo in AddinManager.Registry.GetModules (AddinSearchFlags.IncludeAddins | AddinSearchFlags.LatestVersionsOnly)) {
				if (Utilities.InApplicationNamespace (service, ainfo.Id) && !ainfo.Description.IsHidden) {
					AddinHeader ah = SetupService.GetAddinHeader (ainfo);

					AddinStatus st = AddinStatus.Installed;
					if (!ainfo.Enabled || Utilities.GetMissingDependencies (ainfo).Any ())
						st |= AddinStatus.Disabled;
#if false // TODO
					if (addininfoInstalled.GetUpdate (ainfo) != null)
						st |= AddinStatus.HasUpdate;
#endif
					installed_list.AddAddin (service, ah, ainfo, st);
				}
			}

		}

		private void LoadGallery ()
		{
			gallery_list.Clear ();

			AddinRepositoryEntry[] reps = service.Repositories.GetAvailableAddins (RepositorySearchFlags.None);
			reps = FilterToLatestCompatibleVersion (reps);

			foreach (var arep in reps) {
				if (!Utilities.InApplicationNamespace (service, arep.Addin.Id))
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

				gallery_list.AddAddinRepositoryEntry (service, arep.Addin, arep, status);
			}
		}

		private void LoadUpdates ()
		{
			updates_list.Clear ();

			AddinRepositoryEntry[] reps = service.Repositories.GetAvailableAddins (RepositorySearchFlags.None);
			reps = FilterToLatestCompatibleVersion (reps);

			foreach (var arep in reps) {
				if (!Utilities.InApplicationNamespace (service, arep.Addin.Id))
					continue;

				// Check if this addin is installed and is an earlier version.
				Addin? installed = AddinManager.Registry.GetAddin (Addin.GetIdName (arep.Addin.Id));
				if (installed is null || !installed.Enabled || Addin.CompareVersions (installed.Version, arep.Addin.Version) <= 0)
					continue;

				AddinStatus status = AddinStatus.Installed | AddinStatus.HasUpdate;
				if (!installed.Enabled || Utilities.GetMissingDependencies (installed).Any ())
					status |= AddinStatus.Disabled;

				updates_list.AddAddinRepositoryEntry (service, arep.Addin, arep, status);
			}
		}

		// Similar to RepositoryRegistry.FilterOldVersions(), but also filters out newer versions that require an
		// updated version of the application.
		private AddinRepositoryEntry[] FilterToLatestCompatibleVersion (AddinRepositoryEntry[] addins)
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

		private void HandleInstallFromFileClicked ()
		{
			var dialog = Gtk.FileChooserNative.New (
				Translations.GetString ("Install Extension Package"),
				this,
				Gtk.FileChooserAction.Open,
				Translations.GetString ("Open"),
				Translations.GetString ("Cancel"));
			dialog.Modal = true;
			dialog.SelectMultiple = true;

			var filter = Gtk.FileFilter.New ();
			filter.AddPattern ("*.mpack");
			filter.Name = Translations.GetString ("Extension packages");
			dialog.AddFilter (filter);

			filter = Gtk.FileFilter.New ();
			filter.AddPattern ("*");
			filter.Name = Translations.GetString ("All files");
			dialog.AddFilter (filter);

			dialog.OnResponse += (_, e) => {
				if (e.ResponseId != (int) Gtk.ResponseType.Accept)
					return;

				string[] files = dialog.GetFileList ()
					.Select (f => f.GetPath () ?? string.Empty)
					.Where (f => !string.IsNullOrEmpty (f))
					.ToArray ();
				var install_dialog = new InstallDialog (this, service);
				install_dialog.OnSuccess += (_, _) => LoadAll ();
				if (install_dialog.InitForInstall (files))
					install_dialog.Show ();
			};

			dialog.Show ();
		}
	}

	internal class ToastErrorReporter : IErrorReporter
	{
		private readonly Adw.ToastOverlay toast_overlay;

		public ToastErrorReporter (Adw.ToastOverlay toast_overlay)
		{
			this.toast_overlay = toast_overlay;
		}

		public void ReportError (string message, Exception exception)
		{
			Console.WriteLine ("Error: {0}\n{1}", message, exception);

			GLib.Functions.IdleAdd (0, () => {
				toast_overlay.AddToast (Adw.Toast.New (message));
				return false;
			});
		}

		public void ReportWarning (string message)
		{
			Console.WriteLine ("Warning: {0}", message);

			GLib.Functions.IdleAdd (0, () => {
				toast_overlay.AddToast (Adw.Toast.New (message));
				return false;
			});
		}
	}
}
