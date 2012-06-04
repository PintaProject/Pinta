using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem (true)]
	public class ToolBoxWidget : HBox
	{
		private Toolbar tb1;
		private Toolbar tb2;
		
		public ToolBoxWidget ()
		{
			// First column
			tb1 = new Toolbar () {
				Name = "tb1",
				Orientation = Orientation.Vertical,
				ShowArrow = false,
				ToolbarStyle = ToolbarStyle.Icons,
				IconSize = IconSize.SmallToolbar
			};

			PackStart (tb1, false, false, 0);
			
			// second column
			tb2 = new Toolbar () {
				Name = "tb2",
				Orientation = Orientation.Vertical,
				ShowArrow = false,
				ToolbarStyle = ToolbarStyle.Icons,
				IconSize = IconSize.SmallToolbar
			};

			PackStart (tb2, false, false, 0);
			
			PintaCore.Tools.ToolAdded += HandleToolAdded;
			PintaCore.Tools.ToolRemoved += HandleToolRemoved;

			ShowAll ();
		}
		
		// TODO: This should handle sorting the items
		public void AddItem (ToolButton item)
		{
			if (tb1.NItems <= tb2.NItems)
				tb1.Insert (item, tb1.NItems);
			else
				tb2.Insert (item, tb2.NItems);
		}

		public void RemoveItem (ToolButton item)
		{
			//Run a remove on both tables since it might be in either
			tb1.Remove (item);
			tb2.Remove (item);
		}

		private void HandleToolAdded (object sender, ToolEventArgs e)
		{
			AddItem (e.Tool.ToolItem);
		}

		private void HandleToolRemoved (object sender, ToolEventArgs e)
		{
			RemoveItem (e.Tool.ToolItem);
		}
	}
}
