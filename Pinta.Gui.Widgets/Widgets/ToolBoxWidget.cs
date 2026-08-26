using System.Collections.Generic;
using System.Linq;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

[GObject.Subclass<Adw.WrapBox>]
public sealed partial class ToolBoxWidget
{
	private ToolManager tools = null!; // NRT - set in factory method
					   // Stores the button corresponding to each tool.
	private readonly Dictionary<BaseTool, Gtk.ToggleButton> tool_buttons = new ();
	// Dummy ToggleButton to use for grouping together the tools' buttons.
	private readonly Gtk.ToggleButton toggle_group = Gtk.ToggleButton.New ();

	partial void Initialize ()
	{
		SetOrientation (Gtk.Orientation.Vertical);
	}

	public static ToolBoxWidget New (ToolManager tools)
	{
		ToolBoxWidget widget = NewWithProperties ([]);
		widget.Configure (tools);
		return widget;
	}

	private void Configure (ToolManager tools)
	{
		tools.ToolAdded += (_, e) => HandleToolAdded (e.Tool);
		tools.ToolRemoved += (_, e) => HandleToolRemoved (e.Tool);
		tools.ToolActivated += (_, e) => HandleToolActivated (e.Tool);

		this.tools = tools;
	}

	private static Gtk.ToggleButton CreateToolButton (BaseTool tool)
	{
		Gtk.ToggleButton button = Gtk.ToggleButton.New ();
		button.IconName = tool.Icon;
		button.Name = tool.Name;
		button.FocusOnClick = false;

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

		List<BaseTool> toolList = tools.ToList ();
		int prevIndex = toolList.IndexOf (tool) - 1;
		if (prevIndex >= 0) {
			BaseTool prevTool = toolList[prevIndex];
			Widget? prevSibling = tool_buttons[prevTool];
			InsertChildAfter (toolButton, prevSibling);	
		} else {
			Prepend (toolButton);
		}
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
