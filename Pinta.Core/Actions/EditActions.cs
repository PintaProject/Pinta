// 
// EditActions.cs
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
using System.Linq;
using Cairo;

namespace Pinta.Core;

public sealed class EditActions
{
	public Command Undo { get; }
	public Command Redo { get; }
	public Command Cut { get; }
	public Command Copy { get; }
	public Command CopyMerged { get; }
	public Command Paste { get; }
	public Command PasteIntoNewLayer { get; }
	public Command PasteIntoNewImage { get; }
	public Command EraseSelection { get; }
	public Command FillSelection { get; }
	public Command InvertSelection { get; }
	public Command OffsetSelection { get; }
	public Command SelectAll { get; }
	public Command Deselect { get; }
	public Command LoadPalette { get; }
	public Command SavePalette { get; }
	public Command ResetPalette { get; }
	public Command ResizePalette { get; }

	private Gio.File? last_palette_dir = null;

	private readonly ChromeManager chrome;
	private readonly PaletteFormatManager palette_formats;
	private readonly PaletteManager palette;
	private readonly ToolManager tools;
	private readonly WorkspaceManager workspace;
	public EditActions (
		ChromeManager chrome,
		PaletteFormatManager paletteFormats,
		PaletteManager palette,
		ToolManager tools,
		WorkspaceManager workspace)
	{
		Undo = new Command (
			"undo",
			Translations.GetString ("Undo"),
			null,
			Resources.StandardIcons.EditUndo,
			shortcuts: ["<Primary>Z"]);

		Redo = new Command (
			"redo",
			Translations.GetString ("Redo"),
			null,
			Resources.StandardIcons.EditRedo,
			shortcuts: ["<Primary><Shift>Z", "<Ctrl>Y"]);

		Cut = new Command (
			"cut",
			Translations.GetString ("Cut"),
			null,
			Resources.StandardIcons.EditCut,
			shortcuts: ["<Primary>X"]);

		Copy = new Command (
			"copy",
			Translations.GetString ("Copy"),
			null,
			Resources.StandardIcons.EditCopy,
			shortcuts: ["<Primary>C"]);

		CopyMerged = new Command (
			"copymerged",
			Translations.GetString ("Copy Merged"),
			null,
			Resources.StandardIcons.EditCopy,
			shortcuts: ["<Primary><Shift>C"]);

		Paste = new Command (
			"paste",
			Translations.GetString ("Paste"),
			null,
			Resources.StandardIcons.EditPaste,
			shortcuts: ["<Primary>V"]);

		PasteIntoNewLayer = new Command (
			"pasteintonewlayer",
			Translations.GetString ("Paste Into New Layer"),
			null,
			Resources.StandardIcons.EditPaste,
			shortcuts: ["<Primary><Shift>V"]);

		// Note: <Ctrl><Alt>V shortcut doesn't seem to work on Windows & macOS (bug 2047921).
		PasteIntoNewImage = new Command (
			"pasteintonewimage",
			Translations.GetString ("Paste Into New Image"),
			null,
			Resources.StandardIcons.EditPaste,
			shortcuts: ["<Shift>V", "<Primary><Alt>V"]);

		EraseSelection = new Command (
			"eraseselection",
			Translations.GetString ("Erase Selection"),
			null,
			Resources.Icons.EditSelectionErase,
			shortcuts: ["Delete"]);

		FillSelection = new Command (
			"fillselection",
			Translations.GetString ("Fill Selection"),
			null,
			Resources.Icons.EditSelectionFill,
			shortcuts: ["BackSpace"]);

		InvertSelection = new Command (
			"invertselection",
			Translations.GetString ("Invert Selection"),
			null,
			Resources.Icons.EditSelectionFill,
			shortcuts: ["<Primary>I"]);

		OffsetSelection = new Command (
			"offsetselection",
			Translations.GetString ("Offset Selection"),
			null,
			Resources.Icons.EditSelectionOffset,
			shortcuts: ["<Primary><Shift>O"]);

		SelectAll = new Command (
			"selectall",
			Translations.GetString ("Select All"),
			null,
			Resources.StandardIcons.EditSelectAll,
			shortcuts: ["<Primary>A"]);

		Deselect = new Command (
			"deselect",
			Translations.GetString ("Deselect All"),
			null,
			Resources.Icons.EditSelectionNone,
			shortcuts: ["<Primary><Shift>A", "<Ctrl>D"]);

		LoadPalette = new Command (
			"loadpalette",
			Translations.GetString ("Open..."),
			null,
			Resources.StandardIcons.DocumentOpen);

		SavePalette = new Command (
			"savepalette",
			Translations.GetString ("Save As..."),
			null,
			Resources.StandardIcons.DocumentSave);

		ResetPalette = new Command (
			"resetpalette",
			Translations.GetString ("Reset to Default"),
			null,
			Resources.StandardIcons.DocumentRevert);

		ResizePalette = new Command (
			"resizepalette",
			Translations.GetString ("Set Number of Colors"),
			null,
			Resources.Icons.ImageResize);

		Undo.Sensitive = false;
		Redo.Sensitive = false;

		this.chrome = chrome;
		palette_formats = paletteFormats;
		this.palette = palette;
		this.tools = tools;
		this.workspace = workspace;
	}

