using System;
using Gtk;
using Mono.Addins;
using Mono.Addins.Setup;
using Pinta.Core;

namespace Pinta.Gui.Addins
{
	internal class AddinListView : Adw.Bin
	{
		private Gio.ListStore model;
		private Gtk.SingleSelection selection_model;
		private Gtk.SignalListItemFactory factory;
		private Gtk.ListView list_view;
		private AddinInfoView info_view;

		public AddinListView ()
		{
			model = Gio.ListStore.New (AddinListViewItem.GetGType ());

			selection_model = Gtk.SingleSelection.New (model);
			selection_model.OnSelectionChanged ((_, _) => HandleSelectionChanged ());
			selection_model.Autoselect = true;

			factory = Gtk.SignalListItemFactory.New ();
			factory.OnSetup += (factory, args) => {
				var item = (Gtk.ListItem) args.Object;
				item.SetChild (new AddinListViewItemWidget ());
			};
			factory.OnBind += (factory, args) => {
				var list_item = (Gtk.ListItem) args.Object;
				var model_item = (AddinListViewItem) list_item.GetItem ()!;
				var widget = (AddinListViewItemWidget) list_item.GetChild ()!;
				widget.Update (model_item);
			};

			list_view = ListView.New (selection_model, factory);

			var list_view_scroll = Gtk.ScrolledWindow.New ();
			list_view_scroll.SetChild (list_view);
			list_view_scroll.SetSizeRequest (300, 400);
			list_view_scroll.SetPolicy (PolicyType.Automatic, PolicyType.Automatic);

			info_view = new AddinInfoView ();

			var flap = Adw.Flap.New ();
			flap.FoldPolicy = Adw.FlapFoldPolicy.Never;
			flap.Locked = true;
			flap.Content = list_view_scroll;
			flap.Separator = Gtk.Separator.New (Orientation.Vertical);
			flap.FlapPosition = PackType.End;
			flap.SetFlap (info_view);
			SetChild (flap);
		}

		public void Clear ()
		{
			model.RemoveAll ();
		}

		public void AddAddin (SetupService service, AddinHeader info, Addin addin, AddinStatus status)
		{
			model.Append (new AddinListViewItem (service, info, addin, status));

			// Adding items may not cause a selection-changed signal, as mentioned in the SelectionModel docs
			if (model.NItems == 1)
				HandleSelectionChanged ();
		}

		public void AddAddinRepositoryEntry (SetupService service, AddinHeader info, AddinRepositoryEntry addin, AddinStatus status)
		{
			model.Append (new AddinListViewItem (service, info, addin, status));

			// Adding items may not cause a selection-changed signal, as mentioned in the SelectionModel docs
			if (model.NItems == 1)
				HandleSelectionChanged ();
		}

		private void HandleSelectionChanged ()
		{
			if (model.GetObject (selection_model.Selected) is AddinListViewItem item)
				info_view.Update (item);
		}
	}

	[Flags]
	internal enum AddinStatus
	{
		NotInstalled = 0,
		Installed = 1,
		Disabled = 2,
		HasUpdate = 4
	}
}
