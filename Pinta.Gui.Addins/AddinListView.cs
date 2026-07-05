using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Addins;
using Mono.Addins.Setup;
using Pinta.Core;
using Pinta.Resources;

namespace Pinta.Gui.Addins;

[GObject.Subclass<Adw.Bin>]
internal sealed partial class AddinListView
{
	private Gio.ListStore model;
	private Gtk.SingleSelection selection_model;

	private Adw.StatusPage empty_list_page;
	private Gtk.ScrolledWindow list_view_scroll;
	private Adw.ViewStack list_view_stack;

	private AddinInfoView info_view;

	/// <summary>
	/// Event raised when addins are installed or uninstalled.
	/// </summary>
	public event EventHandler? OnAddinChanged;

	[MemberNotNull (nameof (model), nameof (selection_model))]
	[MemberNotNull (nameof (empty_list_page), nameof (list_view_scroll), nameof (list_view_stack))]
	[MemberNotNull (nameof (info_view))]
	partial void Initialize ()
	{
		Gio.ListStore listStore = Gio.ListStore.New (AddinListViewItem.GetGType ());

		Gtk.SingleSelection selectionModel = Gtk.SingleSelection.New (listStore);
		selectionModel.OnSelectionChanged += (_, _) => HandleSelectionChanged ();
		selectionModel.Autoselect = true;

		Gtk.SignalListItemFactory itemFactory = Gtk.SignalListItemFactory.New ();
		itemFactory.OnSetup += (factory, args) => {
			var item = (Gtk.ListItem) args.Object;
			item.SetChild (AddinListViewItemWidget.New ());
		};
		itemFactory.OnBind += (factory, args) => {
			var list_item = (Gtk.ListItem) args.Object;
			var model_item = (AddinListViewItem) list_item.GetItem ()!;
			var widget = (AddinListViewItemWidget) list_item.GetChild ()!;
			widget.Update (model_item);
		};

		// TODO: have an option to group by category like the old GTK2 addin dialog.
		Gtk.ListView listView = Gtk.ListView.New (selectionModel, itemFactory);

		Gtk.ScrolledWindow listViewScroll = Gtk.ScrolledWindow.New ();
		listViewScroll.SetChild (listView);
		listViewScroll.SetSizeRequest (300, 400);
		listViewScroll.SetPolicy (Gtk.PolicyType.Automatic, Gtk.PolicyType.Automatic);

		Adw.StatusPage emptyListPage = Adw.StatusPage.New ();
		emptyListPage.IconName = StandardIcons.SystemSearch;
		emptyListPage.Title = Translations.GetString ("No Items Found");
		emptyListPage.AddCssClass (AdwaitaStyles.Compact);

		Adw.ViewStack listViewStack = Adw.ViewStack.New ();
		listViewStack.Add (listViewScroll);
		listViewStack.Add (emptyListPage);

		AddinInfoView infoView = AddinInfoView.New ();
		infoView.OnAddinChanged += (o, e) => OnAddinChanged?.Invoke (o, e);

		Adw.Flap flap = Adw.Flap.New ();
		flap.FoldPolicy = Adw.FlapFoldPolicy.Never;
		flap.Locked = true;
		flap.Content = listViewStack;
		flap.Separator = Gtk.Separator.New (Gtk.Orientation.Vertical);
		flap.FlapPosition = Gtk.PackType.End;
		flap.SetFlap (infoView);

		// --- References to keep

		model = listStore;
		selection_model = selectionModel;
		list_view_scroll = listViewScroll;
		empty_list_page = emptyListPage;
		list_view_stack = listViewStack;
		info_view = infoView;

		// --- Post-initialization

		SetChild (flap);
	}

	internal void Configure (SystemManager system, IChromeService chrome)
	{
		info_view.Configure (system, chrome);
	}

	public static new AddinListView New ()
	{
		AddinListView view = NewWithProperties ([]);
		return view;
	}

	public void Clear ()
	{
		model.RemoveAll ();
		list_view_stack.VisibleChild = empty_list_page;
		info_view.Update (null);
	}

	public void AddAddin (
		SetupService service,
		AddinHeader info,
		Addin addin,
		AddinStatus status)
	{
		list_view_stack.VisibleChild = list_view_scroll;

		model.Append (AddinListViewItem.NewForInstalledAddin (service, info, addin, status));

		// Adding items may not cause a selection-changed signal, as mentioned in the SelectionModel docs
		if (model.NItems == 1)
			HandleSelectionChanged ();
	}

	public void AddAddinRepositoryEntry (
		SetupService service,
		AddinHeader info,
		AddinRepositoryEntry addin,
		AddinStatus status)
	{
		list_view_stack.VisibleChild = list_view_scroll;

		model.Append (AddinListViewItem.NewForAvailableAddin (service, info, addin, status));

		// Adding items may not cause a selection-changed signal, as mentioned in the SelectionModel docs
		if (model.NItems == 1)
			HandleSelectionChanged ();
	}

	private void HandleSelectionChanged ()
	{
		if (model.GetObject (selection_model.Selected) is AddinListViewItem item)
			info_view.Update (item);
		else
			info_view.Update (null);
	}
}

[Flags]
internal enum AddinStatus
{
	NotInstalled = 0,
	Installed = 1,
	Disabled = 2,
	HasUpdate = 4,
}
