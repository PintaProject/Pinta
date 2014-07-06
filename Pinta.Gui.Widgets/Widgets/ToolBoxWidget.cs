using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem (true)]
	public class ToolBoxWidget : WrappingPaletteContainer
	{
		public ToolBoxWidget () : base(16)
		{
			PintaCore.Tools.ToolAdded += HandleToolAdded;
			PintaCore.Tools.ToolRemoved += HandleToolRemoved;

			ShowAll ();
		}
		
		// TODO: This should handle sorting the items
		public void AddItem (ToolButton item)
		{
            Append(item);
		}

		public void RemoveItem (ToolButton item)
		{
			//Run a remove on both tables since it might be in either
            Remove(item);
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
