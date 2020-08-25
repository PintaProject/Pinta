// 
// ColorPalettePad.cs
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
	public class ColorPalettePad : IDockPad
	{
		public void Initialize (Dock workspace, Application app, GLib.Menu padMenu)
		{
			ColorPaletteWidget palette = new ColorPaletteWidget () { Name = "palette" };

			DockItem palette_item = new DockItem(palette, "Palette")
			{
				Label = Translations.GetString("Palette")
			};

			// TODO-GTK3 (docking)
#if false
			palette_item.Icon = PintaCore.Resources.GetIcon ("Pinta.png");
			palette_item.DefaultLocation = "Toolbox/Bottom";
			palette_item.Behavior |= DockItemBehavior.CantClose;
			palette_item.DefaultWidth = 35;
#endif
			workspace.AddItem(palette_item, DockPlacement.Left);

			var show_palette = new ToggleCommand ("palette", Translations.GetString ("Palette"), null, "Pinta.png");
			app.AddAction(show_palette);
			padMenu.AppendItem(show_palette.CreateMenuItem());

			show_palette.Toggled += (val) => {
				palette_item.Visible = val;
			};

			// TODO-GTK3 (docking)
#if false
			palette_item.VisibleChanged += (o, args) => {
				show_palette.Value = palette_item.Visible;
			};
#endif

			palette.Initialize ();
			show_palette.Value = palette_item.Visible;
		}
    }
}
