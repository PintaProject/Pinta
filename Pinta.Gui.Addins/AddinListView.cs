using System;
using Gtk;
using Mono.Addins;
using Mono.Addins.Setup;
using Pinta.Core;

namespace Pinta.Gui.Addins
{
	internal class AddinListView : Gtk.ScrolledWindow
	{
		private Gio.ListStore model;
		private Gtk.SingleSelection selection_model;
		private Gtk.SignalListItemFactory factory;
		private Gtk.ListView view;

		public AddinListView ()
		{
			CanFocus = false;
			SetSizeRequest (200, 400);
			SetPolicy (PolicyType.Automatic, PolicyType.Automatic);

			model = Gio.ListStore.New (AddinListViewItem.GetGType ());

			selection_model = Gtk.SingleSelection.New (model);
			//selection_model.OnSelectionChanged ((o, args) => HandleSelectionChanged (o, args));

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

			view = ListView.New (selection_model, factory);
			view.CanFocus = false;

			SetChild (view);
		}

		public void Clear ()
		{
			model.RemoveAll ();
		}

		public void AddAddin (AddinHeader info, Addin addin, AddinStatus status)
		{
			// TODO
			model.Append (new AddinListViewItem (info, addin, status));
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
