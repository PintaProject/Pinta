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
using Gtk;
using Mono.Unix;
using Pinta.Core;

namespace Pinta.Actions
{
	class LayerPropertiesAction : IActionHandler
	{
		#region IActionHandler Members
		public void Initialize ()
		{
			PintaCore.Actions.Layers.Properties.Activated += Activated;
		}

		public void Uninitialize ()
		{
			PintaCore.Actions.Layers.Properties.Activated -= Activated;
		}
		#endregion

		private void Activated (object sender, EventArgs e)
		{
			var dialog = new LayerPropertiesDialog ();

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok
			    && dialog.AreLayerPropertiesUpdated) {

				var historyMessage = GetLayerPropertyUpdateMessage (
						dialog.InitialLayerProperties,
						dialog.UpdatedLayerProperties);

				var historyItem = new UpdateLayerPropertiesHistoryItem (
					"Menu.Layers.LayerProperties.png",
					historyMessage,
					PintaCore.Layers.CurrentLayerIndex,
					dialog.InitialLayerProperties,
					dialog.UpdatedLayerProperties);

				PintaCore.Workspace.ActiveWorkspace.History.PushNewItem (historyItem);

				PintaCore.Workspace.ActiveWorkspace.Invalidate ();

			} else {

				var layer = PintaCore.Workspace.ActiveDocument.CurrentUserLayer;
				var initial = dialog.InitialLayerProperties;
				initial.SetProperties (layer);

				if (layer.Opacity != initial.Opacity)
					PintaCore.Workspace.ActiveWorkspace.Invalidate ();
			}

			dialog.Destroy ();
		}

		private string GetLayerPropertyUpdateMessage (LayerProperties initial, LayerProperties updated)
		{

			string ret = null;
			int count = 0;

			if (updated.Opacity != initial.Opacity) {
				ret = Catalog.GetString ("Layer Opacity");
				count++;
			}

			if (updated.Name != initial.Name) {
				ret = Catalog.GetString ("Rename Layer");
				count++;
			}

			if (updated.Hidden != initial.Hidden) {
				ret = (updated.Hidden) ? Catalog.GetString ("Hide Layer") : Catalog.GetString ("Show Layer");
				count++;
			}

			if (ret == null || count > 1)
				ret = Catalog.GetString ("Layer Properties");

			return ret;
		}
	}
}
