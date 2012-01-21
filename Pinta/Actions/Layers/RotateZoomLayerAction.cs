// 
// RotateZoomLayerAction.cs
//  
// Author:
//       Cameron White <cameronwhite91@gmail.com>
// 
// Copyright (c) 2012 Cameron White
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
using Pinta.Gui.Widgets;
using Mono.Unix;

namespace Pinta.Actions
{
	public class RotateZoomLayerAction : IActionHandler
	{
		public void Initialize ()
		{
			PintaCore.Actions.Layers.RotateZoom.Activated += Activated;
		}

		public void Uninitialize ()
		{
			PintaCore.Actions.Layers.RotateZoom.Activated -= Activated;
		}

		private void Activated (object sender, EventArgs e)
		{
			// TODO - allow the layer to be zoomed in or out
			// TODO - show a live preview of the rotation
			
			var rotateZoomData = new RotateZoomData ();
			var dialog = new SimpleEffectDialog (Catalog.GetString ("Rotate / Zoom Layer"),
				PintaCore.Resources.GetIcon ("Menu.Layers.RotateZoom.png"), rotateZoomData);

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok && !rotateZoomData.IsDefault)
			{
				DoRotate (rotateZoomData);
			}

			dialog.Destroy ();
		}

		private void DoRotate (RotateZoomData rotateZoomData)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			var oldSurface = doc.CurrentLayer.Surface.Clone ();

			doc.CurrentLayer.Rotate (rotateZoomData.Angle);
			doc.Workspace.Invalidate ();

			var historyItem = new SimpleHistoryItem ("Menu.Layers.RotateZoom.png", Catalog.GetString ("Rotate / Zoom Layer"), oldSurface, doc.CurrentLayerIndex);

			doc.History.PushNewItem (historyItem);
		}

		private class RotateZoomData : EffectData
		{
			public double Angle = 0;

			public override bool IsDefault {
				get { return Angle == 0; }
			}
		}
	}
}

