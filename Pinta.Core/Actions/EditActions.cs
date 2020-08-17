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
		
		private string lastPaletteDir = null;
		
		public EditActions ()
		{
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add ("Menu.Edit.Deselect.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Edit.Deselect.png")));
			fact.Add ("Menu.Edit.EraseSelection.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Edit.EraseSelection.png")));
			fact.Add ("Menu.Edit.FillSelection.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Edit.FillSelection.png")));
			fact.Add ("Menu.Edit.InvertSelection.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Edit.InvertSelection.png")));
			fact.Add ("Menu.Edit.SelectAll.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Edit.SelectAll.png")));
			fact.Add ("Menu.Edit.Addins.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Edit.Addins.png")));
			fact.AddDefault ();
			
			Undo = new Command ("undo", Translations.GetString ("Undo"), null, "edit-undo");
			Redo = new Command ("redo", Translations.GetString ("Redo"), null, "edit-redo");
			Cut = new Command ("cut", Translations.GetString ("Cut"), null, "edit-cut");
			Copy = new Command ("copy", Translations.GetString ("Copy"), null, "edit-copy");
			CopyMerged = new Command ("copymerged", Translations.GetString ("Copy Merged"), null, "edit-copy");
			Paste = new Command ("paste", Translations.GetString ("Paste"), null, "edit-paste");
			PasteIntoNewLayer = new Command ("pasteintonewlayer", Translations.GetString ("Paste Into New Layer"), null, "edit-paste");
			PasteIntoNewImage = new Command ("pasteintonewimage", Translations.GetString ("Paste Into New Image"), null, "edit-paste");
			EraseSelection = new Command ("eraseselection", Translations.GetString ("Erase Selection"), null, "Menu.Edit.EraseSelection.png");
			FillSelection = new Command ("fillselection", Translations.GetString ("Fill Selection"), null, "Menu.Edit.FillSelection.png");
			InvertSelection = new Command ("invertselection", Translations.GetString ("Invert Selection"), null, "Menu.Edit.InvertSelection.png");
			SelectAll = new Command ("selectall", Translations.GetString ("Select All"), null, "edit-select-all");
			Deselect = new Command ("deselect", Translations.GetString ("Deselect All"), null, "Menu.Edit.Deselect.png");
			
			LoadPalette = new Command ("loadpalette", Translations.GetString ("Open..."), null, "document-open");
			SavePalette = new Command ("savepalette", Translations.GetString ("Save As..."), null, "document-save");
			ResetPalette = new Command ("resetpalette", Translations.GetString ("Reset to Default"), null, "document-revert");
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

			// TODO-GTK3 (also add Ctrl+Y from the GTK2 build for Windows?)
			app.AddAccelAction(Redo, "<Primary><Shift>Z");
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
			paste_section.AppendItem(Cut.CreateMenuItem());

			var sel_section = new GLib.Menu();
			menu.AppendSection(null, sel_section);

			app.AddAccelAction(SelectAll, "<Primary>A");
			sel_section.AppendItem(SelectAll.CreateMenuItem());

			// TODO-GTK3 (also add old Ctrl+D from the GTK2 build?)
			app.AddAccelAction(Deselect, "<Primary><Shift>A");
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

			Cairo.ImageSurface old = doc.CurrentUserLayer.Surface.Clone ();

			using (var g = new Cairo.Context (doc.CurrentUserLayer.Surface)) {
				g.AppendPath (doc.Selection.SelectionPath);
				g.FillRule = FillRule.EvenOdd;

				g.SetSourceColor (PintaCore.Palette.PrimaryColor);
				g.Fill ();
			}

			doc.Workspace.Invalidate ();
			doc.History.PushNewItem (new SimpleHistoryItem ("Menu.Edit.FillSelection.png", Translations.GetString ("Fill Selection"), old, doc.CurrentUserLayerIndex));
		}

		private void HandlePintaCoreActionsEditSelectAllActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			SelectionHistoryItem hist = new SelectionHistoryItem (Stock.SelectAll, Translations.GetString ("Select All"));
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

			Cairo.ImageSurface old = doc.CurrentUserLayer.Surface.Clone ();

			using (var g = new Cairo.Context (doc.CurrentUserLayer.Surface)) {
				g.AppendPath (doc.Selection.SelectionPath);
				g.FillRule = FillRule.EvenOdd;

				g.Operator = Cairo.Operator.Clear;
				g.Fill ();
			}

			doc.Workspace.Invalidate ();

			if (sender is string && (sender as string) == "Cut")
				doc.History.PushNewItem (new SimpleHistoryItem (Stock.Cut, Translations.GetString ("Cut"), old, doc.CurrentUserLayerIndex));
			else
				doc.History.PushNewItem (new SimpleHistoryItem ("Menu.Edit.EraseSelection.png", Translations.GetString ("Erase Selection"), old, doc.CurrentUserLayerIndex));
		}

		private void HandlePintaCoreActionsEditDeselectActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			SelectionHistoryItem hist = new SelectionHistoryItem ("Menu.Edit.Deselect.png", Translations.GetString ("Deselect"));
			hist.TakeSnapshot ();

            doc.ResetSelectionPaths ();

			doc.History.PushNewItem (hist);
			doc.Workspace.Invalidate ();
		}

		private void HandlerPintaCoreActionsEditCopyActivated (object sender, EventArgs e)
		{
			Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			if (PintaCore.Tools.CurrentTool.TryHandleCopy (cb))
				return;

			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			using (ImageSurface src = doc.GetClippedLayer (doc.CurrentUserLayerIndex)) {

				Gdk.Rectangle rect = doc.GetSelectedBounds (true);
				if (rect.Width == 0 || rect.Height == 0)
					return;
			
				ImageSurface dest = new ImageSurface (Format.Argb32, rect.Width, rect.Height);

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
				using (var dest = new ImageSurface (Format.Argb32, rect.Width, rect.Height)) {
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
			Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			if (PintaCore.Tools.CurrentTool.TryHandleCut (cb))
				return;
			PintaCore.Tools.Commit ();
			
			// Copy selection
			HandlerPintaCoreActionsEditCopyActivated (sender, e);

			// Erase selection
			HandlePintaCoreActionsEditEraseSelectionActivated ("Cut", e);
		}

		private void HandlerPintaCoreActionsEditUndoActivated (object sender, EventArgs e)
		{
			if (PintaCore.Tools.CurrentTool.TryHandleUndo ())
				return;
			Document doc = PintaCore.Workspace.ActiveDocument;
			doc.History.Undo ();
			PintaCore.Tools.CurrentTool.AfterUndo();
		}

		private void HandlerPintaCoreActionsEditRedoActivated (object sender, EventArgs e)
		{
			if (PintaCore.Tools.CurrentTool.TryHandleRedo ())
				return;
			Document doc = PintaCore.Workspace.ActiveDocument;
			doc.History.Redo ();
			PintaCore.Tools.CurrentTool.AfterRedo();
		}

		private void HandlerPintaCoreActionsEditLoadPaletteActivated (object sender, EventArgs e)
		{
			using (var fcd = new Gtk.FileChooserDialog(Translations.GetString("Open Palette File"), PintaCore.Chrome.MainWindow,
				FileChooserAction.Open, Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
				Gtk.Stock.Open, Gtk.ResponseType.Ok))
			{
				FileFilter ff = new FileFilter();
				foreach (var format in PintaCore.System.PaletteFormats.Formats)
				{
					if (!format.IsWriteOnly())
					{
						foreach (var ext in format.Extensions)
							ff.AddPattern(string.Format("*.{0}", ext));
					}
				}

				ff.Name = Translations.GetString("Palette files");
				fcd.AddFilter(ff);

				FileFilter ff2 = new FileFilter();
				ff2.Name = Translations.GetString("All files");
				ff2.AddPattern("*.*");
				fcd.AddFilter(ff2);

				fcd.AlternativeButtonOrder = new int[] { (int)ResponseType.Ok, (int)ResponseType.Cancel };

				if (lastPaletteDir != null)
					fcd.SetCurrentFolder(lastPaletteDir);

				int response = fcd.Run();

				if (response == (int)Gtk.ResponseType.Ok)
				{
					lastPaletteDir = fcd.CurrentFolder;
					PintaCore.Palette.CurrentPalette.Load(fcd.Filename);
				}
			}
		}

		private void HandlerPintaCoreActionsEditSavePaletteActivated (object sender, EventArgs e)
		{
			using (var fcd = new Gtk.FileChooserDialog(Translations.GetString("Save Palette File"), PintaCore.Chrome.MainWindow,
				FileChooserAction.Save, Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
				Gtk.Stock.Save, Gtk.ResponseType.Ok))
			{
				foreach (var format in PintaCore.System.PaletteFormats.Formats)
				{
					if (!format.IsReadOnly())
					{
						FileFilter fileFilter = format.Filter;
						fcd.AddFilter(fileFilter);
					}
				}

				fcd.AlternativeButtonOrder = new int[] { (int)ResponseType.Ok, (int)ResponseType.Cancel };

				if (lastPaletteDir != null)
					fcd.SetCurrentFolder(lastPaletteDir);

				int response = fcd.Run();

				if (response == (int)Gtk.ResponseType.Ok)
				{
					var format = PintaCore.System.PaletteFormats.Formats.FirstOrDefault(f => f.Filter == fcd.Filter);

					string finalFileName = fcd.Filename;

					string extension = System.IO.Path.GetExtension(fcd.Filename);
					if (string.IsNullOrEmpty(extension))
						finalFileName += "." + format.Extensions.First();

					PintaCore.Palette.CurrentPalette.Save(finalFileName, format.Saver);
				}
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
			doc.ToolLayer.Clear ();

			SelectionHistoryItem historyItem = new SelectionHistoryItem ("Menu.Edit.InvertSelection.png",
			                                                             Translations.GetString ("Invert Selection"));
			historyItem.TakeSnapshot ();

			doc.Selection.Invert (doc.SelectionLayer.Surface, doc.ImageSize);

			doc.History.PushNewItem (historyItem);
			doc.Workspace.Invalidate ();
		}

		private void WorkspaceActiveDocumentChanged (object sender, EventArgs e)
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
