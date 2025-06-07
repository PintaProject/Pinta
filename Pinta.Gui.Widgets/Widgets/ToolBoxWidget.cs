using System.Linq;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class ToolBoxWidget : FlowBox
{
	public ToolBoxWidget ()
	{
		PintaCore.Tools.ToolAdded += HandleToolAdded;
		PintaCore.Tools.ToolRemoved += HandleToolRemoved;

		SetOrientation (Orientation.Vertical);
		MinChildrenPerLine = 8; // Pinta 3 has 22 default tools, meaning a max of 3 columns regardless of size, smaller values don't lead to better use of visual space.
		MaxChildrenPerLine = 1024; // Allow for single column if there's sufficient space to do so.
	}

	public void AddItem (ToolBoxButton item)
	{
		var index = PintaCore.Tools.ToList ().IndexOf (item.Tool);
		Insert (item.Tool.ToolItem, index);
	}

	public void RemoveItem (ToolBoxButton item)
	{
		Remove (item);
	}

	private void HandleToolAdded (object? sender, ToolEventArgs e)
	{
		AddItem (e.Tool.ToolItem);
	}

	private void HandleToolRemoved (object? sender, ToolEventArgs e)
	{
		RemoveItem (e.Tool.ToolItem);
	}
}
