namespace Pinta.Actions;

using System;
using Pinta.Core;

internal sealed class PreferencesDialogAction : IActionHandler
{
	private readonly AppActions app;
	private readonly IChromeService chrome;
	private readonly ISettingsService settings;

	internal PreferencesDialogAction (
	    AppActions app,
	    IChromeService chrome,
	    ISettingsService settings)
	{
		this.app = app;
		this.chrome = chrome;
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
		using PreferencesDialog dialog = PreferencesDialog.New (settings);
		dialog.Present (chrome.MainWindow);
	}
}
