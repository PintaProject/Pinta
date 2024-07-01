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
	private readonly LayerActions layer_actions;
	private readonly WorkspaceManager workspace_manager;
	private readonly ToolManager tool_manager;

	internal RotateZoomLayerAction (
		LayerActions layerActions,
		WorkspaceManager workspaceManager,
		ToolManager toolManager)
	{
		layer_actions = layerActions;
		workspace_manager = workspaceManager;
		tool_manager = toolManager;
	}

	void IActionHandler.Initialize ()
	{
		layer_actions.RotateZoom.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		layer_actions.RotateZoom.Activated -= Activated;
	}

	private void Activated (object sender, EventArgs e)
	{
		// TODO - allow the layer to be zoomed in or out

		RotateZoomData data = new ();

		SimpleEffectDialog dialog = new (
			Translations.GetString ("Rotate / Zoom Layer"),
			Resources.Icons.LayerRotateZoom,
			data,
			new PintaLocalizer ());

		// When parameters are modified, update the display transform of the layer.
		dialog.EffectDataChanged += (o, args) => {
			var xform = ComputeMatrix (data);
			var doc = workspace_manager.ActiveDocument;
			doc.Layers.CurrentUserLayer.Transform = xform.Clone ();
			workspace_manager.Invalidate ();
		};

		dialog.OnResponse += (_, args) => {
			ClearLivePreview ();
			if (args.ResponseId == (int) Gtk.ResponseType.Ok && !data.IsDefault)
				ApplyTransform (data);

			dialog.Destroy ();
		};

		dialog.Present ();
	}

	private void ClearLivePreview ()
	{
		workspace_manager.ActiveDocument.Layers.CurrentUserLayer.Transform.InitIdentity ();
		workspace_manager.Invalidate ();
	}

	private Matrix ComputeMatrix (RotateZoomData data)
	{
		var xform = CairoExtensions.CreateIdentityMatrix ();
		var image_size = workspace_manager.ImageSize;
		var center_x = image_size.Width / 2.0;
		var center_y = image_size.Height / 2.0;

		xform.Translate ((1 + data.Pan.X) * center_x, (1 + data.Pan.Y) * center_y);
		xform.Rotate (-data.Angle.ToRadians ().Radians);
		xform.Scale (data.Zoom, data.Zoom);
		xform.Translate (-center_x, -center_y);

		return xform;
	}

	private void ApplyTransform (RotateZoomData data)
	{
		var doc = workspace_manager.ActiveDocument;

		tool_manager.Commit ();

		var old_surf = doc.Layers.CurrentUserLayer.Surface.Clone ();

		var xform = ComputeMatrix (data);

		doc.Layers.CurrentUserLayer.ApplyTransform (
			xform,
			workspace_manager.ImageSize,
			workspace_manager.ImageSize);

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
		public PointD Pan { get; set; } = new (0, 0);

		[Caption ("Zoom"), MinimumValue (0), MaximumValue (16)]
		public double Zoom { get; set; } = 1.0;

		public override bool IsDefault => Angle.Degrees == 0 && Pan.X == 0.0 && Pan.Y == 0.0 && Zoom == 1.0;
	}
}

