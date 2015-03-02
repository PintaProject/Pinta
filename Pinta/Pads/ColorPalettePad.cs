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
using Mono.Unix;
using MonoDevelop.Components.Docking;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta
{
	public class ColorPalettePad : IDockPad
	{
		public void Initialize (DockFrame workspace, Menu padMenu)
		{
			DockItem palette_item = workspace.AddItem ("Palette");
			ColorPaletteWidget palette = new ColorPaletteWidget () { Name = "palette" };

			palette_item.Label = Catalog.GetString ("Palette");
			palette_item.Content = palette;
			palette_item.Icon = PintaCore.Resources.GetIcon ("Pinta.png");
			palette_item.DefaultLocation = "Toolbox/Bottom";
			palette_item.Behavior |= DockItemBehavior.CantClose;
			palette_item.DefaultWidth = 65;

			Gtk.ToggleAction show_palette = padMenu.AppendToggleAction ("Palette", Catalog.GetString ("Palette"), null, "Pinta.png");
			show_palette.Activated += delegate { palette_item.Visible = show_palette.Active; };
			palette_item.VisibleChanged += delegate { show_palette.Active = palette_item.Visible; };

			palette.Initialize ();
			show_palette.Active = palette_item.Visible;
		}
	}
}
