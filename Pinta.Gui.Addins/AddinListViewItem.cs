using System;
using Gtk;
using Mono.Addins;
using Mono.Addins.Setup;
using Pinta.Core;

// The widget can display either an installed add-in, or an entry from an add-in repository.
using AddinOrRepoEntry = OneOf.OneOf<Mono.Addins.Addin, Mono.Addins.Setup.AddinRepositoryEntry>;

namespace Pinta.Gui.Addins
{
	// GObject subclass for use with Gio.ListStore
	internal class AddinListViewItem : GObject.Object
	{
		private AddinHeader info;
		private AddinStatus status;

		private AddinOrRepoEntry addin;

		public AddinListViewItem (AddinHeader info, Addin addin, AddinStatus status)
			: base (true, Array.Empty<GObject.ConstructArgument> ())
		{
			this.info = info;
			this.addin = AddinOrRepoEntry.FromT0 (addin);
			this.status = status;
		}

		public AddinListViewItem (AddinHeader info, AddinRepositoryEntry addin, AddinStatus status)
			: base (true, Array.Empty<GObject.ConstructArgument> ())
		{
			this.info = info;
			this.addin = AddinOrRepoEntry.FromT1 (addin);
			this.status = status;
		}

		public string Name => info.Name;
		public string Description => info.Description;
		public string Version => info.Version;

		public bool Enabled {
			get => addin.Match (
				addin => addin.Enabled,
				_ => throw new NotImplementedException ()
			);
			set => addin.Match (
				addin => addin.Enabled = value,
				_ => throw new NotImplementedException ()
			);
		}

		public bool CanDisable => addin.Match (
			addin => addin.Description.CanDisable,
			repo_entry => false
		);

		public string? DownloadSize {
			get {
				if (int.TryParse (info.Properties.GetPropertyValue ("DownloadSize"), out int size)) {
					float fs = ((float) size) / 1048576f;
					return fs.ToString ("0.00 MB");
				} else {
					return null;
				}
			}
		}

		public string? RepositoryName => addin.Match<string?> (
			addin => null,
			repo_entry => !string.IsNullOrEmpty (repo_entry.RepositoryName) ? repo_entry.RepositoryName : repo_entry.RepositoryUrl
		);
	}

	internal class AddinListViewItemWidget : Box
	{
		private AddinListViewItem? item;
		private Gtk.Label name_label;
		private Gtk.Label desc_label;

		public AddinListViewItemWidget ()
		{
			Spacing = 6;
			this.SetAllMargins (10);
			SetOrientation (Orientation.Vertical);

			name_label = new Gtk.Label () {
				Halign = Align.Start,
				Hexpand = true,
				Ellipsize = Pango.EllipsizeMode.End,
			};
			Append (name_label);

			desc_label = new Gtk.Label () {
				Halign = Align.Start,
				Hexpand = true,
				Ellipsize = Pango.EllipsizeMode.End,
			};
			desc_label.AddCssClass (Pinta.Core.AdwaitaStyles.Body);
			desc_label.AddCssClass (Pinta.Core.AdwaitaStyles.DimLabel);
			Append (desc_label);
		}

		// Set the widget's contents to the provided item.
		public void Update (AddinListViewItem item)
		{
			this.item = item;

			name_label.SetLabel (item.Name);
			desc_label.SetLabel (item.Description);
		}
	}
}