	public void RegisterActions (Gtk.Application app, Gio.Menu menu)
	{
		Gio.Menu paste_section = Gio.Menu.New ();
		paste_section.AppendItem (Cut.CreateMenuItem ());
		paste_section.AppendItem (Copy.CreateMenuItem ());
		paste_section.AppendItem (CopyMerged.CreateMenuItem ());
		paste_section.AppendItem (Paste.CreateMenuItem ());
		paste_section.AppendItem (PasteIntoNewLayer.CreateMenuItem ());
		paste_section.AppendItem (PasteIntoNewImage.CreateMenuItem ());

		Gio.Menu sel_section = Gio.Menu.New ();
		sel_section.AppendItem (SelectAll.CreateMenuItem ());
		sel_section.AppendItem (Deselect.CreateMenuItem ());

		Gio.Menu edit_sel_section = Gio.Menu.New ();
		edit_sel_section.AppendItem (EraseSelection.CreateMenuItem ());
		edit_sel_section.AppendItem (FillSelection.CreateMenuItem ());
		edit_sel_section.AppendItem (InvertSelection.CreateMenuItem ());
		edit_sel_section.AppendItem (OffsetSelection.CreateMenuItem ());

		Gio.Menu palette_section = Gio.Menu.New ();

		Gio.Menu palette_menu = Gio.Menu.New ();
		palette_menu.AppendItem (LoadPalette.CreateMenuItem ());
		palette_menu.AppendItem (SavePalette.CreateMenuItem ());
		palette_menu.AppendItem (ResetPalette.CreateMenuItem ());
		palette_menu.AppendItem (ResizePalette.CreateMenuItem ());

		menu.AppendItem (Undo.CreateMenuItem ());
		menu.AppendItem (Redo.CreateMenuItem ());
		menu.AppendSection (null, paste_section);
		menu.AppendSection (null, sel_section);
		menu.AppendSection (null, edit_sel_section);
		menu.AppendSection (null, palette_section);
		menu.AppendSubmenu (Translations.GetString ("Palette"), palette_menu);

		app.SetActionsAndShortcuts ([

			Undo,
			Redo,

			Cut,
			Copy,
			CopyMerged,
			Paste,
			PasteIntoNewLayer,
			PasteIntoNewImage,

			SelectAll,
			Deselect,

			EraseSelection,
			FillSelection,
			InvertSelection,
			OffsetSelection,
			LoadPalette,
			SavePalette,
			ResetPalette,
			ResizePalette]);
	}

