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

		// Helper to format the shortcut string for the current OS
		string FormatShortcut (string shortcut)
		{
			bool isMac = SystemManager.GetOperatingSystem () == OS.Mac;

			// 1. Map <Primary> to the main modifier (Cmd on Mac, Ctrl on Win/Lin)
			// 2. Normalize <Ctrl> to <Control> for GTK consistency
			return shortcut
				.Replace ("<Primary>", isMac ? "<Meta>" : "<Control>")
				.Replace ("<Ctrl>", "<Control>");
		}

		// Helper to create and populate a group
		void AddGroup (string title, IEnumerable<Command> commands)
		{
			var validCommands = commands
				.Where (c => c.Shortcuts.Length > 0 && !string.IsNullOrEmpty(c.Label))
				.OrderBy (c => c.Label)
				.ToList ();

			if (validCommands.Count == 0)
				return;

			Adw.PreferencesGroup group = Adw.PreferencesGroup.New ();
			group.Title = Translations.GetString (title);

			foreach (var cmd in validCommands) {
				Adw.ActionRow row = Adw.ActionRow.New ();
				row.Title = cmd.Label.Replace ("_", "");

				if (cmd.IconName != null) {
					row.IconName = cmd.IconName;
				}

				string formattedShortcut = FormatShortcut (cmd.Shortcuts[0]);
				Gtk.ShortcutLabel shortcutLabel = Gtk.ShortcutLabel.New (formattedShortcut);
				row.AddSuffix (shortcutLabel);
				group.Add (row);
			}

			shortcutsPage.Add (group);
		}

		// 1. Tool Shortcuts
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

			string keyName = ((char)tool.ShortcutKey.Value).ToString ().ToUpperInvariant ();
			Gtk.ShortcutLabel shortcutLabel = Gtk.ShortcutLabel.New (keyName);
			row.AddSuffix (shortcutLabel);
			toolShortcutsGroup.Add (row);
		}

		shortcutsPage.Add (toolShortcutsGroup);

		// 2. Layer Commands
		AddGroup ("Layers", GetCommands (actions.Layers));

		// 3. Menu Commands
		AddGroup ("File", GetCommands (actions.File));
		AddGroup ("Edit", GetCommands (actions.Edit));
		AddGroup ("View", GetCommands (actions.View));
		AddGroup ("Image", GetCommands (actions.Image));
		AddGroup ("Adjustments", actions.Adjustments.Actions);
		AddGroup ("Effects", actions.Effects.Actions);
		AddGroup ("Window", GetCommands (actions.Window));
		AddGroup ("Help", GetCommands (actions.Help));

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
