//
// LayerActions.cs
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

namespace Pinta.Core;

public sealed class LayerActions
{
	public Command AddNewLayer { get; }
	public Command DeleteLayer { get; }
	public Command DuplicateLayer { get; }
	public Command MergeLayerDown { get; }
	public Command ImportFromFile { get; }
	public Command FlipHorizontal { get; }
	public Command FlipVertical { get; }
	public Command RotateZoom { get; }
	public Command MoveLayerUp { get; }
	public Command MoveLayerDown { get; }
	public Command Properties { get; }

	private readonly ChromeManager chrome;
	private readonly ImageConverterManager image_formats;
	private readonly RecentFileManager recent_files;
	private readonly ToolManager tools;
	private readonly WorkspaceManager workspace;
	private readonly ImageActions image;
	public LayerActions (
		ChromeManager chrome,
		ImageConverterManager imageFormats,
		RecentFileManager recentFiles,
		ToolManager tools,
		WorkspaceManager workspace,
		ImageActions image)
	{
		AddNewLayer = new Command (
			"addnewlayer",
			Translations.GetString ("Add New Layer"),
			null,
			Resources.Icons.LayerNew,
			shortcuts: ["<Primary><Shift>N"]);

		DeleteLayer = new Command (
			"deletelayer",
			Translations.GetString ("Delete Layer"),
			null,
			Resources.Icons.LayerDelete,
			shortcuts: ["<Primary><Shift>Delete"]);

		DuplicateLayer = new Command (
			"duplicatelayer",
			Translations.GetString ("Duplicate Layer"),
			null,
			Resources.Icons.LayerDuplicate,
			shortcuts: ["<Primary><Shift>D"]);

		MergeLayerDown = new Command (
			"mergelayerdown",
			Translations.GetString ("Merge Layer Down"),
			null,
			Resources.Icons.LayerMergeDown,
			shortcuts: ["<Primary>M"]);

		ImportFromFile = new Command (
			"importfromfile",
			Translations.GetString ("Import from File..."),
			null,
			Resources.Icons.LayerImport);

		FlipHorizontal = new Command (
			"fliplayerhorizontal",
			Translations.GetString ("Flip Horizontal"),
			null,
			Resources.Icons.LayerFlipHorizontal);

		FlipVertical = new Command (
			"fliplayervertical",
			Translations.GetString ("Flip Vertical"),
			null,
			Resources.Icons.LayerFlipVertical);

		RotateZoom = new Command (
			"RotateZoom",
			Translations.GetString ("Rotate / Zoom Layer..."),
			null,
			Resources.Icons.LayerRotateZoom);

		MoveLayerUp = new Command (
			"movelayerup",
			Translations.GetString ("Move Layer Up"),
			null,
			Resources.Icons.LayerMoveUp);

		MoveLayerDown = new Command (
			"movelayerdown",
			Translations.GetString ("Move Layer Down"),
			null,
			Resources.Icons.LayerMoveDown);

		Properties = new Command (
			"properties",
			Translations.GetString ("Layer Properties..."),
			null,
			Resources.Icons.LayerProperties,
			shortcuts: ["F4"]);

		this.chrome = chrome;
		image_formats = imageFormats;
		recent_files = recentFiles;
		this.tools = tools;
		this.workspace = workspace;
		this.image = image;
	}

	public void RegisterActions (Gtk.Application app, Gio.Menu menu)
	{
		Gio.Menu flip_section = Gio.Menu.New ();
		flip_section.AppendItem (FlipHorizontal.CreateMenuItem ());
		flip_section.AppendItem (FlipVertical.CreateMenuItem ());
		flip_section.AppendItem (RotateZoom.CreateMenuItem ());

		Gio.Menu prop_section = Gio.Menu.New ();
		prop_section.AppendItem (Properties.CreateMenuItem ());

		menu.AppendItem (ImportFromFile.CreateMenuItem ());
		menu.AppendSection (null, flip_section);
		menu.AppendSection (null, prop_section);

		app.AddCommands ([
			AddNewLayer,
			DeleteLayer,
			DuplicateLayer,
			MergeLayerDown,
			ImportFromFile,

			FlipHorizontal,
			FlipVertical,
			RotateZoom,

			Properties,

			MoveLayerDown,
			MoveLayerUp]);
	}