	public void CreateHistoryWindowToolBar (Gtk.Box toolbar)
	{
		toolbar.Append (Undo.CreateToolBarItem ());
		toolbar.Append (Redo.CreateToolBarItem ());
	}

	public void RegisterHandlers ()
	{
		Deselect.Activated += HandlePintaCoreActionsEditDeselectActivated;
		EraseSelection.Activated += HandlePintaCoreActionsEditEraseSelectionActivated;
		SelectAll.Activated += HandlePintaCoreActionsEditSelectAllActivated;
		FillSelection.Activated += HandlePintaCoreActionsEditFillSelectionActivated;
		Copy.Activated += HandlerPintaCoreActionsEditCopyActivated;
		CopyMerged.Activated += HandlerPintaCoreActionsEditCopyMergedActivated;
		Undo.Activated += HandlerPintaCoreActionsEditUndoActivated;
		Redo.Activated += HandlerPintaCoreActionsEditRedoActivated;
		Cut.Activated += HandlerPintaCoreActionsEditCutActivated;
		LoadPalette.Activated += HandlerPintaCoreActionsEditLoadPaletteActivated;
		SavePalette.Activated += HandlerPintaCoreActionsEditSavePaletteActivated;
		ResetPalette.Activated += HandlerPintaCoreActionsEditResetPaletteActivated;
		InvertSelection.Activated += HandleInvertSelectionActivated;

		workspace.ActiveDocumentChanged += WorkspaceActiveDocumentChanged;

		workspace.SelectionChanged += (o, _) => {
			var visible = false;
			if (workspace.HasOpenDocuments)
				visible = workspace.ActiveDocument.Selection.Visible;

			Deselect.Sensitive = visible;
			EraseSelection.Sensitive = visible;
			FillSelection.Sensitive = visible;
			InvertSelection.Sensitive = visible;
			OffsetSelection.Sensitive = visible;
		};
	}

	#region Action Handlers
	private void HandlePintaCoreActionsEditFillSelectionActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		ImageSurface old = doc.Layers.CurrentUserLayer.Surface.Clone ();

		using Context g = new (doc.Layers.CurrentUserLayer.Surface);

		g.AppendPath (doc.Selection.SelectionPath);
		g.FillRule = FillRule.EvenOdd;

		g.SetSourceColor (palette.PrimaryColor);
		g.Fill ();

