using System.Linq;
using Gdk;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class ToolBoxWidget : FlowBox
{
	public ToolBoxWidget ()
	{
		PintaCore.Tools.ToolAdded += HandleToolAdded;
		PintaCore.Tools.ToolRemoved += HandleToolRemoved;

		// Force images to have a specific size regardless of size of the application canvas
		// Discussion #1374 where icons seems to be getting smaller when the screen gets bigger!
		Gtk.CssProvider css = Gtk.CssProvider.New ();
		css.LoadFromString (".ToolBoxWidget { -gtk-icon-size: 2rem; }"); // Works as well for high resolution and low resolution (Tested 1440P 100% scaling)
		Gdk.Display? display = Gdk.Display.GetDefault () ?? null;
		if (display is not null) {
			Gtk.StyleContext.AddProviderForDisplay (display, css, 1);
		}

		SetOrientation (Orientation.Vertical);
		MinChildrenPerLine = 8; // Pinta 3 has 22 default tools, meaning a max of 3 columns regardless of size, smaller values don't lead to better use of visual space.
		MaxChildrenPerLine = 1024;
	}

	public void AddItem (ToolBoxButton item)
	{
		// Despite .Flat already being set, if you use .AddCSSClass("ToolBoxWidget"), stuff doesn't work, so we set both at once.
		item.Tool.ToolItem.SetCssClasses (["ToolBoxWidget", AdwaitaStyles.Flat]);
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