	public void RegisterHandlers ()
	{
		AddNewLayer.Activated += HandlePintaCoreActionsLayersAddNewLayerActivated;
		DeleteLayer.Activated += HandlePintaCoreActionsLayersDeleteLayerActivated;
		DuplicateLayer.Activated += HandlePintaCoreActionsLayersDuplicateLayerActivated;
		MergeLayerDown.Activated += HandlePintaCoreActionsLayersMergeLayerDownActivated;
		MoveLayerDown.Activated += HandlePintaCoreActionsLayersMoveLayerDownActivated;
		MoveLayerUp.Activated += HandlePintaCoreActionsLayersMoveLayerUpActivated;
		FlipHorizontal.Activated += HandlePintaCoreActionsLayersFlipHorizontalActivated;
		FlipVertical.Activated += HandlePintaCoreActionsLayersFlipVerticalActivated;
		ImportFromFile.Activated += HandlePintaCoreActionsLayersImportFromFileActivated;

		workspace.LayerAdded += EnableOrDisableLayerActions;
		workspace.LayerRemoved += EnableOrDisableLayerActions;
		workspace.SelectedLayerChanged += EnableOrDisableLayerActions;
		workspace.ActiveDocumentChanged += EnableOrDisableLayerActions;

		EnableOrDisableLayerActions (null, EventArgs.Empty);
	}

	private void EnableOrDisableLayerActions (object? sender, EventArgs e)
	{
		Document? activeDoc = workspace.ActiveDocumentOrDefault;

		bool hasMultipleLayers = activeDoc?.Layers.UserLayers.Count > 1;
		DeleteLayer.Sensitive = hasMultipleLayers;
		image.Flatten.Sensitive = hasMultipleLayers;

		bool canMergeDown = activeDoc?.Layers.CurrentUserLayerIndex > 0;
		MergeLayerDown.Sensitive = canMergeDown;
		MoveLayerDown.Sensitive = canMergeDown;

		MoveLayerUp.Sensitive = activeDoc != null
			&& activeDoc.Layers.CurrentUserLayerIndex < activeDoc.Layers.UserLayers.Count - 1;
	}

	private Gtk.FileFilter CreateImagesFileFilter ()
	{
		Gtk.FileFilter imagesFilter = Gtk.FileFilter.New ();
		foreach (var format in image_formats.Formats) {
			if (!format.IsImportAvailable ()) continue;
			foreach (string ext in format.Extensions)
				imagesFilter.AddPattern ($"*.{ext}");
		}

		// On Unix-like systems, file extensions are often considered optional.
		// Files can often also be identified by their MIME types.
		// Windows does not understand MIME types natively.
		// Adding a MIME filter on Windows would break the native file picker and force a GTK file picker instead.
		if (SystemManager.GetOperatingSystem () != OS.Windows)
			foreach (var format in image_formats.Formats)
				foreach (var mime in format.Mimes)
					imagesFilter.AddMimeType (mime);

		imagesFilter.Name = Translations.GetString ("Image files");

		return imagesFilter;
	}

	private async void HandlePintaCoreActionsLayersImportFromFileActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		// Add image files filter
		using Gtk.FileFilter imagesFilter = CreateImagesFileFilter ();

		using Gio.ListStore fileFilters = Gio.ListStore.New (Gtk.FileFilter.GetGType ());
		fileFilters.Append (imagesFilter);

		using Gtk.FileDialog fileDialog = Gtk.FileDialog.New ();
		fileDialog.SetTitle (Translations.GetString ("Open Image File"));
		fileDialog.SetFilters (fileFilters);
		if (recent_files.GetDialogDirectory () is Gio.File dir && dir.QueryExists (null))
			fileDialog.SetInitialFolder (dir);

		Gio.File? choice = await fileDialog.OpenFileAsync (chrome.MainWindow);

		if (choice is null) return;

		Gio.File? directory = choice.GetParent ();

		if (directory is not null)
			recent_files.LastDialogDirectory = directory;

		// Open the image and add it to the layers
		UserLayer layer = doc.Layers.AddNewLayer (choice.GetDisplayName ());

		using (Gio.FileInputStream fs = choice.Read (null)) {
			try {
				using GdkPixbuf.Pixbuf bg = GdkPixbuf.Pixbuf.NewFromStream (fs, cancellable: null)!; // NRT: only nullable when an error is thrown
				using Cairo.Context context = new (layer.Surface);
				context.DrawPixbuf (bg, PointD.Zero);
			} finally {
				fs.Close (null);
			}
		}

