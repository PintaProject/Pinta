using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class ToolBoxWidget : Box
	{
		public ToolBoxWidget ()
		{
			HeightRequest = 375;
			AddCssClass (AdwaitaStyles.Linked);

			// TODO-GTK4 - test the overflow behaviour once all tools are available, if there isn't enough height.

			PintaCore.Tools.ToolAdded += HandleToolAdded;
			PintaCore.Tools.ToolRemoved += HandleToolRemoved;

			Orientation = Orientation.Vertical;
			Spacing = 0;
		}

		public void AddItem (ToolBoxButton item)
		{
			var index = PintaCore.Tools.ToList ().IndexOf (item.Tool);

			Widget? prev_widget = GetFirstChild ();
			for (int i = 1; i < index; ++i)
				prev_widget = prev_widget!.GetNextSibling ();

			InsertChildAfter (item.Tool.ToolItem, prev_widget);
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
}
