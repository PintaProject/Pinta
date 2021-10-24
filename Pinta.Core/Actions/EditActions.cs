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
using System.IO;
using Gtk;
using Cairo;
using System.Linq;

namespace Pinta.Core
{
	public class EditActions
	{
		public Command Undo { get; private set; }
		public Command Redo { get; private set; }
		public Command Cut { get; private set; }
		public Command Copy { get; private set; }
		public Command CopyMerged { get; private set; }
		public Command Paste { get; private set; }
		public Command PasteIntoNewLayer { get; private set; }
		public Command PasteIntoNewImage { get; private set; }
		public Command EraseSelection { get; private set; }
		public Command FillSelection { get; private set; }
		public Command InvertSelection { get; private set; }
		public Command SelectAll { get; private set; }
		public Command Deselect { get; private set; }
		public Command LoadPalette { get; private set; }
		public Command SavePalette { get; private set; }
		public Command ResetPalette { get; private set; }
		public Command ResizePalette { get; private set; }
		
		private string? lastPaletteDir = null;
		
		public EditActions ()
		{
			Undo = new Command ("undo", Translations.GetString ("Undo"), null, Resources.StandardIcons.EditUndo);
			Redo = new Command ("redo", Translations.GetString ("Redo"), null, Resources.StandardIcons.EditRedo);
			Cut = new Command ("cut", Translations.GetString ("Cut"), null, Resources.StandardIcons.EditCut);
			Copy = new Command ("copy", Translations.GetString ("Copy"), null, Resources.StandardIcons.EditCopy);
			CopyMerged = new Command ("copymerged", Translations.GetString ("Copy Merged"), null, Resources.StandardIcons.EditCopy);
			Paste = new Command ("paste", Translations.GetString ("Paste"), null, Resources.StandardIcons.EditPaste);
			PasteIntoNewLayer = new Command ("pasteintonewlayer", Translations.GetString ("Paste Into New Layer"), null, Resources.StandardIcons.EditPaste);
			PasteIntoNewImage = new Command ("pasteintonewimage", Translations.GetString ("Paste Into New Image"), null, Resources.StandardIcons.EditPaste);
			EraseSelection = new Command ("eraseselection", Translations.GetString ("Erase Selection"), null, Resources.Icons.EditSelectionErase);
			FillSelection = new Command ("fillselection", Translations.GetString ("Fill Selection"), null, Resources.Icons.EditSelectionFill);
			InvertSelection = new Command ("invertselection", Translations.GetString ("Invert Selection"), null, Resources.Icons.EditSelectionFill);
			SelectAll = new Command ("selectall", Translations.GetString ("Select All"), null, Resources.StandardIcons.EditSelectAll);
			Deselect = new Command ("deselect", Translations.GetString ("Deselect All"), null, Resources.Icons.EditSelectionNone);
			
			LoadPalette = new Command ("loadpalette", Translations.GetString ("Open..."), null, Resources.StandardIcons.DocumentOpen);
			SavePalette = new Command ("savepalette", Translations.GetString ("Save As..."), null, Resources.StandardIcons.DocumentSave);
			ResetPalette = new Command ("resetpalette", Translations.GetString ("Reset to Default"), null, Resources.StandardIcons.DocumentRevert);
			ResizePalette = new Command ("resizepalette", Translations.GetString ("Set Number of Colors"), null, Resources.Icons.ImageResize);

			Undo.IsImportant = true;
			Undo.Sensitive = false;
			Redo.Sensitive = false;
		}

