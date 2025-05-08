// 
// ImageActions.cs
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
using Cairo;

namespace Pinta.Core;

public sealed class ImageActions
{
	public Command CropToSelection { get; }
	public Command AutoCrop { get; }
	public Command Resize { get; }
	public Command CanvasSize { get; }
	public Command FlipHorizontal { get; }
	public Command FlipVertical { get; }
	public Command RotateCW { get; }
	public Command RotateCCW { get; }
	public Command Rotate180 { get; }
	public Command Flatten { get; }

	private readonly ToolManager tools;
	private readonly WorkspaceManager workspace;
	private readonly ViewActions view;

	public ImageActions (
		ToolManager tools,
		WorkspaceManager workspace,
		ViewActions view)
	{
		CropToSelection = new Command (
			"croptoselection",
			Translations.GetString ("Crop to Selection"),
			null,
			Resources.Icons.ImageCrop,
			shortcuts: ["<Primary><Shift>X"]);

		AutoCrop = new Command (
			"autocrop",
			Translations.GetString ("Auto Crop"),
			null,
			Resources.Icons.ImageCrop,
			shortcuts: ["<Ctrl><Alt>X"]);

		Resize = new Command (
			"resize",
			Translations.GetString ("Resize Image..."),
			null,
			Resources.Icons.ImageResize,
			shortcuts: ["<Primary>R"]);

		CanvasSize = new Command (
			"canvassize",
			Translations.GetString ("Resize Canvas..."),
			null,
			Resources.Icons.ImageResizeCanvas,
			shortcuts: ["<Primary><Shift>R"]);

		FlipHorizontal = new Command (
			"fliphorizontal",
			Translations.GetString ("Flip Horizontal"),
			null,
			Resources.Icons.ImageFlipHorizontal);

		FlipVertical = new Command (
			"flipvertical",
			Translations.GetString ("Flip Vertical"),
			null,
			Resources.Icons.ImageFlipVertical);

		RotateCW = new Command (
			"rotatecw",
			Translations.GetString ("Rotate 90° Clockwise"),
			null,
			Resources.Icons.ImageRotate90CW,
			shortcuts: ["<Primary>H"]);

		RotateCCW = new Command (
			"rotateccw",
			Translations.GetString ("Rotate 90° Counter-Clockwise"),
			null,
			Resources.Icons.ImageRotate90CCW,
			shortcuts: ["<Primary>G"]);

		Rotate180 = new Command (
			"rotate180",
			Translations.GetString ("Rotate 180°"),
			null,
			Resources.Icons.ImageRotate180,
			shortcuts: ["<Primary>J"]);

		Flatten = new Command (
			"flatten",
			Translations.GetString ("Flatten"),
			null,
			Resources.Icons.ImageFlatten,
			shortcuts: ["<Primary><Shift>F"]);

		this.tools = tools;
		this.workspace = workspace;
		this.view = view;
	}

	public void RegisterActions (Gtk.Application app, Gio.Menu menu)
	{
		Gio.Menu flip_section = Gio.Menu.New ();
		flip_section.AppendItem (FlipHorizontal.CreateMenuItem ());
		flip_section.AppendItem (FlipVertical.CreateMenuItem ());

		Gio.Menu rotate_section = Gio.Menu.New ();
		rotate_section.AppendItem (RotateCW.CreateMenuItem ());
		rotate_section.AppendItem (RotateCCW.CreateMenuItem ());
		rotate_section.AppendItem (Rotate180.CreateMenuItem ());

		Gio.Menu flatten_section = Gio.Menu.New ();
		flatten_section.AppendItem (Flatten.CreateMenuItem ());

		menu.AppendItem (CropToSelection.CreateMenuItem ());
		menu.AppendItem (AutoCrop.CreateMenuItem ());
		menu.AppendItem (Resize.CreateMenuItem ());
		menu.AppendItem (CanvasSize.CreateMenuItem ());
		menu.AppendSection (null, flip_section);
		menu.AppendSection (null, rotate_section);
		menu.AppendSection (null, flatten_section);

		app.SetActionsAndShortcuts ([
			CropToSelection,
			AutoCrop,
			Resize,
			CanvasSize,

			FlipHorizontal,
			FlipVertical,

			RotateCW,
			RotateCCW,
			Rotate180,

			Flatten]);
	}

