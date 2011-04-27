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

using Gtk;
using Mono.Unix;
using MonoDevelop.Components.Docking;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta
{
	public class LayersPad : IDockPad
	{
		public void Initialize (DockFrame workspace, Menu padMenu)
		{
			var layers = new LayersListWidget ();
			DockItem layers_item = workspace.AddItem ("Layers");
			DockItemToolbar layers_tb = layers_item.GetToolbar (PositionType.Bottom);

			layers_item.Label = Catalog.GetString ("Layers");
			layers_item.Content = layers;
			layers_item.Icon = PintaCore.Resources.GetIcon ("Menu.Layers.MergeLayerDown.png");

			layers_tb.Add (PintaCore.Actions.Layers.AddNewLayer.CreateDockToolBarItem ());
			layers_tb.Add (PintaCore.Actions.Layers.DeleteLayer.CreateDockToolBarItem ());
			layers_tb.Add (PintaCore.Actions.Layers.DuplicateLayer.CreateDockToolBarItem ());
			layers_tb.Add (PintaCore.Actions.Layers.MergeLayerDown.CreateDockToolBarItem ());
			layers_tb.Add (PintaCore.Actions.Layers.MoveLayerUp.CreateDockToolBarItem ());
			layers_tb.Add (PintaCore.Actions.Layers.MoveLayerDown.CreateDockToolBarItem ());

			Gtk.ToggleAction show_layers = padMenu.AppendToggleAction ("Layers", Catalog.GetString ("Layers"), null, "Menu.Layers.MergeLayerDown.png");
			show_layers.Activated += delegate { layers_item.Visible = show_layers.Active; };
			layers_item.VisibleChanged += delegate { show_layers.Active = layers_item.Visible; };

			show_layers.Active = layers_item.Visible;

			PintaCore.Workspace.ActiveDocumentChanged += delegate { layers.Reset (); };
		}
	}
}
