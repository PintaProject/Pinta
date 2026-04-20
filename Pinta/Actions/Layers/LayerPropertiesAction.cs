// 
// LayerPropertiesAction.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
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

using System;
using Pinta.Core;

namespace Pinta.Actions;

internal sealed class LayerPropertiesAction : IActionHandler
{
	private readonly ChromeManager chrome;
	private readonly LayerActions layers;
	private readonly WorkspaceManager workspace;
	internal LayerPropertiesAction (
		ChromeManager chrome,
		LayerActions layers,
		WorkspaceManager workspace)
	{
		this.chrome = chrome;
		this.layers = layers;
		this.workspace = workspace;
	}

	void IActionHandler.Initialize ()
	{
		layers.Properties.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		layers.Properties.Activated -= Activated;
	}

	private async void Activated (object sender, EventArgs e)
	{
		Document active = workspace.ActiveDocument;
		using LayerPropertiesDialog dialog = new (chrome, workspace);
		try {
			Gtk.ResponseType response = await dialog.RunAsync ();

			if (response == Gtk.ResponseType.Ok && dialog.AreLayerPropertiesUpdated) {
				UpdateLayerPropertiesHistoryItem historyItem = GetLayerUpdateHistoryItem (
					active.Layers.CurrentUserLayerIndex,
					dialog.InitialLayerProperties,
					dialog.UpdatedLayerProperties);

				active.History.PushNewItem (historyItem);

				workspace.ActiveWorkspace.Invalidate ();
			} else {
				Layer layer = active.Layers.CurrentUserLayer;
				Layer selectionLayer = active.Layers.SelectionLayer;
				LayerProperties initial = dialog.InitialLayerProperties;
				initial.SetProperties (layer);

				if (selectionLayer != null)
					initial.SetProperties (selectionLayer);

				if ((layer.Opacity != initial.Opacity) || (layer.BlendMode != initial.BlendMode) || (layer.Hidden != initial.Hidden))
					workspace.ActiveWorkspace.Invalidate ();
			}
		} finally {
			dialog.Destroy ();
		}
	}

	private static UpdateLayerPropertiesHistoryItem GetLayerUpdateHistoryItem (
		int layer,
		LayerProperties initial,
		LayerProperties updated)
	{

		string? message = null;
		string icon = Resources.Icons.LayerProperties;
		int count = 0;

		if (updated.Opacity != initial.Opacity) {
			message = Translations.GetString ("Layer Opacity");
			icon = Resources.Icons.LayerProperties;
			count++;
		}

		if (updated.Name != initial.Name) {
			message = Translations.GetString ("Rename Layer");
			icon = Resources.Icons.LayerProperties;
			count++;
		}

		if (updated.Hidden != initial.Hidden) {
			message = initial.Hidden ? Translations.GetString ("Show Layer") : Translations.GetString ("Hide Layer");
			icon = initial.Hidden ? Resources.StandardIcons.ViewReveal : Resources.StandardIcons.ViewConceal;
			count++;
		}

		if (message == null || count > 1) {
			message = Translations.GetString ("Layer Properties");
			icon = Resources.Icons.LayerProperties;
		}

		return new UpdateLayerPropertiesHistoryItem (icon, message, layer, initial, updated);
	}
}
