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
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Actions;

public sealed class RotateZoomLayerAction : IActionHandler
{
	private readonly ChromeManager chrome;
	private readonly LayerActions layers;
	private readonly WorkspaceManager workspace;
	private readonly ToolManager tools;
	internal RotateZoomLayerAction (
		ChromeManager chrome,
		LayerActions layers,
		WorkspaceManager workspace,
		ToolManager tools)
	{
		this.chrome = chrome;
		this.layers = layers;
		this.workspace = workspace;
		this.tools = tools;
	}

	void IActionHandler.Initialize ()
	{
		layers.RotateZoom.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		layers.RotateZoom.Activated -= Activated;
	}

	private async void Activated (object sender, EventArgs e)
	{
		// TODO - allow the layer to be zoomed in or out

		RotateZoomData data = new ();

		using SimpleEffectDialog dialog = new (
			chrome.MainWindow,
			Translations.GetString ("Rotate / Zoom Layer"),
			Resources.Icons.LayerRotateZoom,
			data,
			new PintaLocalizer (),
			workspace);

		// When parameters are modified, update the display transform of the layer.
		dialog.EffectDataChanged += (o, args) => {
			var xform = ComputeMatrix (data);
			var doc = workspace.ActiveDocument;
			doc.Layers.CurrentUserLayer.Transform = xform.Clone ();
			workspace.Invalidate ();
		};

		Gtk.ResponseType response = await dialog.RunAsync ();

		dialog.Destroy ();

		ClearLivePreview ();

		if (response != Gtk.ResponseType.Ok || data.IsDefault) return;

		ApplyTransform (data);
	}

	private void ClearLivePreview ()
	{
		workspace.ActiveDocument.Layers.CurrentUserLayer.Transform.InitIdentity ();
		workspace.Invalidate ();
	}

	private Matrix ComputeMatrix (RotateZoomData data)
	{
		var xform = CairoExtensions.CreateIdentityMatrix ();
		var image_size = workspace.ImageSize;
		var center_x = image_size.Width / 2.0;
		var center_y = image_size.Height / 2.0;

		xform.Translate ((1 + data.Pan.Horizontal) * center_x, (1 + data.Pan.Vertical) * center_y);
		xform.Rotate (-data.Angle.ToRadians ().Radians);
		xform.Scale (data.Zoom, data.Zoom);
		xform.Translate (-center_x, -center_y);

		return xform;
	}

	private void ApplyTransform (RotateZoomData data)
	{
		var doc = workspace.ActiveDocument;

		tools.Commit ();

		var old_surf = doc.Layers.CurrentUserLayer.Surface.Clone ();

		var xform = ComputeMatrix (data);

		doc.Layers.CurrentUserLayer.ApplyTransform (
			xform,
			workspace.ImageSize,
			workspace.ImageSize);

		doc.Workspace.Invalidate ();

		doc.History.PushNewItem (
			new SimpleHistoryItem (
				Resources.Icons.LayerRotateZoom,
				Translations.GetString ("Rotate / Zoom Layer"),
				old_surf,
				doc.Layers.CurrentUserLayerIndex
			)
		);
	}

	private sealed class RotateZoomData : EffectData
	{
		[Caption ("Angle")]
		public DegreesAngle Angle { get; set; } = new (0);

		[Caption ("Pan")]
		public CenterOffset<double> Pan { get; set; } = new (0, 0);

		[Caption ("Zoom"), MinimumValue (0), MaximumValue (16)]
		public double Zoom { get; set; } = 1.0;

		public override bool IsDefault => Angle.Degrees == 0 && Pan.Horizontal == 0.0 && Pan.Vertical == 0.0 && Zoom == 1.0;
	}
}