		#region Initialization
		public void RegisterActions(Gtk.Application app, GLib.Menu menu)
		{
			app.AddAccelAction(Undo, "<Primary>Z");
			menu.AppendItem(Undo.CreateMenuItem());

			app.AddAccelAction(Redo, new[] { "<Primary><Shift>Z", "<Ctrl>Y" });
			menu.AppendItem(Redo.CreateMenuItem());

			var paste_section = new GLib.Menu();
			menu.AppendSection(null, paste_section);

			app.AddAccelAction(Cut, "<Primary>X");
			paste_section.AppendItem(Cut.CreateMenuItem());

			app.AddAccelAction(Copy, "<Primary>C");
			paste_section.AppendItem(Copy.CreateMenuItem());

			app.AddAccelAction(CopyMerged, "<Primary><Shift>C");
			paste_section.AppendItem(CopyMerged.CreateMenuItem());

			app.AddAccelAction(Paste, "<Primary>V");
			paste_section.AppendItem(Paste.CreateMenuItem());

			app.AddAccelAction(PasteIntoNewLayer, "<Primary><Shift>V");
			paste_section.AppendItem(PasteIntoNewLayer.CreateMenuItem());

			app.AddAccelAction(PasteIntoNewImage, "<Primary><Alt>V");
			paste_section.AppendItem(PasteIntoNewImage.CreateMenuItem());

			var sel_section = new GLib.Menu();
			menu.AppendSection(null, sel_section);

			app.AddAccelAction(SelectAll, "<Primary>A");
			sel_section.AppendItem(SelectAll.CreateMenuItem());

			app.AddAccelAction(Deselect, new[] { "<Primary><Shift>A", "<Ctrl>D" });
			sel_section.AppendItem(Deselect.CreateMenuItem());

			var edit_sel_section = new GLib.Menu();
			menu.AppendSection(null, edit_sel_section);

			app.AddAccelAction(EraseSelection, "Delete");
			edit_sel_section.AppendItem(EraseSelection.CreateMenuItem());

			app.AddAccelAction(FillSelection, "BackSpace");
			edit_sel_section.AppendItem(FillSelection.CreateMenuItem());

			app.AddAccelAction(InvertSelection, "<Primary>I");
			edit_sel_section.AppendItem(InvertSelection.CreateMenuItem());

			var palette_section = new GLib.Menu();
			menu.AppendSection(null, palette_section);

			var palette_menu = new GLib.Menu();
			menu.AppendSubmenu(Translations.GetString("Palette"), palette_menu);

			app.AddAction(LoadPalette);
			palette_menu.AppendItem(LoadPalette.CreateMenuItem());

			app.AddAction(SavePalette);
			palette_menu.AppendItem(SavePalette.CreateMenuItem());

			app.AddAction(ResetPalette);
			palette_menu.AppendItem(ResetPalette.CreateMenuItem());

			app.AddAction(ResizePalette);
			palette_menu.AppendItem(ResizePalette.CreateMenuItem());
		}

		public void CreateHistoryWindowToolBar (Gtk.Toolbar toolbar)
		{
			toolbar.AppendItem (Undo.CreateToolBarItem ());
			toolbar.AppendItem (Redo.CreateToolBarItem ());
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

			PintaCore.Workspace.ActiveDocumentChanged += WorkspaceActiveDocumentChanged;

			PintaCore.Workspace.SelectionChanged += (o, _) => {
				var visible = false;
				if (PintaCore.Workspace.HasOpenDocuments)
					visible = PintaCore.Workspace.ActiveDocument.Selection.Visible;

				Deselect.Sensitive = visible;
				EraseSelection.Sensitive = visible;
				FillSelection.Sensitive = visible;
				InvertSelection.Sensitive = visible;
			};
		}

		#endregion

		#region Action Handlers
		private void HandlePintaCoreActionsEditFillSelectionActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			Cairo.ImageSurface old = doc.Layers.CurrentUserLayer.Surface.Clone ();

			using (var g = new Cairo.Context (doc.Layers.CurrentUserLayer.Surface)) {
				g.AppendPath (doc.Selection.SelectionPath);
				g.FillRule = FillRule.EvenOdd;

				g.SetSourceColor (PintaCore.Palette.PrimaryColor);
				g.Fill ();
			}

			doc.Workspace.Invalidate ();
			doc.History.PushNewItem (new SimpleHistoryItem (Resources.Icons.EditSelectionFill, Translations.GetString ("Fill Selection"), old, doc.Layers.CurrentUserLayerIndex));
		}

		private void HandlePintaCoreActionsEditSelectAllActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			SelectionHistoryItem hist = new SelectionHistoryItem (Resources.StandardIcons.EditSelectAll, Translations.GetString ("Select All"));
			hist.TakeSnapshot ();

			doc.ResetSelectionPaths ();
			doc.Selection.Visible = true;

