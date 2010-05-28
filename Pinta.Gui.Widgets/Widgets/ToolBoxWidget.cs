using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gtk;

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
			
			ShowAll ();
		}
		
		public void AddItem (ToolButton item)
		{
			if (tb1.NItems <= tb2.NItems)
				tb1.Insert (item, tb1.NItems);
			else
				tb2.Insert (item, tb2.NItems);
		}
	}
}
