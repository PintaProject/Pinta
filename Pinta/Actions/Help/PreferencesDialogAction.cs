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
	private readonly ToolManager tools;

	internal PreferencesDialogAction (
		AppActions app,
		ActionManager actions,
		ChromeManager chrome,
		ToolManager tools)
	{
		this.app = app;
		this.actions = actions;
		this.chrome = chrome;
		this.tools = tools;
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

		// --- 1. Menu & Action Shortcuts ---
		Adw.PreferencesGroup menuShortcutsGroup = Adw.PreferencesGroup.New ();
		menuShortcutsGroup.Title = Translations.GetString ("Application Commands");

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

		var commandsWithShortcuts = allCommands
			.Where (c => c.Shortcuts.Length > 0 && !string.IsNullOrEmpty(c.Label))
			.OrderBy (c => c.Label);

		foreach (var cmd in commandsWithShortcuts) {
			Adw.ActionRow row = Adw.ActionRow.New ();
			row.Title = cmd.Label.Replace ("_", "");

			if (cmd.IconName != null) {
				row.IconName = cmd.IconName;
			}

			Gtk.ShortcutLabel shortcutLabel = Gtk.ShortcutLabel.New (cmd.Shortcuts[0]);
			row.AddSuffix (shortcutLabel);
			menuShortcutsGroup.Add (row);
		}
		shortcutsPage.Add (menuShortcutsGroup);

		// --- 2. Tool Shortcuts ---
		Adw.PreferencesGroup toolShortcutsGroup = Adw.PreferencesGroup.New ();
		toolShortcutsGroup.Title = Translations.GetString ("Tools");

		// tool.ShortcutKey returns a Gdk.Key. We filter out the 'invalid/void' keys.
		var toolsWithShortcuts = tools
			.Where (t => t.ShortcutKey.Value != 0 && t.ShortcutKey.Value != Gdk.Constants.KEY_VoidSymbol)
			.OrderBy (t => t.Name);

		foreach (var tool in toolsWithShortcuts) {
			Adw.ActionRow row = Adw.ActionRow.New ();
			row.Title = tool.Name;
			row.IconName = tool.Icon;

			// Gtk.ShortcutLabel natively understands strings like "B" or "M"
			string keyName = ((char)tool.ShortcutKey.Value).ToString ().ToUpperInvariant ();
			Gtk.ShortcutLabel shortcutLabel = Gtk.ShortcutLabel.New (keyName);
			row.AddSuffix (shortcutLabel);
			toolShortcutsGroup.Add (row);
		}
		shortcutsPage.Add (toolShortcutsGroup);

		dialog.Add (shortcutsPage);

		await dialog.PresentAsync ();
	}

	private static IEnumerable<Command> GetCommands(object actionCollection)
	{
		return actionCollection.GetType()
			.GetProperties()
			.Where(p => p.PropertyType == typeof(Command))
			.Select(p => (Command)p.GetValue(actionCollection)!)
			.Where(c => c != null);
	}
}
