//
// LayersPad.cs
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

using Pinta.Core;
using Pinta.Docking;
using Pinta.Gui.Widgets;

namespace Pinta;

internal sealed class LayersPad : IDockPad
{
	private readonly LayerActions layer_actions;
	internal LayersPad (LayerActions layerActions)
	{
		layer_actions = layerActions;
	}

	public void Initialize (Dock workspace)
	{
		LayersListView layers = LayersListView.New ();
		DockItem layers_item = new (
			child: layers,
			uniqueName: "Layers",
			iconName: Resources.Icons.LayerDuplicate
		) {
			Label = Translations.GetString ("Layers"),
		};

		Gio.Menu hamburger_menu = Gio.Menu.New ();

		Gio.Menu flip_section = Gio.Menu.New ();
		flip_section.AppendItem (layer_actions.FlipHorizontal.CreateMenuItem ());
		flip_section.AppendItem (layer_actions.FlipVertical.CreateMenuItem ());
		flip_section.AppendItem (layer_actions.RotateZoom.CreateMenuItem ());

		Gio.Menu prop_section = Gio.Menu.New ();
		prop_section.AppendItem (layer_actions.Properties.CreateMenuItem ());

		hamburger_menu.AppendItem (layer_actions.ImportFromFile.CreateMenuItem ());
		hamburger_menu.AppendSection (null, flip_section);
		hamburger_menu.AppendSection (null, prop_section);

		Gtk.MenuButton hamburger_button = Gtk.MenuButton.New ();
		hamburger_button.MenuModel = hamburger_menu;
		hamburger_button.IconName = Resources.StandardIcons.OpenMenu;

		hamburger_button.Direction = Gtk.ArrowType.Up;

		Gtk.Box layers_tb = layers_item.AddToolBar ();
		layers_tb.AppendMultiple ([
			layer_actions.AddNewLayer.CreateDockToolBarItem (),
			layer_actions.DeleteLayer.CreateDockToolBarItem (),
			layer_actions.DuplicateLayer.CreateDockToolBarItem (),
			layer_actions.MergeLayerDown.CreateDockToolBarItem (),
			layer_actions.MoveLayerUp.CreateDockToolBarItem (),
			layer_actions.MoveLayerDown.CreateDockToolBarItem (),
			hamburger_button
		]);

		workspace.AddItem (layers_item, DockPlacement.Right);
	}
}
