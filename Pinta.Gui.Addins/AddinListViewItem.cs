using System;
using Gtk;
using Mono.Addins;
using Mono.Addins.Setup;
using Pinta.Core;

namespace Pinta.Gui.Addins
{
	// GObject subclass for use with Gio.ListStore
	internal class AddinListViewItem : GObject.Object
	{
		private AddinHeader info;
		private Addin addin;
		private AddinStatus status;

		public AddinListViewItem (AddinHeader info, Addin addin, AddinStatus status)
			: base (true, Array.Empty<GObject.ConstructArgument> ())
		{
			this.info = info;
			this.addin = addin;
			this.status = status;
		}

		public string Name => info.Name;
		public string Description => info.Description;
		public string Version => info.Version;

		public bool Enabled {
			get => addin.Enabled;
			set => addin.Enabled = value;
		}
		public bool CanDisable => addin.Description.CanDisable;
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
