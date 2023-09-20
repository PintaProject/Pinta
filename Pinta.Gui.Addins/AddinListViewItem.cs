using System;
using Gtk;
using Mono.Addins;
using Mono.Addins.Setup;
using Pinta.Core;

namespace Pinta.Gui.Addins;

// GObject subclass for use with Gio.ListStore
internal sealed class AddinListViewItem : GObject.Object
{
	private readonly SetupService service;
	private readonly AddinHeader info;
	private readonly AddinStatus status;
	private readonly Addin? installed_addin;
	private readonly AddinRepositoryEntry? available_addin;

	/// <summary>
	/// Constructor for the list of installed addins.
	/// </summary>
	public AddinListViewItem (SetupService service, AddinHeader info, Addin installed_addin, AddinStatus status)
		: base (true, Array.Empty<GObject.ConstructArgument> ())
	{
		this.service = service;
		this.info = info;
		this.installed_addin = installed_addin;
		this.status = status;
	}

	/// <summary>
	/// Constructor for the gallery view of available add-ins, some of which may already be installed.
	/// </summary>
	public AddinListViewItem (SetupService service, AddinHeader info, AddinRepositoryEntry available_addin, AddinStatus status)
		: base (true, Array.Empty<GObject.ConstructArgument> ())
	{
		this.service = service;
		this.info = info;
		this.available_addin = available_addin;
		this.status = status;

		installed_addin = AddinManager.Registry.GetAddin (Addin.GetIdName (info.Id));
	}

	public SetupService Service => service;

	public string Name => info.Name;
	public string Description => info.Description;
	public string Version => info.Version;
	public string Url => info.Url;

	public bool Installed => installed_addin is not null;
	public Addin? Addin => installed_addin;
	public AddinRepositoryEntry? RepositoryEntry => available_addin;

	public bool CanDisable => installed_addin?.Description.CanDisable ?? false;
	public bool Enabled {
		get => installed_addin!.Enabled;
		set => installed_addin!.Enabled = value;
	}

	public bool CanUninstall => installed_addin?.Description.CanUninstall ?? false;

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

	public string? RepositoryName => available_addin == null ? null :
		!string.IsNullOrEmpty (available_addin.RepositoryName) ? available_addin.RepositoryName : available_addin.RepositoryUrl;
}

internal sealed class AddinListViewItemWidget : Box
{
	private readonly Gtk.Label name_label;
	private readonly Gtk.Label desc_label;

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
		name_label.SetLabel (item.Name);
		desc_label.SetLabel (item.Description);
	}
}