			doc.History.PushNewItem (hist);
			doc.Workspace.Invalidate ();
		}

		private void HandlePintaCoreActionsEditEraseSelectionActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			Cairo.ImageSurface old = doc.Layers.CurrentUserLayer.Surface.Clone ();

			using (var g = new Cairo.Context (doc.Layers.CurrentUserLayer.Surface)) {
				g.AppendPath (doc.Selection.SelectionPath);
				g.FillRule = FillRule.EvenOdd;

				g.Operator = Cairo.Operator.Clear;
				g.Fill ();
			}

			doc.Workspace.Invalidate ();

			if (sender is string && (sender as string) == "Cut")
				doc.History.PushNewItem (new SimpleHistoryItem (Resources.StandardIcons.EditCut, Translations.GetString ("Cut"), old, doc.Layers.CurrentUserLayerIndex));
			else
				doc.History.PushNewItem (new SimpleHistoryItem (Resources.Icons.EditSelectionErase, Translations.GetString ("Erase Selection"), old, doc.Layers.CurrentUserLayerIndex));
		}

		private void HandlePintaCoreActionsEditDeselectActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			SelectionHistoryItem hist = new SelectionHistoryItem (Resources.Icons.EditSelectionNone, Translations.GetString ("Deselect"));
			hist.TakeSnapshot ();

            doc.ResetSelectionPaths ();

			doc.History.PushNewItem (hist);
			doc.Workspace.Invalidate ();
		}

		private void HandlerPintaCoreActionsEditCopyActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			if (PintaCore.Tools.CurrentTool?.DoHandleCopy (doc, cb) == true)
				return;

			PintaCore.Tools.Commit ();

			using (ImageSurface src = doc.Layers.GetClippedLayer (doc.Layers.CurrentUserLayerIndex)) {

				Gdk.Rectangle rect = doc.GetSelectedBounds (true);
				if (rect.Width == 0 || rect.Height == 0)
					return;
			
				ImageSurface dest = CairoExtensions.CreateImageSurface (Format.Argb32, rect.Width, rect.Height);

				using (Context g = new Context (dest)) {
					g.SetSourceSurface (src, -rect.X, -rect.Y);
					g.Paint ();
				}
			
				cb.Image = dest.ToPixbuf ();

				(dest as IDisposable).Dispose ();
			}
		}

		private void HandlerPintaCoreActionsEditCopyMergedActivated (object sender, EventArgs e)
		{
			var cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			var doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			// Get our merged ("flattened") image
			using (var src = doc.GetFlattenedImage ()) {
				var rect = doc.GetSelectedBounds (true);

				// Copy it to a correctly sized surface 
				using (var dest = CairoExtensions.CreateImageSurface (Format.Argb32, rect.Width, rect.Height)) {

					using (Context g = new Context (dest)) {
						g.SetSourceSurface (src, -rect.X, -rect.Y);
						g.Paint ();
					}

					// Give it to the clipboard
					cb.Image = dest.ToPixbuf ();
				}
			}
		}
		
		private void HandlerPintaCoreActionsEditCutActivated (object sender, EventArgs e)
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			if (PintaCore.Tools.CurrentTool?.DoHandleCut (doc, cb) == true)
				return;
			PintaCore.Tools.Commit ();
			
			// Copy selection
			HandlerPintaCoreActionsEditCopyActivated (sender, e);

			// Erase selection
			HandlePintaCoreActionsEditEraseSelectionActivated ("Cut", e);
		}

		private void HandlerPintaCoreActionsEditUndoActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			if (PintaCore.Tools.CurrentTool?.DoHandleUndo (doc) == true)
				return;
			doc.History.Undo ();
			PintaCore.Tools.CurrentTool?.DoAfterUndo(doc);
		}

		private void HandlerPintaCoreActionsEditRedoActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			if (PintaCore.Tools.CurrentTool?.DoHandleRedo (doc) == true)
				return;
			doc.History.Redo ();
			PintaCore.Tools.CurrentTool?.DoAfterRedo(doc);
		}

		private void HandlerPintaCoreActionsEditLoadPaletteActivated (object sender, EventArgs e)
		{
			using var fcd = new FileChooserNative (
				Translations.GetString ("Open Palette File"),
				PintaCore.Chrome.MainWindow,
				FileChooserAction.Open,
				Translations.GetString ("Open"),
				Translations.GetString ("Cancel"));

			var ff = new FileFilter {
				Name = Translations.GetString ("Palette files")
			};

			foreach (var format in PintaCore.System.PaletteFormats.Formats) {
				if (!format.IsWriteOnly ()) {
					foreach (var ext in format.Extensions)
						ff.AddPattern (string.Format ("*.{0}", ext));
				}
			}

			fcd.AddFilter (ff);

			FileFilter ff2 = new FileFilter {
				Name = Translations.GetString ("All files")
			};
			ff2.AddPattern ("*.*");
			fcd.AddFilter (ff2);

			if (lastPaletteDir != null)
				fcd.SetCurrentFolder (lastPaletteDir);

			var response = (ResponseType) fcd.Run ();

			if (response == ResponseType.Accept) {
				var filename = fcd.Filename;
				lastPaletteDir = System.IO.Path.GetDirectoryName (filename);
				PintaCore.Palette.CurrentPalette.Load (filename);
			}

		}

		private void HandlerPintaCoreActionsEditSavePaletteActivated (object sender, EventArgs e)
		{
			using var fcd = new FileChooserNative (
				Translations.GetString ("Save Palette File"),
				PintaCore.Chrome.MainWindow,
				FileChooserAction.Save,
				Translations.GetString ("Save"),
				Translations.GetString ("Cancel")) {
				DoOverwriteConfirmation = true
			};

			foreach (var format in PintaCore.System.PaletteFormats.Formats) {
				if (!format.IsReadOnly ()) {
					FileFilter fileFilter = format.Filter;
					fcd.AddFilter (fileFilter);
				}
			}

			if (lastPaletteDir != null)
				fcd.SetCurrentFolder (lastPaletteDir);

			var response = (ResponseType)fcd.Run ();

			if (response == Gtk.ResponseType.Accept) {
				string filename = fcd.Filename;

				// Add in the extension if necessary, based on the current selected file filter.
				// Note: on macOS, fcd.Filter doesn't seem to properly update to the current filter.
				// However, on macOS the dialog always adds the extension automatically, so this issue doesn't matter.
				string extension = System.IO.Path.GetExtension (filename);
				if (string.IsNullOrEmpty(extension)) {
					var currentFormat = PintaCore.System.PaletteFormats.Formats.First (f => f.Filter == fcd.Filter);
					filename += "." + currentFormat.Extensions.First ();
				}

				var format = PintaCore.System.PaletteFormats.GetFormatByFilename (filename);
				if (format is null)
					throw new FormatException ();

				PintaCore.Palette.CurrentPalette.Save (filename, format.Saver);
				lastPaletteDir = System.IO.Path.GetDirectoryName (filename);
			}

		}

		private void HandlerPintaCoreActionsEditResetPaletteActivated (object sender, EventArgs e)
		{
			PintaCore.Palette.CurrentPalette.LoadDefault ();
		}

		void HandleInvertSelectionActivated (object sender, EventArgs e)
		{
			PintaCore.Tools.Commit ();

			Document doc = PintaCore.Workspace.ActiveDocument;

			// Clear the selection resize handles if necessary.
			doc.Layers.ToolLayer.Clear ();

			SelectionHistoryItem historyItem = new SelectionHistoryItem (Resources.Icons.EditSelectionInvert,
			                                                             Translations.GetString ("Invert Selection"));
			historyItem.TakeSnapshot ();

			doc.Selection.Invert (doc.Layers.SelectionLayer.Surface, doc.ImageSize);

			doc.History.PushNewItem (historyItem);
			doc.Workspace.Invalidate ();
		}

		private void WorkspaceActiveDocumentChanged (object? sender, EventArgs e)
		{
			if (!PintaCore.Workspace.HasOpenDocuments) {
				Undo.Sensitive = false;
				Redo.Sensitive = false;
				return;
			}

			Redo.Sensitive = PintaCore.Workspace.ActiveWorkspace.History.CanRedo;
			Undo.Sensitive = PintaCore.Workspace.ActiveWorkspace.History.CanUndo;
		}
#endregion
	}
}
