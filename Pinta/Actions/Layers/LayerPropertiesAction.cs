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
			var doc = PintaCore.Workspace.ActiveDocument;

			using var dialog = new LayerPropertiesDialog ();

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok
				&& dialog.AreLayerPropertiesUpdated) {

				var historyMessage = GetLayerPropertyUpdateMessage (
						dialog.InitialLayerProperties,
						dialog.UpdatedLayerProperties);

				var historyItem = new UpdateLayerPropertiesHistoryItem (
					Resources.Icons.LayerProperties,
					historyMessage,
					doc.Layers.CurrentUserLayerIndex,
					dialog.InitialLayerProperties,
					dialog.UpdatedLayerProperties);

				doc.History.PushNewItem (historyItem);

				PintaCore.Workspace.ActiveWorkspace.Invalidate ();

			} else {

				var layer = doc.Layers.CurrentUserLayer;
				var selectionLayer = doc.Layers.SelectionLayer;
				var initial = dialog.InitialLayerProperties;
				initial.SetProperties (layer);
				if (selectionLayer != null)
					initial.SetProperties (selectionLayer);

				if ((layer.Opacity != initial.Opacity) || (layer.BlendMode != initial.BlendMode) || (layer.Hidden != initial.Hidden)) 
					PintaCore.Workspace.ActiveWorkspace.Invalidate ();
			}
		}

		private string GetLayerPropertyUpdateMessage (LayerProperties initial, LayerProperties updated)
		{

			string? ret = null;
			int count = 0;

			if (updated.Opacity != initial.Opacity) {
				ret = Translations.GetString ("Layer Opacity");
				count++;
			}

			if (updated.Name != initial.Name) {
				ret = Translations.GetString ("Rename Layer");
				count++;
			}

			if (updated.Hidden != initial.Hidden) {
				ret = (updated.Hidden) ? Translations.GetString ("Hide Layer") : Translations.GetString ("Show Layer");
				count++;
			}

			if (ret == null || count > 1)
				ret = Translations.GetString ("Layer Properties");

			return ret;
		}
	}
}
