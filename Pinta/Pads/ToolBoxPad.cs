// 
// ToolBoxPad.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2011 Jonathan Pobst
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Gtk;
using Pinta.Docking;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta
{
	public class ToolBoxPad : IDockPad
	{
		public void Initialize (Dock workspace, Application app, GLib.Menu padMenu)
		{
			ToolBoxWidget toolbox = new ToolBoxWidget () { Name = "toolbox" };

			DockItem toolbox_item = new DockItem(toolbox, "Toolbox")
			{
				Label = Translations.GetString("Tools")
			};

			// TODO-GTK3 (docking)
#if false
			toolbox_item.Content = toolbox;
			toolbox_item.Icon = Gtk.IconTheme.Default.LoadIcon(Resources.Icons.ToolPencil, 16);
			toolbox_item.Behavior |= DockItemBehavior.CantClose;
			toolbox_item.DefaultWidth = 35;
#endif
			workspace.AddItem(toolbox_item, DockPlacement.Left);

			var show_toolbox = new ToggleCommand("Tools", Translations.GetString("Tools"), null, Resources.Icons.ToolPencil);
			app.AddAction(show_toolbox);
			padMenu.AppendItem(show_toolbox.CreateMenuItem());

			show_toolbox.Toggled += (val) => { toolbox_item.Visible = val; };
			// TODO-GTK3 (docking)
#if false
			toolbox_item.VisibleChanged += (o, args) => { show_toolbox.Value = toolbox_item.Visible; };
#endif

			show_toolbox.Value = toolbox_item.Visible;
		}
	}
}
