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
		LayersListView layers = new ();
		DockItem layers_item = new (
			child: layers,
			uniqueName: "Layers",
			iconName: Resources.Icons.LayerDuplicate
		) {
			Label = Translations.GetString ("Layers"),
		};

		Gtk.Box layers_tb = layers_item.AddToolBar ();
		layers_tb.AppendMultiple ([
			layer_actions.AddNewLayer.CreateDockToolBarItem (),
			layer_actions.DeleteLayer.CreateDockToolBarItem (),
			layer_actions.DuplicateLayer.CreateDockToolBarItem (),
			layer_actions.MergeLayerDown.CreateDockToolBarItem (),
			layer_actions.MoveLayerUp.CreateDockToolBarItem (),
			layer_actions.MoveLayerDown.CreateDockToolBarItem (),
		]);

		workspace.AddItem (layers_item, DockPlacement.Right);
	}
}
