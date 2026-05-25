//
// PreferencesDialogAction.cs
//

using System;
using System.Collections.Generic;
using System.Linq;
using Pinta.Core;

namespace Pinta.Actions;

internal sealed class PreferencesDialogAction : IActionHandler
{
	private readonly AppActions app;
	private readonly ActionManager actions;
	private readonly ChromeManager chrome;

	internal PreferencesDialogAction (
		AppActions app,
		ActionManager actions,
		ChromeManager chrome)
	{
		this.app = app;
		this.actions = actions;
		this.chrome = chrome;
	}

	void IActionHandler.Initialize ()
	{
		app.Preferences.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		app.Preferences.Activated -= Activated;
	}

	private async void Activated (object sender, EventArgs e)
	{
		using Adw.PreferencesWindow dialog = Adw.PreferencesWindow.New ();
		dialog.TransientFor = chrome.MainWindow;
		dialog.Title = Translations.GetString ("Preferences");
		dialog.DefaultWidth = 600;
		dialog.DefaultHeight = 800;

		Adw.PreferencesPage shortcutsPage = Adw.PreferencesPage.New ();
		shortcutsPage.Title = Translations.GetString ("Keyboard Shortcuts");
		shortcutsPage.IconName = "keyboard-shortcuts-symbolic";

		Adw.PreferencesGroup shortcutsGroup = Adw.PreferencesGroup.New ();
		shortcutsGroup.Title = Translations.GetString ("Application Shortcuts");

		// Gather all commands from Pinta's action manager
		var allCommands = new List<Command> ();
		allCommands.AddRange (GetCommands (actions.App));
		allCommands.AddRange (GetCommands (actions.File));
		allCommands.AddRange (GetCommands (actions.Edit));
		allCommands.AddRange (GetCommands (actions.View));
		allCommands.AddRange (GetCommands (actions.Image));
		allCommands.AddRange (GetCommands (actions.Layers));
		allCommands.AddRange (GetCommands (actions.Window));
		allCommands.AddRange (GetCommands (actions.Help));
		allCommands.AddRange (GetCommands (actions.Addins));

		// Sort alphabetically by label and filter out commands without shortcuts
		var commandsWithShortcuts = allCommands
			.Where (c => c.Shortcuts.Length > 0 && !string.IsNullOrEmpty(c.Label))
			.OrderBy (c => c.Label);

		foreach (var cmd in commandsWithShortcuts) {
			Adw.ActionRow row = Adw.ActionRow.New ();
			row.Title = cmd.Label.Replace ("_", ""); // Remove GTK mnemonic underscores

			if (cmd.IconName != null) {
				row.IconName = cmd.IconName;
			}

			// Use a Gtk.ShortcutLabel to display the keys nicely
			Gtk.ShortcutLabel shortcutLabel = Gtk.ShortcutLabel.New (cmd.Shortcuts[0]);
			row.AddSuffix (shortcutLabel);

			shortcutsGroup.Add (row);
		}

		shortcutsPage.Add (shortcutsGroup);
		dialog.Add (shortcutsPage);

		await dialog.PresentAsync ();
	}

	private static IEnumerable<Command> GetCommands(object actionCollection)
	{
		// Simple reflection to grab all 'Command' properties from the action classes (FileActions, EditActions, etc.)
		return actionCollection.GetType()
			.GetProperties()
			.Where(p => p.PropertyType == typeof(Command))
			.Select(p => (Command)p.GetValue(actionCollection)!)
			.Where(c => c != null);
	}
}
