using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class ToolBoxWidget : Toolbar
	{
		public ToolBoxWidget ()
		{
			HeightRequest = 375;

			PintaCore.Tools.ToolAdded += HandleToolAdded;
			PintaCore.Tools.ToolRemoved += HandleToolRemoved;

			Orientation = Orientation.Vertical;
			ToolbarStyle = ToolbarStyle.Icons;

			ShowAll ();
		}

		public void AddItem (ToolBoxButton item)
		{
			item.IsImportant = false;

			var index = PintaCore.Tools.ToList ().IndexOf (item.Tool);

			Insert (item.Tool.ToolItem, index);
		}

		public void RemoveItem (ToolButton item)
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
}
