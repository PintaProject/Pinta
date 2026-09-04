using Gio;
using Pinta.Core;

namespace Pinta.Actions;

internal sealed class ColorSchemeChangedAction : IActionHandler
{
	private readonly ISettingsService settings;
	internal ColorSchemeChangedAction (ISettingsService settings)
	{
		this.settings = settings;
	}

	void IActionHandler.Initialize ()
	{
		settings.SettingChanged += OnSettingChanged;

		// Load the initial color scheme setting.
		OnSettingChanged (null, new (SettingNames.COLOR_SCHEME));
	}

	void IActionHandler.Uninitialize ()
	{
		settings.SettingChanged -= OnSettingChanged;
	}

	private void OnSettingChanged (object? sender, SettingChangedEventArgs e)
	{
		if (e.Key != SettingNames.COLOR_SCHEME)
			return;

		int schemeIndex = PintaCore.Settings.GetSetting (SettingNames.COLOR_SCHEME, 0);
		Adw.ColorScheme scheme = schemeIndex switch {
			1 => Adw.ColorScheme.ForceLight,
			2 => Adw.ColorScheme.ForceDark,
			_ => Adw.ColorScheme.Default,
		};

		Adw.StyleManager.GetDefault ().SetColorScheme (scheme);
	}
}

