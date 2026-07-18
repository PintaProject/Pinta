
using System;
using Pinta.Core;

namespace Pinta.Actions.File;

public sealed class SelectionSettingsAction : IActionHandler
{
	private readonly ChromeManager chrome;

	public SelectionSettingsAction (
		ChromeManager chrome)
	{
		this.chrome = chrome;
	}

	public void Initialize ()
	{
		PintaCore.Actions.File.SelectionSettings.Activated += OnActivated;
	}

	public void Uninitialize ()
	{
		PintaCore.Actions.File.SelectionSettings.Activated -= OnActivated;
	}

	private void OnActivated (object? sender, EventArgs e)
	{
		var dialog = new SelectionSettingsDialog (
			PintaCore.Chrome,
			PintaCore.Settings.GetSetting(
				Pinta.Core.SettingNames.SELECTION_ANIMATION,
				true));

		dialog.Show ();
	}
}
