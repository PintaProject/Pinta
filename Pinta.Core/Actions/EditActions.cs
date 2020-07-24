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
using Mono.Unix;
using System.Linq;

namespace Pinta.Core
{
	public class EditActions
	{
		public Gtk.Action Undo { get; private set; }
		public Gtk.Action Redo { get; private set; }
		public Gtk.Action Cut { get; private set; }
		public Gtk.Action Copy { get; private set; }
		public Gtk.Action CopyMerged { get; private set; }
		public Gtk.Action Paste { get; private set; }
		public Gtk.Action PasteIntoNewLayer { get; private set; }
		public Gtk.Action PasteIntoNewImage { get; private set; }
		public Gtk.Action EraseSelection { get; private set; }
		public Gtk.Action FillSelection { get; private set; }
		public Gtk.Action InvertSelection { get; private set; }
		public Gtk.Action SelectAll { get; private set; }
		public Gtk.Action Deselect { get; private set; }
		public Gtk.Action LoadPalette { get; private set; }
		public Gtk.Action SavePalette { get; private set; }
		public Gtk.Action ResetPalette { get; private set; }
		public Gtk.Action ResizePalette { get; private set; }
		
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
			
			Undo = new Gtk.Action ("Undo", Catalog.GetString ("Undo"), null, Stock.Undo);
			Redo = new Gtk.Action ("Redo", Catalog.GetString ("Redo"), null, Stock.Redo);
			Cut = new Gtk.Action ("Cut", Catalog.GetString ("Cut"), null, Stock.Cut);
			Copy = new Gtk.Action ("Copy", Catalog.GetString ("Copy"), null, Stock.Copy);
			CopyMerged = new Gtk.Action ("CopyMerged", Catalog.GetString ("Copy Merged"), null, Stock.Copy);
			Paste = new Gtk.Action ("Paste", Catalog.GetString ("Paste"), null, Stock.Paste);
			PasteIntoNewLayer = new Gtk.Action ("PasteIntoNewLayer", Catalog.GetString ("Paste Into New Layer"), null, Stock.Paste);
			PasteIntoNewImage = new Gtk.Action ("PasteIntoNewImage", Catalog.GetString ("Paste Into New Image"), null, Stock.Paste);
			EraseSelection = new Gtk.Action ("EraseSelection", Catalog.GetString ("Erase Selection"), null, "Menu.Edit.EraseSelection.png");
			FillSelection = new Gtk.Action ("FillSelection", Catalog.GetString ("Fill Selection"), null, "Menu.Edit.FillSelection.png");
			InvertSelection = new Gtk.Action ("InvertSelection", Catalog.GetString ("Invert Selection"), null, "Menu.Edit.InvertSelection.png");
			SelectAll = new Gtk.Action ("SelectAll", Catalog.GetString ("Select All"), null, Stock.SelectAll);
			Deselect = new Gtk.Action ("Deselect", Catalog.GetString ("Deselect All"), null, "Menu.Edit.Deselect.png");
			
			LoadPalette = new Gtk.Action ("LoadPalette", Catalog.GetString ("Open..."), null, Stock.Open);
			SavePalette = new Gtk.Action ("SavePalette", Catalog.GetString ("Save As..."), null, Stock.Save);
			ResetPalette = new Gtk.Action ("ResetPalette", Catalog.GetString ("Reset to Default"), null, Stock.RevertToSaved);
			ResizePalette = new Gtk.Action ("ResizePalette", Catalog.GetString ("Set Number of Colors"), null, "Menu.Image.Resize.png");

			Undo.IsImportant = true;
			Undo.Sensitive = false;
			Redo.Sensitive = false;
			InvertSelection.Sensitive = false;
			Deselect.Sensitive = false;
			EraseSelection.Sensitive = false;
			FillSelection.Sensitive = false;
		}

		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
			menu.Append (Undo.CreateAcceleratedMenuItem (Gdk.Key.Z, Gdk.ModifierType.ControlMask));

