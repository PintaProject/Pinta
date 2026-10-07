namespace Pinta.Actions;

using System;
using Pinta.Core;

internal sealed class PreferencesDialogAction : IActionHandler
{
	private readonly AppActions app;
	private readonly IChromeService chrome;
	private readonly IPaletteService palette;
	private readonly ISettingsService settings;

	internal PreferencesDialogAction (
	    AppActions app,
	    IChromeService chrome,
			IPaletteService palette,
	    ISettingsService settings)
	{
		this.app = app;
		this.chrome = chrome;
		this.palette = palette;
		this.settings = settings;
	}

	void IActionHandler.Initialize ()
	{
		app.Preferences.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		app.Preferences.Activated -= Activated;
	}

	private void Activated (object sender, EventArgs e)
	{
		using PreferencesDialog dialog = PreferencesDialog.New (chrome, palette, settings);
		dialog.Present (chrome.MainWindow);
	}
}