		AddLayerHistoryItem hist = new (
			Resources.Icons.LayerImport,
			Translations.GetString ("Import From File"),
			doc.Layers.IndexOf (layer));

		// --- Changes to document go after everything else is completed successfully

		doc.Layers.SetCurrentUserLayer (layer);
		doc.History.PushNewItem (hist);
		doc.Workspace.Invalidate ();
	}

	private void HandlePintaCoreActionsLayersFlipVerticalActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		doc.Layers.CurrentUserLayer.FlipVertical ();
		doc.Workspace.Invalidate ();
		doc.History.PushNewItem (new InvertHistoryItem (InvertType.FlipLayerVertical, doc.Layers.CurrentUserLayerIndex));
	}

	private void HandlePintaCoreActionsLayersFlipHorizontalActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		doc.Layers.CurrentUserLayer.FlipHorizontal ();
		doc.Workspace.Invalidate ();
		doc.History.PushNewItem (new InvertHistoryItem (InvertType.FlipLayerHorizontal, doc.Layers.CurrentUserLayerIndex));
	}

	private void HandlePintaCoreActionsLayersMoveLayerUpActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		SwapLayersHistoryItem hist = new (
			Resources.Icons.LayerMoveUp,
			Translations.GetString ("Move Layer Up"),
			doc.Layers.CurrentUserLayerIndex,
			doc.Layers.CurrentUserLayerIndex + 1);

		doc.Layers.MoveCurrentLayerUp ();
		doc.History.PushNewItem (hist);
	}

	private void HandlePintaCoreActionsLayersMoveLayerDownActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		SwapLayersHistoryItem hist = new (
			Resources.Icons.LayerMoveDown,
			Translations.GetString ("Move Layer Down"),
			doc.Layers.CurrentUserLayerIndex,
			doc.Layers.CurrentUserLayerIndex - 1);

		doc.Layers.MoveCurrentLayerDown ();
		doc.History.PushNewItem (hist);
	}

	private void HandlePintaCoreActionsLayersMergeLayerDownActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		int bottomLayerIndex = doc.Layers.CurrentUserLayerIndex - 1;
		Cairo.ImageSurface oldBottomSurface = doc.Layers.UserLayers[bottomLayerIndex].Surface.Clone ();

		CompoundHistoryItem hist = new (
			Resources.Icons.LayerMergeDown,
			Translations.GetString ("Merge Layer Down"));

		DeleteLayerHistoryItem h1 = new (
			string.Empty,
			string.Empty,
			doc.Layers.CurrentUserLayer,
			doc.Layers.CurrentUserLayerIndex);

		doc.Layers.MergeCurrentLayerDown ();

		SimpleHistoryItem h2 = new (
			string.Empty,
			string.Empty,
			oldBottomSurface,
			bottomLayerIndex);
		hist.Push (h1);
		hist.Push (h2);

		doc.History.PushNewItem (hist);
	}

	private void HandlePintaCoreActionsLayersDuplicateLayerActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		UserLayer l = doc.Layers.DuplicateCurrentLayer ();

		// Make new layer the current layer
		doc.Layers.SetCurrentUserLayer (l);

		AddLayerHistoryItem hist = new (
			Resources.Icons.LayerDuplicate,
			Translations.GetString ("Duplicate Layer"),
			doc.Layers.IndexOf (l));
		doc.History.PushNewItem (hist);
	}

	private void HandlePintaCoreActionsLayersDeleteLayerActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		DeleteLayerHistoryItem hist = new (
			Resources.Icons.LayerDelete,
			Translations.GetString ("Delete Layer"),
			doc.Layers.CurrentUserLayer,
			doc.Layers.CurrentUserLayerIndex);

		doc.Layers.DeleteLayer (doc.Layers.CurrentUserLayerIndex);

		doc.History.PushNewItem (hist);
	}

	private void HandlePintaCoreActionsLayersAddNewLayerActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;
		tools.Commit ();

		UserLayer l = doc.Layers.AddNewLayer (string.Empty);

		// Make new layer the current layer
		doc.Layers.SetCurrentUserLayer (l);

		AddLayerHistoryItem hist = new (
			Resources.Icons.LayerNew,
			Translations.GetString ("Add New Layer"),
			doc.Layers.IndexOf (l));
		doc.History.PushNewItem (hist);
	}
}
