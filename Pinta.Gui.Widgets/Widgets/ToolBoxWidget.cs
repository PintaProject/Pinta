using System.Linq;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class ToolBoxWidget : Gtk.FlowBox
{
	private readonly ToolManager tools;

	public ToolBoxWidget (ToolManager tools)
	{
		// --- Initialization (Gtk.FlowBox)

		SetOrientation (Gtk.Orientation.Vertical);
		MinChildrenPerLine = 8; // Pinta 3 has 22 default tools, meaning a max of 3 columns regardless of size, smaller values don't lead to better use of visual space.
		MaxChildrenPerLine = 1024; // Allow for single column if there's sufficient space to do so.

		// --- References to keep

		this.tools = tools;
	}

	public void AddItem (BaseTool tool)
	{
		var index = tools.ToList ().IndexOf (tool);
		Insert (tool.ToolItem, index);
	}

	public void RemoveItem (BaseTool tool)
	{
		Remove (tool.ToolItem);
	}
}
