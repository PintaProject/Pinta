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

	[Gtk.Connect ("menubar_switchrow")]
	private Adw.SwitchRow menubar_row;

	public static PreferencesDialog New (ISettingsService settings)
	{
		PreferencesDialog dialog = NewWithProperties ([]);
		dialog.LoadSettings (settings);
		return dialog;
	}

	partial void Initialize ()
	{
		Adw.ComboRow.SelectedPropertyDefinition.Notify (color_scheme_row, OnColorSchemeChanged);

		Adw.SwitchRow.ActivePropertyDefinition.Notify (menubar_row, OnMenuBarChanged);
	}

	/// <summary>
	/// Initialize the UI widgets from the existing settings.
	/// </summary>
	private void LoadSettings (ISettingsService settingsService)
	{
		settings = settingsService;

		int schemeIndex = settings.GetSetting (SettingNames.COLOR_SCHEME, 0);
		color_scheme_row.SetSelected ((uint) schemeIndex);

		bool menuBarShown = settings.GetSetting (SettingNames.MENUBAR_SHOWN, SettingDefaults.MenuBarShown ());
		menubar_row.Active = menuBarShown;
	}

	private void OnColorSchemeChanged (Object sender, NotifySignalArgs args)
	{
		int schemeIndex = (int) color_scheme_row.Selected;
		settings.PutSetting (SettingNames.COLOR_SCHEME, schemeIndex);
	}

	private void OnMenuBarChanged (Object sender, NotifySignalArgs args)
	{
		// Don't trigger the restart message when the setting is loaded on startup.
		if (menubar_row.Active == settings.GetSetting (SettingNames.MENUBAR_SHOWN, SettingDefaults.MenuBarShown ()))
			return;

		settings.PutSetting (SettingNames.MENUBAR_SHOWN, menubar_row.Active);

		// Changing the setting requires a restart since the application window is
		// constructed differently (see WindowShell).
		ShowRestartMessage ();
	}

	private void ShowRestartMessage ()
	{
		Adw.Toast toast = Adw.Toast.New (Translations.GetString ("Please restart Pinta for the changes to take effect."));
		toast.Timeout = 2;

		AddToast (toast);
	}
}
