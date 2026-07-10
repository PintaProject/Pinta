//
// KeyboardShortcutsDialogAction.cs
//

using System;
using System.Collections.Generic;
using System.Linq;
using Pinta.Core;

namespace Pinta.Actions;

internal sealed class KeyboardShortcutsDialogAction : IActionHandler
{
	private readonly AppActions app;
	private readonly ActionManager actions;
	private readonly ChromeManager chrome;
	private readonly ToolManager tools;

	internal KeyboardShortcutsDialogAction (
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
		app.KeyboardShortcuts.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		app.KeyboardShortcuts.Activated -= Activated;
	}

	private void Activated (object sender, EventArgs e)
	{
		using Adw.ShortcutsDialog dialog = Adw.ShortcutsDialog.New ();

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

		// Helper to create and populate a section and add it to the dialog
		void AddSection (string sectionTitle, IEnumerable<Command> commands)
		{
			var validCommands = commands
				.Where (c => c.Shortcuts.Length > 0 && !string.IsNullOrEmpty (c.Label))
				.OrderBy (c => c.Label)
				.ToList ();

			if (validCommands.Count == 0)
				return;

			Adw.ShortcutsSection section = Adw.ShortcutsSection.New (sectionTitle);

			foreach (var cmd in validCommands) {
				string title = cmd.Label.Replace ("_", "");
				string accel = FormatShortcut (cmd.Shortcuts[0]);
				Adw.ShortcutsItem item = Adw.ShortcutsItem.New (title, accel);
				section.Add (item);
			}

			dialog.Add (section);
		}

		// 1. Tool Shortcuts
		var toolShortcutsSection = Adw.ShortcutsSection.New (Translations.GetString ("Tools"));

		// tool.ShortcutKey returns a Gdk.Key. We filter out the 'invalid/void' keys.
		var toolsWithShortcuts = tools
			.Where (t => t.ShortcutKey.Value != 0 && t.ShortcutKey.Value != Gdk.Constants.KEY_VoidSymbol)
			.OrderBy (t => t.Name);

		foreach (var tool in toolsWithShortcuts) {
			string keyName = ((char) tool.ShortcutKey.Value).ToString ().ToUpperInvariant ();
			Adw.ShortcutsItem item = Adw.ShortcutsItem.New (tool.Name, keyName);
			toolShortcutsSection.Add (item);
		}

		dialog.Add (toolShortcutsSection);

		// 2. Layer Commands
		AddSection (Translations.GetString ("Layers"), GetCommands (actions.Layers));

		// 3. Menu Commands
		AddSection (Translations.GetString ("File"), GetCommands (actions.File));
		AddSection (Translations.GetString ("Edit"), GetCommands (actions.Edit));
		AddSection (Translations.GetString ("View"), GetCommands (actions.View));
		AddSection (Translations.GetString ("Image"), GetCommands (actions.Image));
		AddSection (Translations.GetString ("Adjustments"), actions.Adjustments.Actions);
		AddSection (Translations.GetString ("Effects"), actions.Effects.Actions);
		AddSection (Translations.GetString ("Window"), GetCommands (actions.Window));
		AddSection (Translations.GetString ("Help"), GetCommands (actions.Help));

		dialog.Present (chrome.MainWindow);
	}

	private static IEnumerable<Command> GetCommands (object actionCollection)
	{
		return actionCollection.GetType ()
			.GetProperties ()
			.Where (p => p.PropertyType == typeof (Command))
			.Select (p => (Command) p.GetValue (actionCollection)!)
			.Where (c => c != null);
	}
}