	public void RegisterHandlers ()
	{
		FlipHorizontal.Activated += HandlePintaCoreActionsImageFlipHorizontalActivated;
		FlipVertical.Activated += HandlePintaCoreActionsImageFlipVerticalActivated;
		Rotate180.Activated += HandlePintaCoreActionsImageRotate180Activated;
		Flatten.Activated += HandlePintaCoreActionsImageFlattenActivated;
		RotateCW.Activated += HandlePintaCoreActionsImageRotateCWActivated;
		RotateCCW.Activated += HandlePintaCoreActionsImageRotateCCWActivated;
		CropToSelection.Activated += HandlePintaCoreActionsImageCropToSelectionActivated;
		AutoCrop.Activated += HandlePintaCoreActionsImageAutoCropActivated;

		workspace.SelectionChanged += (_, _) => {

			bool visible = false;

			if (workspace.HasOpenDocuments)
				visible = workspace.ActiveDocument.Selection.Visible;

			CropToSelection.Sensitive = visible;
		};
	}

	private void HandlePintaCoreActionsImageRotateCCWActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		doc.RotateImageCCW ();
		doc.History.PushNewItem (new InvertHistoryItem (InvertType.Rotate90CCW));
	}

	private void HandlePintaCoreActionsImageRotateCWActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		doc.RotateImageCW ();
		doc.History.PushNewItem (new InvertHistoryItem (InvertType.Rotate90CW));
	}

	private void HandlePintaCoreActionsImageFlattenActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		ImageSurface oldBottomSurface =
			doc.Layers
			.UserLayers[0]
			.Surface
			.Clone ();

		CompoundHistoryItem hist = new (
			Resources.Icons.ImageFlatten,
			Translations.GetString ("Flatten"));

		for (int i = doc.Layers.UserLayers.Count - 1; i >= 1; i--)
			hist.Push (new DeleteLayerHistoryItem (string.Empty, string.Empty, doc.Layers.UserLayers[i], i));

		doc.Layers.FlattenLayers ();

		hist.Push (new SimpleHistoryItem (string.Empty, string.Empty, oldBottomSurface, 0));

		doc.History.PushNewItem (hist);
	}

	private void HandlePintaCoreActionsImageRotate180Activated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		doc.RotateImage180 ();
		doc.History.PushNewItem (new InvertHistoryItem (InvertType.Rotate180));
	}

	private void HandlePintaCoreActionsImageFlipVerticalActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		doc.FlipImageVertical ();
		doc.History.PushNewItem (new InvertHistoryItem (InvertType.FlipVertical));
	}

	private void HandlePintaCoreActionsImageFlipHorizontalActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		doc.FlipImageHorizontal ();
		doc.History.PushNewItem (new InvertHistoryItem (InvertType.FlipHorizontal));
	}

	private void HandlePintaCoreActionsImageCropToSelectionActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		RectangleI rect = doc.GetSelectedBounds (true);

		CropImageToRectangle (doc, rect, doc.Selection.SelectionPath);
	}

	private void HandlePintaCoreActionsImageAutoCropActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		ImageSurface image = doc.GetFlattenedImage ();

		RectangleI rect = Utility.GetObjectBounds (image);

		// Ignore the current selection when auto-cropping.
		CropImageToRectangle (doc, rect, /*selection*/ null);
	}

	void CropImageToRectangle (Document doc, RectangleI rect, Path? selection)
	{
		if (rect.Width <= 0 || rect.Height <= 0)
			return;

		ResizeHistoryItem hist = new (workspace, doc.ImageSize) {
			Icon = Resources.Icons.ImageCrop,
			Text = Translations.GetString ("Crop to Selection"),
		};
		hist.StartSnapshotOfImage ();
		hist.RestoreSelection = doc.Selection.Clone ();

		double original_scale = doc.Workspace.Scale;
		doc.ImageSize = rect.Size;
		doc.Workspace.ViewSize = rect.Size;
		doc.Workspace.Scale = original_scale;

		view.UpdateCanvasScale ();

		foreach (var layer in doc.Layers.UserLayers)
			layer.Crop (rect, selection);

		hist.FinishSnapshotOfImage ();

		doc.History.PushNewItem (hist);
		doc.ResetSelectionPaths ();

		doc.Workspace.Invalidate ();
	}
}
