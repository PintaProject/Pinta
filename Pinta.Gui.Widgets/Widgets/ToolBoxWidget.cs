using System.Linq;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class ToolBoxWidget : FlowBox
{
	public ToolBoxWidget ()
	{
		HeightRequest = 375;
		AddCssClass (AdwaitaStyles.Linked);

		PintaCore.Tools.ToolAdded += HandleToolAdded;
		PintaCore.Tools.ToolRemoved += HandleToolRemoved;

		SetOrientation (Orientation.Vertical);
		ColumnSpacing = 0;
		RowSpacing = 0;
		Homogeneous = true;
		MinChildrenPerLine = 6;
		MaxChildrenPerLine = 1024; // If there is enough vertical space, only use one column
		Vexpand = false;
		Valign = Align.Start;
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