		doc.Workspace.Invalidate ();
		doc.History.PushNewItem (
			new SimpleHistoryItem (
				Resources.Icons.EditSelectionFill,
				Translations.GetString ("Fill Selection"),
				old,
				doc.Layers.CurrentUserLayerIndex
			)
		);
	}

	private void HandlePintaCoreActionsEditSelectAllActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		SelectionHistoryItem hist = new (
			workspace,
			Resources.StandardIcons.EditSelectAll,
			Translations.GetString ("Select All"));

		hist.TakeSnapshot ();

		doc.ResetSelectionPaths ();
		doc.Selection.Visible = true;

		doc.History.PushNewItem (hist);
		doc.Workspace.Invalidate ();
	}

	private void HandlePintaCoreActionsEditEraseSelectionActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		ImageSurface old = doc.Layers.CurrentUserLayer.Surface.Clone ();

		using Context g = new (doc.Layers.CurrentUserLayer.Surface);

		g.AppendPath (doc.Selection.SelectionPath);
		g.FillRule = FillRule.EvenOdd;

		g.Operator = Cairo.Operator.Clear;
		g.Fill ();

		doc.Workspace.Invalidate ();

		doc.History.PushNewItem (
			sender switch {
				string and "Cut" => new SimpleHistoryItem (Resources.StandardIcons.EditCut, Translations.GetString ("Cut"), old, doc.Layers.CurrentUserLayerIndex),
				_ => new SimpleHistoryItem (Resources.Icons.EditSelectionErase, Translations.GetString ("Erase Selection"), old, doc.Layers.CurrentUserLayerIndex),
			}
		);
	}

	private void HandlePintaCoreActionsEditDeselectActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		SelectionHistoryItem hist = new (
			workspace,
			Resources.Icons.EditSelectionNone,
			Translations.GetString ("Deselect"));

		hist.TakeSnapshot ();

		doc.ResetSelectionPaths ();

		doc.History.PushNewItem (hist);
		doc.Workspace.Invalidate ();
	}

	private void HandlerPintaCoreActionsEditCopyActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		Gdk.Clipboard cb = GdkExtensions.GetDefaultClipboard ();

		if (tools.CurrentTool?.DoHandleCopy (doc, cb) == true)
			return;

		tools.Commit ();

		ImageSurface src = doc.Layers.GetClippedLayer (doc.Layers.CurrentUserLayerIndex);

		RectangleI rect = doc.GetSelectedBounds (true);
		if (rect.IsEmpty)
			return;

		ImageSurface dest = CairoExtensions.CreateImageSurface (Format.Argb32, rect.Width, rect.Height);

		using Context g = new (dest);

		g.SetSourceSurface (src, -rect.X, -rect.Y);
		g.Paint ();

		cb.SetImage (dest);
	}

	private void HandlerPintaCoreActionsEditCopyMergedActivated (object sender, EventArgs e)
	{
		Gdk.Clipboard cb = GdkExtensions.GetDefaultClipboard ();
		Document doc = workspace.ActiveDocument;

		tools.Commit ();

		// Get our merged ("flattened") image
		ImageSurface src = doc.GetFlattenedImage (/* clip_to_selection */ true);
		RectangleI rect = doc.GetSelectedBounds (true);

		// Copy it to a correctly sized surface 
		ImageSurface dest = CairoExtensions.CreateImageSurface (Format.Argb32, rect.Width, rect.Height);

		using Context g = new (dest);

		g.SetSourceSurface (src, -rect.X, -rect.Y);
		g.Paint ();

		// Give it to the clipboard
		cb.SetImage (dest);
	}

	private void HandlerPintaCoreActionsEditCutActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		Gdk.Clipboard cb = GdkExtensions.GetDefaultClipboard ();

		if (tools.CurrentTool?.DoHandleCut (doc, cb) == true)
			return;

		tools.Commit ();

		// Copy selection
		HandlerPintaCoreActionsEditCopyActivated (sender, e);

		// Erase selection
		HandlePintaCoreActionsEditEraseSelectionActivated ("Cut", e);
	}

	private void HandlerPintaCoreActionsEditUndoActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		if (tools.CurrentTool?.DoHandleUndo (doc) == true)
			return;

		doc.History.Undo ();

		tools.CurrentTool?.DoAfterUndo (doc);
	}

	private void HandlerPintaCoreActionsEditRedoActivated (object sender, EventArgs e)
	{
		Document doc = workspace.ActiveDocument;

		if (tools.CurrentTool?.DoHandleRedo (doc) == true)
			return;

		doc.History.Redo ();

		tools.CurrentTool?.DoAfterRedo (doc);
	}

	private async void HandlerPintaCoreActionsEditLoadPaletteActivated (object sender, EventArgs e)
	{
		using Gtk.FileFilter palettesFilter = CreatePalettesFilter ();
		using Gtk.FileFilter catchAllFilter = CreateCatchAllFilter ();

		using Gio.ListStore filters = Gio.ListStore.New (Gtk.FileFilter.GetGType ());
		filters.Append (palettesFilter);
		filters.Append (catchAllFilter);

		using Gtk.FileDialog fileDialog = Gtk.FileDialog.New ();
		fileDialog.SetTitle (Translations.GetString ("Open Palette File"));
		fileDialog.SetFilters (filters);
		if (last_palette_dir != null)
			fileDialog.SetInitialFolder (last_palette_dir);

		var choice = await fileDialog.OpenFileAsync (chrome.MainWindow);

		if (choice is null)
			return;

		last_palette_dir = choice.GetParent ();
		palette.CurrentPalette.Load (palette_formats, choice);
	}

	private void HandlerPintaCoreActionsEditSavePaletteActivated (object sender, EventArgs e)
	{
		var fcd = Gtk.FileChooserNative.New (
			Translations.GetString ("Save Palette File"),
			chrome.MainWindow,
			Gtk.FileChooserAction.Save,
			Translations.GetString ("Save"),
			Translations.GetString ("Cancel"));

		foreach (var format in palette_formats.Formats) {

			if (format.IsReadOnly ())
				continue;

			Gtk.FileFilter fileFilter = format.Filter;
			fcd.AddFilter (fileFilter);
		}

		if (last_palette_dir != null)
			fcd.SetCurrentFolder (last_palette_dir);

		fcd.OnResponse += (_, args) => {

			Gtk.ResponseType response = (Gtk.ResponseType) args.ResponseId;

			if (response != Gtk.ResponseType.Accept)
				return;

			Gio.File file = fcd.GetFile ()!;

			// Add in the extension if necessary, based on the current selected file filter.
			// Note: on macOS, fcd.Filter doesn't seem to properly update to the current filter.
			// However, on macOS the dialog always adds the extension automatically, so this issue doesn't matter.
			string basename = file.GetParent ()!.GetRelativePath (file)!;
			string extension = System.IO.Path.GetExtension (basename);
			if (string.IsNullOrEmpty (extension)) {
				var currentFormat = palette_formats.Formats.First (f => f.Filter == fcd.Filter);
				basename += "." + currentFormat.Extensions.First ();
				file = file.GetParent ()!.GetChild (basename);
			}

			PaletteDescriptor format = palette_formats.GetFormatByFilename (basename) ?? throw new FormatException ();
			palette.CurrentPalette.Save (file, format.Saver);
			last_palette_dir = file.GetParent ();
		};

		fcd.Show ();
	}

	private Gtk.FileFilter CreatePalettesFilter ()
	{
		Gtk.FileFilter palettesFilter = Gtk.FileFilter.New ();

		palettesFilter.Name = Translations.GetString ("Palette files");

		foreach (var format in palette_formats.Formats) {

			if (format.IsWriteOnly ())
				continue;

			foreach (var ext in format.Extensions)
				palettesFilter.AddPattern ($"*.{ext}");
		}

		return palettesFilter;
	}

	private static Gtk.FileFilter CreateCatchAllFilter ()
	{
		Gtk.FileFilter catchAllFilter = Gtk.FileFilter.New ();
		catchAllFilter.Name = Translations.GetString ("All files");
		catchAllFilter.AddPattern ("*");
		return catchAllFilter;
	}

	private void HandlerPintaCoreActionsEditResetPaletteActivated (object sender, EventArgs e)
	{
		palette.CurrentPalette.LoadDefault ();
	}

	void HandleInvertSelectionActivated (object sender, EventArgs e)
	{
		tools.Commit ();

		Document doc = workspace.ActiveDocument;

		// Clear the selection resize handles if necessary.
		doc.Layers.ToolLayer.Clear ();

		SelectionHistoryItem historyItem = new (
			workspace,
			Resources.Icons.EditSelectionInvert,
			Translations.GetString ("Invert Selection"));

		historyItem.TakeSnapshot ();

		doc.Selection.Invert (doc.ImageSize);

		doc.History.PushNewItem (historyItem);
		doc.Workspace.Invalidate ();
	}

	private void WorkspaceActiveDocumentChanged (object? sender, EventArgs e)
	{
		if (!workspace.HasOpenDocuments) {
			Undo.Sensitive = false;
			Redo.Sensitive = false;
			return;
		}

		Redo.Sensitive = workspace.ActiveWorkspace.History.CanRedo;
		Undo.Sensitive = workspace.ActiveWorkspace.History.CanUndo;
	}
	#endregion
}
