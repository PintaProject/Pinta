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
using System.ComponentModel;
using Cairo;
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
			
			var data = new RotateZoomData ();
			var dialog = new SimpleEffectDialog (Catalog.GetString ("Rotate / Zoom Layer"),
				PintaCore.Resources.GetIcon ("Menu.Layers.RotateZoom.png"), data,
			                                     new PintaLocalizer ());

            // When parameters are modified, update the display transform of the layer.
		    dialog.EffectDataChanged += (o, args) =>
		    {
		        var xform = ComputeMatrix (data);
		        var doc = PintaCore.Workspace.ActiveDocument;
		        doc.CurrentUserLayer.Transform.InitMatrix (xform);
		        PintaCore.Workspace.Invalidate ();
		    };

			int response = dialog.Run ();
		    ClearLivePreview ();
			if (response == (int)Gtk.ResponseType.Ok && !data.IsDefault)
				ApplyTransform (data);

			dialog.Destroy ();
		}

	    private static void ClearLivePreview ()
	    {
            PintaCore.Workspace.ActiveDocument.CurrentUserLayer.Transform.InitIdentity ();
            PintaCore.Workspace.Invalidate ();
	    }

	    private static Matrix ComputeMatrix (RotateZoomData data)
	    {
	        var xform = new Matrix ();
	        var image_size = PintaCore.Workspace.ImageSize;
            var center_x = image_size.Width / 2.0;
            var center_y = image_size.Height / 2.0;

            xform.Translate ((1 + data.Pan.X) * center_x, (1 + data.Pan.Y) * center_y);
            xform.Rotate ((-data.Angle / 180d) * Math.PI);
            xform.Scale (data.Zoom, data.Zoom);
	        xform.Translate (-center_x, -center_y);

	        return xform;
	    }

	    private void ApplyTransform (RotateZoomData data)
		{
			var doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			var old_surf = doc.CurrentUserLayer.Surface.Clone ();

	        var xform = ComputeMatrix (data);
			doc.CurrentUserLayer.ApplyTransform (xform, PintaCore.Workspace.ImageSize);
			doc.Workspace.Invalidate ();

	        doc.History.PushNewItem (new SimpleHistoryItem ("Menu.Layers.RotateZoom.png",
	            Catalog.GetString ("Rotate / Zoom Layer"), old_surf, doc.CurrentUserLayerIndex));
		}

		private class RotateZoomData : EffectData
		{
			[Caption ("Angle")]
			public double Angle = 0;

            [Caption ("Pan")]
		    public PointD Pan;

            [Caption ("Zoom"), MinimumValue (0), MaximumValue (16)]
            public double Zoom = 1.0;

			public override bool IsDefault {
                get
                {
                    return Angle == 0 && Pan.X == 0.0 && Pan.Y == 0.0 && Zoom == 1.0;
                }
			}
		}
	}
}

