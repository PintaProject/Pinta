using Gio;
using Pinta.Core;

namespace Pinta.Actions;

internal sealed class MenuBarToggledAction : IActionHandler
{
	private readonly ViewActions view;
	private readonly ChromeManager chrome;

	internal MenuBarToggledAction (
		ViewActions view,
		ChromeManager chrome)
	{
		this.view = view;
		this.chrome = chrome;
	}

	void IActionHandler.Initialize ()
	{
		view.MenuBar.Toggled += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		view.MenuBar.Toggled -= Activated;
	}

	private async void Activated (bool value, bool interactive)
	{
		if (!interactive)
			return;

		// Changing the setting requires a restart since the application window is
		// constructed differently (see WindowShell). Only show this when the user
		// changes the option, not when the setting is loaded on startup!
		await chrome.ShowMessageDialog (
			chrome.MainWindow,
			Translations.GetString ("Restart Pinta"),
			Translations.GetString ("Please restart Pinta for the changes to take effect."));
	}
}

