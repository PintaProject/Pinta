using System;
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
	public AddinListViewItem (
		SetupService service,
		AddinHeader info,
		Addin installed_addin,
		AddinStatus status)
	: base (
		true,
		Array.Empty<GObject.ConstructArgument> ())
	{
		this.service = service;
		this.info = info;
		this.installed_addin = installed_addin;
		this.status = status;
	}

	/// <summary>
	/// Constructor for the gallery view of available add-ins, some of which may already be installed.
	/// </summary>
	public AddinListViewItem (
		SetupService service,
		AddinHeader info,
		AddinRepositoryEntry available_addin,
		AddinStatus status)
	: base (
		true,
		Array.Empty<GObject.ConstructArgument> ())
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
			if (!int.TryParse (info.Properties.GetPropertyValue ("DownloadSize"), out int size))
				return null;

			float fs = size / 1048576f;
			return fs.ToString ("0.00 MB");
		}
	}

	public string? RepositoryName => available_addin == null ? null :
		!string.IsNullOrEmpty (available_addin.RepositoryName) ? available_addin.RepositoryName : available_addin.RepositoryUrl;
}

internal sealed class AddinListViewItemWidget : Gtk.Box
{
	private readonly Gtk.Label name_label;
	private readonly Gtk.Label description_label;

	public AddinListViewItemWidget ()
	{
		Gtk.Label nameLabel = new () {
			Halign = Gtk.Align.Start,
			Hexpand = true,
			Ellipsize = Pango.EllipsizeMode.End,
		};

		Gtk.Label descriptionLabel = new () {
			Halign = Gtk.Align.Start,
			Hexpand = true,
			Ellipsize = Pango.EllipsizeMode.End,
		};
		descriptionLabel.AddCssClass (AdwaitaStyles.Body);
		descriptionLabel.AddCssClass (AdwaitaStyles.DimLabel);

		// --- References to keep

		name_label = nameLabel;
		description_label = descriptionLabel;

		// --- Post-initialization

		Spacing = 6;

		SetOrientation (Gtk.Orientation.Vertical);

		this.SetAllMargins (10);

		Append (nameLabel);
		Append (descriptionLabel);
	}

	// Set the widget's contents to the provided item.
	public void Update (AddinListViewItem item)
	{
		name_label.SetLabel (item.Name);
		description_label.SetLabel (item.Description);
	}
}
