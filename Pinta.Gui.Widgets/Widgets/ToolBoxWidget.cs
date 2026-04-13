using System.Collections.Generic;
using System.Linq;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class ToolBoxWidget : Gtk.FlowBox
{
	private readonly ToolManager tools;
	// Stores the button corresponding to each tool.
	private readonly Dictionary<BaseTool, Gtk.ToggleButton> tool_buttons = new ();
	// Dummy ToggleButton to use for grouping together the tools' buttons.
	private readonly Gtk.ToggleButton toggle_group = Gtk.ToggleButton.New ();

	public ToolBoxWidget (ToolManager tools)
	{
		tools.ToolAdded += (_, e) => HandleToolAdded (e.Tool);
		tools.ToolRemoved += (_, e) => HandleToolRemoved (e.Tool);
		tools.ToolActivated += (_, e) => HandleToolActivated (e.Tool);

		// --- Initialization (Gtk.FlowBox)

		SetOrientation (Gtk.Orientation.Vertical);
		MinChildrenPerLine = 8; // Pinta 3 has 22 default tools, meaning a max of 3 columns regardless of size, smaller values don't lead to better use of visual space.
		MaxChildrenPerLine = 1024; // Allow for single column if there's sufficient space to do so.
		SelectionMode = Gtk.SelectionMode.None; // Don't allow the buttons to be selected.

		// --- References to keep

		this.tools = tools;
	}

	private static Gtk.ToggleButton CreateToolButton (BaseTool tool)
	{
		Gtk.ToggleButton button = Gtk.ToggleButton.New ();
		button.IconName = tool.Icon;
		button.Name = tool.Name;
		button.CanFocus = false;

		button.SetCssClasses ([Resources.Styles.ToolBoxButton, AdwaitaStyles.Flat]);

		string shortcutText = "";
		if (tool.ShortcutKey != Gdk.Key.Invalid) {
			string shortcutLabel = Translations.GetString ("Shortcut key");
			shortcutText = $"{shortcutLabel}: {tool.ShortcutKey.ToUpper ().Name ()}\n";
		}

		button.TooltipText = $"{tool.Name}\n{shortcutText}\n{tool.StatusBarText}";

		return button;
	}

	private void HandleToolAdded (BaseTool tool)
	{
		Gtk.ToggleButton toolButton = CreateToolButton (tool);
		toolButton.Group = toggle_group;
		toolButton.OnClicked += (_, _) => HandleToolButtonClicked (tool);
		tool_buttons[tool] = toolButton;

		int index = tools.ToList ().IndexOf (tool);
		Insert (toolButton, index);
	}

	private void HandleToolButtonClicked (BaseTool tool)
	{
		tools.SetCurrentTool (tool);
	}

	/// <summary>
	/// If the tool was switched without clicking on the button (e.g. via shortcut key),
	/// ensure the tool's button is active. Note we don't need to deactivate the previous
	/// button since they're all in the same toggle button group.
	/// </summary>
	private void HandleToolActivated (BaseTool tool)
	{
		Gtk.ToggleButton toolButton = tool_buttons[tool];
		toolButton.Active = true;
	}

	private void HandleToolRemoved (BaseTool tool)
	{
		Gtk.ToggleButton toolButton = tool_buttons[tool];
		Remove (toolButton);
	}
}
