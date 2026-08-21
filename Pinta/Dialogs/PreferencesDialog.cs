using GObject;

namespace Pinta;

using Pinta.Core;

[GObject.Subclass<Adw.PreferencesDialog> (qualifiedName: nameof (PreferencesDialog))]
[Gtk.Template<Gtk.AssemblyResource> ("PreferencesDialog.ui")]
internal sealed partial class PreferencesDialog
{
	private ISettingsService settings = null!; // NRT - set by factory method

	[Gtk.Connect ("color_scheme_comborow")]
	private Adw.ComboRow color_scheme_row;

	public static PreferencesDialog New (ISettingsService settings)
	{
		PreferencesDialog dialog = NewWithProperties ([]);
		dialog.LoadSettings (settings);
		return dialog;
	}

	partial void Initialize ()
	{
		Adw.ComboRow.SelectedPropertyDefinition.Notify (color_scheme_row, OnColorSchemeChanged);
	}

	/// <summary>
	/// Initialize the UI widgets from the existing settings.
	/// </summary>
	private void LoadSettings (ISettingsService settingsService)
	{
		settings = settingsService;

		int schemeIndex = settings.GetSetting (SettingNames.COLOR_SCHEME, 0);
		color_scheme_row.SetSelected ((uint) schemeIndex);
	}

	private void OnColorSchemeChanged (Object sender, NotifySignalArgs args)
	{
		int schemeIndex = (int) color_scheme_row.Selected;
		settings.PutSetting (SettingNames.COLOR_SCHEME, schemeIndex);
	}
}