			ImageMenuItem redo = Redo.CreateAcceleratedMenuItem (Gdk.Key.Z, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask);
			redo.AddAccelerator ("activate", PintaCore.Actions.AccelGroup, new AccelKey (Gdk.Key.Y, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
			menu.Append (redo);

			menu.AppendSeparator ();
			menu.Append (Cut.CreateAcceleratedMenuItem (Gdk.Key.X, Gdk.ModifierType.ControlMask));
			menu.Append (Copy.CreateAcceleratedMenuItem (Gdk.Key.C, Gdk.ModifierType.ControlMask));
			menu.Append (CopyMerged.CreateAcceleratedMenuItem (Gdk.Key.C, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.Append (Paste.CreateAcceleratedMenuItem (Gdk.Key.V, Gdk.ModifierType.ControlMask));
			menu.Append (PasteIntoNewLayer.CreateAcceleratedMenuItem (Gdk.Key.V, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask));
			menu.Append (PasteIntoNewImage.CreateAcceleratedMenuItem (Gdk.Key.V, Gdk.ModifierType.Mod1Mask | Gdk.ModifierType.ControlMask));
			
			menu.AppendSeparator ();
			menu.Append (SelectAll.CreateAcceleratedMenuItem (Gdk.Key.A, Gdk.ModifierType.ControlMask));

			ImageMenuItem deslect = Deselect.CreateAcceleratedMenuItem (Gdk.Key.A, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask);
			deslect.AddAccelerator ("activate", PintaCore.Actions.AccelGroup, new AccelKey (Gdk.Key.D, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
			menu.Append (deslect);

			menu.AppendSeparator ();
			menu.Append (EraseSelection.CreateAcceleratedMenuItem (Gdk.Key.Delete, Gdk.ModifierType.None));
			menu.Append (FillSelection.CreateAcceleratedMenuItem (Gdk.Key.BackSpace, Gdk.ModifierType.None));
			menu.Append (InvertSelection.CreateAcceleratedMenuItem (Gdk.Key.I, Gdk.ModifierType.ControlMask));
			
			menu.AppendSeparator ();
			Gtk.Action menu_action = new Gtk.Action ("Palette", Mono.Unix.Catalog.GetString ("Palette"), null, null);
			Menu palette_menu = (Menu) menu.AppendItem (menu_action.CreateSubMenuItem ()).Submenu;
			palette_menu.Append (LoadPalette.CreateMenuItem ());
			palette_menu.Append (SavePalette.CreateMenuItem ());
			palette_menu.Append (ResetPalette.CreateMenuItem ());
			palette_menu.Append (ResizePalette.CreateMenuItem ());
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
			doc.History.PushNewItem (new SimpleHistoryItem ("Menu.Edit.FillSelection.png", Catalog.GetString ("Fill Selection"), old, doc.CurrentUserLayerIndex));
		}

		private void HandlePintaCoreActionsEditSelectAllActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			SelectionHistoryItem hist = new SelectionHistoryItem (Stock.SelectAll, Catalog.GetString ("Select All"));
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
				doc.History.PushNewItem (new SimpleHistoryItem (Stock.Cut, Catalog.GetString ("Cut"), old, doc.CurrentUserLayerIndex));
			else
				doc.History.PushNewItem (new SimpleHistoryItem ("Menu.Edit.EraseSelection.png", Catalog.GetString ("Erase Selection"), old, doc.CurrentUserLayerIndex));
		}

		private void HandlePintaCoreActionsEditDeselectActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			SelectionHistoryItem hist = new SelectionHistoryItem ("Menu.Edit.Deselect.png", Catalog.GetString ("Deselect"));
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
			var fcd = new Gtk.FileChooserDialog (Catalog.GetString ("Open Palette File"), PintaCore.Chrome.MainWindow,
				FileChooserAction.Open, Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
				Gtk.Stock.Open, Gtk.ResponseType.Ok);

			FileFilter ff = new FileFilter ();
			foreach (var format in PintaCore.System.PaletteFormats.Formats) {
				if (!format.IsWriteOnly ()) {
					foreach (var ext in format.Extensions)
						ff.AddPattern (string.Format("*.{0}", ext));
				}
			}

			ff.Name = Catalog.GetString ("Palette files");
			fcd.AddFilter (ff);

			FileFilter ff2 = new FileFilter ();
			ff2.Name = Catalog.GetString ("All files");
			ff2.AddPattern ("*.*");
			fcd.AddFilter (ff2);

			fcd.AlternativeButtonOrder = new int[] { (int) ResponseType.Ok, (int) ResponseType.Cancel };

			if (lastPaletteDir != null)
				fcd.SetCurrentFolder (lastPaletteDir);
			
			int response = fcd.Run ();
		
			if (response == (int) Gtk.ResponseType.Ok) {
				lastPaletteDir = fcd.CurrentFolder;
				PintaCore.Palette.CurrentPalette.Load (fcd.Filename);
			}

			fcd.Destroy ();
		}

		private void HandlerPintaCoreActionsEditSavePaletteActivated (object sender, EventArgs e)
		{
			var fcd = new Gtk.FileChooserDialog (Catalog.GetString ("Save Palette File"), PintaCore.Chrome.MainWindow,
				FileChooserAction.Save, Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
				Gtk.Stock.Save, Gtk.ResponseType.Ok);

			foreach (var format in PintaCore.System.PaletteFormats.Formats) {
				if (!format.IsReadOnly ()) {
					FileFilter fileFilter = format.Filter;
					fcd.AddFilter (fileFilter);
				}
			}

			fcd.AlternativeButtonOrder = new int[] { (int) ResponseType.Ok, (int) ResponseType.Cancel };

			if (lastPaletteDir != null)
				fcd.SetCurrentFolder (lastPaletteDir);

			int response = fcd.Run ();

			if (response == (int) Gtk.ResponseType.Ok) {
				var format = PintaCore.System.PaletteFormats.Formats.FirstOrDefault (f => f.Filter == fcd.Filter);

				string finalFileName = fcd.Filename;

				string extension = System.IO.Path.GetExtension (fcd.Filename);
				if (string.IsNullOrEmpty(extension))
					finalFileName += "." + format.Extensions.First ();

				PintaCore.Palette.CurrentPalette.Save(finalFileName, format.Saver);
			}

			fcd.Destroy ();
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
			                                                             Catalog.GetString ("Invert Selection"));
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
