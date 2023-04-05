using System;
using System.Linq;
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

		public AddinManagerDialog (Gtk.Window parent, SetupService service, bool allow_install)
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
			content.Append (view_stack);
			Content = content;

			// TODO - handle allow_install and service
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

	}
}
