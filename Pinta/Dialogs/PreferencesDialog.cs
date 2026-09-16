using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GObject;

namespace Pinta;

using Pinta.Core;

[GObject.Subclass<Adw.PreferencesDialog> (qualifiedName: nameof (PreferencesDialog))]
[Gtk.Template<Gtk.AssemblyResource> ("PreferencesDialog.ui")]
internal sealed partial class PreferencesDialog
{
	private ISettingsService settings = null!; // NRT - set by factory method
	private List<string> language_codes = [];

	[Gtk.Connect ("language_comborow")]
	private Adw.ComboRow language_row;

	[Gtk.Connect ("color_scheme_comborow")]
	private Adw.ComboRow color_scheme_row;

	[Gtk.Connect ("menubar_switchrow")]
	private Adw.SwitchRow menubar_row;

	[Gtk.Connect ("selection_anim_switchrow")]
	private Adw.SwitchRow selection_anim_row;

	public static PreferencesDialog New (ISettingsService settings)
	{
		PreferencesDialog dialog = NewWithProperties ([]);
		dialog.LoadSettings (settings);
		return dialog;
	}

	partial void Initialize ()
	{
		language_codes.Add (string.Empty); // For 'Default'.

		// Build a map from language label to language code.
		Dictionary<string, string> langMap = Translations.GetAvailableLanguages ()
			.ToDictionary (Translations.GetLanguageDisplayName, lang => lang);

		// Add the available languages to the combobox, ordered by label.
		Gtk.StringList langModel = (Gtk.StringList) language_row.Model;
		foreach (string lang in langMap.Keys.Order ()) {
			langModel.Append (lang);
			language_codes.Add (langMap[lang]);
		}

		Adw.ComboRow.SelectedPropertyDefinition.Notify (language_row, OnLanguageChanged);
		Adw.ComboRow.SelectedPropertyDefinition.Notify (color_scheme_row, OnColorSchemeChanged);
		Adw.SwitchRow.ActivePropertyDefinition.Notify (menubar_row, OnMenuBarChanged);
		Adw.SwitchRow.ActivePropertyDefinition.Notify (selection_anim_row, OnSelectionAnimChanged);
	}

	/// <summary>
	/// Initialize the UI widgets from the existing settings.
	/// </summary>
	private void LoadSettings (ISettingsService settingsService)
	{
		settings = settingsService;

		int langIndex = language_codes.IndexOf (settings.GetSetting (SettingNames.LANGUAGE, SettingDefaults.LANGUAGE));
		if (langIndex >= 0 && langIndex < language_codes.Count)
			language_row.SetSelected ((uint) langIndex);

		int schemeIndex = settings.GetSetting (SettingNames.COLOR_SCHEME, 0);
		color_scheme_row.SetSelected ((uint) schemeIndex);

		bool menuBarShown = settings.GetSetting (SettingNames.MENUBAR_SHOWN, SettingDefaults.MenuBarShown ());
		menubar_row.Active = menuBarShown;

		bool selectionAnimated = settings.GetSetting (
			Pinta.Core.SettingNames.CANVAS_SELECTION_ANIMATED,
			Pinta.Core.SettingDefaults.CANVAS_SELECTION_ANIMATED);
		selection_anim_row.Active = selectionAnimated;
	}

	private void OnLanguageChanged (Object sender, NotifySignalArgs args)
	{
		int langIndex = (int) language_row.Selected;
		string langCode = language_codes[langIndex];

		// Don't trigger the restart message when the setting is loaded on startup.
		string currentLang = settings.GetSetting (SettingNames.LANGUAGE, SettingDefaults.LANGUAGE);
		if (langCode == currentLang)
			return;

		settings.PutSetting (SettingNames.LANGUAGE, langCode);

		// Changing the language requires restarting Pinta.
		ShowRestartMessage ();
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

	private void OnSelectionAnimChanged (Object sender, NotifySignalArgs args)
	{
		settings.PutSetting (Pinta.Core.SettingNames.CANVAS_SELECTION_ANIMATED, selection_anim_row.Active);
	}

	private void ShowRestartMessage ()
	{
		Adw.Toast toast = Adw.Toast.New (Translations.GetString ("Please restart Pinta for the changes to take effect."));
		toast.Timeout = 2;

		AddToast (toast);
	}
}
