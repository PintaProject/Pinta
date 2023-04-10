using System;
using System.Linq;
using System.Threading.Tasks;
using Mono.Addins;
using Mono.Addins.Setup;

namespace Pinta.Gui.Addins
{
	public class AddinManagerDialog : Adw.Window
	{
		private SetupService service;
		private AddinListView installed_list;
		private AddinListView updates_list;
		private AddinListView gallery_list;

		private StatusProgressBar progress_bar;

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

			var content = Gtk.Box.New (Gtk.Orientation.Vertical, 0);
			content.Append (header_bar);
			progress_bar = new StatusProgressBar (view_stack);
			content.Append (progress_bar);
			Content = content;

			// TODO - set icons for these panes
			installed_list = new AddinListView ();
			view_stack.AddTitled (installed_list, null, Pinta.Core.Translations.GetString ("Installed"));
			updates_list = new AddinListView ();
			view_stack.AddTitled (updates_list, "updates", Pinta.Core.Translations.GetString ("Updates"));
			gallery_list = new AddinListView ();
			view_stack.AddTitled (gallery_list, null, Pinta.Core.Translations.GetString ("Gallery"));

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
				GLib.Functions.IdleAddFull (0, (_) => {

					progress_bar.HideProgress ();
					LoadGallery ();

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
#if false // TODO
					if (IsFiltered (ah))
						continue;
#endif
					AddinStatus st = AddinStatus.Installed;
					if (!ainfo.Enabled || Utilities.GetMissingDependencies (ainfo).Any ())
						st |= AddinStatus.Disabled;
#if false // TODO
					if (addininfoInstalled.GetUpdate (ainfo) != null)
						st |= AddinStatus.HasUpdate;
#endif
					installed_list.AddAddin (ah, ainfo, st);
				}
			}

		}

		private void LoadGallery ()
		{
			gallery_list.Clear ();

			// TODO - support filtering the list of repositories.
			AddinRepositoryEntry[] reps = service.Repositories.GetAvailableAddins (RepositorySearchFlags.LatestVersionsOnly);

			foreach (var arep in reps) {
				if (!Utilities.InApplicationNamespace (service, arep.Addin.Id))
					continue;

#if false // TODO
				if (IsFiltered (arep.Addin))
					continue;
#endif

				AddinStatus status = AddinStatus.NotInstalled;

				// Find whatever version is installed
				Addin sinfo = AddinManager.Registry.GetAddin (Addin.GetIdName (arep.Addin.Id));

				if (sinfo != null) {
					status |= AddinStatus.Installed;
					if (!sinfo.Enabled || Utilities.GetMissingDependencies (sinfo).Any ())
						status |= AddinStatus.Disabled;
					if (Addin.CompareVersions (sinfo.Version, arep.Addin.Version) > 0)
						status |= AddinStatus.HasUpdate;
				}

				gallery_list.AddAddinRepositoryEntry (arep.Addin, arep, status);
			}
		}
	}
}
