using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GObject;

namespace Pinta;

using Pinta.Core;
using Pinta.Gui.Widgets;

[GObject.Subclass<Adw.PreferencesDialog> (qualifiedName: nameof (PreferencesDialog))]
[Gtk.Template<Gtk.AssemblyResource> ("PreferencesDialog.ui")]
internal sealed partial class PreferencesDialog
{
	private ISettingsService settings = null!; // NRT - set by factory method
	private IChromeService chrome = null!;
	private IPaletteService palette = null!;
	private List<string> language_codes = [];

	[Gtk.Connect ("language_comborow")]
	private Adw.ComboRow language_row;

	[Gtk.Connect ("color_scheme_comborow")]
	private Adw.ComboRow color_scheme_row;

	[Gtk.Connect ("menubar_switchrow")]
	private Adw.SwitchRow menubar_row;

	[Gtk.Connect ("selection_anim_switchrow")]
	private Adw.SwitchRow selection_anim_row;

	[Gtk.Connect ("startup_width_spinrow")]
	private Adw.SpinRow startup_width_row;

	[Gtk.Connect ("startup_height_spinrow")]
	private Adw.SpinRow startup_height_row;

	[Gtk.Connect ("startup_background_comborow")]
	private Adw.ComboRow startup_background_row;

	[Gtk.Connect ("startup_color_row")]
	private Adw.ActionRow startup_color_row;

	private PintaColorButton startup_color_button = null!;

	public static PreferencesDialog New (IChromeService chrome, IPaletteService palette, ISettingsService settings)
	{
		PreferencesDialog dialog = NewWithProperties ([]);
		dialog.chrome = chrome;
		dialog.palette = palette;
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
		Adw.SpinRow.ValuePropertyDefinition.Notify (startup_width_row, OnStartupWidthChanged);
		Adw.SpinRow.ValuePropertyDefinition.Notify (startup_height_row, OnStartupHeightChanged);
		Adw.ComboRow.SelectedPropertyDefinition.Notify (startup_background_row, OnStartupBackgroundChanged);

		startup_color_button = PintaColorButton.New ();
		startup_color_button.Valign = Gtk.Align.Center;
		startup_color_button.Hexpand = false;
		startup_color_button.WidthRequest = 80;
		startup_color_button.OnClicked += async (_, _) => await ChooseStartupColor ();
		startup_color_row.AddSuffix (startup_color_button);
		startup_color_row.ActivatableWidget = startup_color_button;
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

		startup_width_row.Value = settings.GetSetting (SettingNames.STARTUP_IMAGE_WIDTH, 800);
		startup_height_row.Value = settings.GetSetting (SettingNames.STARTUP_IMAGE_HEIGHT, 600);
		startup_background_row.SetSelected ((uint) settings.GetSetting (SettingNames.STARTUP_IMAGE_BACKGROUND, (int) BackgroundType.White));

		string colorHex = settings.GetSetting (SettingNames.STARTUP_IMAGE_BACKGROUND_COLOR, Cairo.Color.Black.ToHex ());
		startup_color_button.DisplayColor = Cairo.Color.FromHex (colorHex) ?? Cairo.Color.Black;
		startup_color_row.Visible = startup_background_row.Selected == (uint) BackgroundType.SecondaryColor;
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

	private void OnStartupWidthChanged (Object sender, NotifySignalArgs args)
	{
		settings.PutSetting (SettingNames.STARTUP_IMAGE_WIDTH, (int) startup_width_row.Value);
	}

	private void OnStartupHeightChanged (Object sender, NotifySignalArgs args)
	{
		settings.PutSetting (SettingNames.STARTUP_IMAGE_HEIGHT, (int) startup_height_row.Value);
	}

	private void OnStartupBackgroundChanged (Object sender, NotifySignalArgs args)
	{
		settings.PutSetting (SettingNames.STARTUP_IMAGE_BACKGROUND, (int) startup_background_row.Selected);
		startup_color_row.Visible = startup_background_row.Selected == (uint) BackgroundType.SecondaryColor;
	}

	private async Task ChooseStartupColor ()
	{
		using ColorPickerDialog dialog = ColorPickerDialog.New (
			chrome.MainWindow,
			palette,
			new SingleColor (startup_color_button.DisplayColor),
			primarySelected: true,
			false,
			Translations.GetString ("Choose Color"));

		// The picker needs this to receive input from Preferences on macOS
		dialog.Modal = true;

		try {
			Gtk.ResponseType response = await dialog.RunAsync ();

			if (response != Gtk.ResponseType.Ok)
				return;

			Cairo.Color color = ((SingleColor) dialog.Colors).Color;
			startup_color_button.DisplayColor = color;
			settings.PutSetting (SettingNames.STARTUP_IMAGE_BACKGROUND_COLOR, color.ToHex ());
		} finally {
			dialog.Destroy ();
		}
	}

	private void ShowRestartMessage ()
	{
		Adw.Toast toast = Adw.Toast.New (Translations.GetString ("Please restart Pinta for the changes to take effect."));
		toast.Timeout = 2;

		AddToast (toast);
	}
}
