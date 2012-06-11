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
using Gtk;
using Cairo;
using Mono.Unix;

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
		public Gtk.Action AddinManager { get; private set; }
		
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
			EraseSelection = new Gtk.Action ("EraseSelection", Catalog.GetString ("Delete Selection"), null, "Menu.Edit.EraseSelection.png");
			FillSelection = new Gtk.Action ("FillSelection", Catalog.GetString ("Fill Selection"), null, "Menu.Edit.FillSelection.png");
			InvertSelection = new Gtk.Action ("InvertSelection", Catalog.GetString ("Invert Selection"), null, "Menu.Edit.InvertSelection.png");
			SelectAll = new Gtk.Action ("SelectAll", Catalog.GetString ("Select All"), null, Stock.SelectAll);
			Deselect = new Gtk.Action ("Deselect", Catalog.GetString ("Deselect All"), null, "Menu.Edit.Deselect.png");
			
			LoadPalette = new Gtk.Action ("LoadPalette", Catalog.GetString ("Open..."), null, Stock.Open);
			SavePalette = new Gtk.Action ("SavePalette", Catalog.GetString ("Save As..."), null, Stock.Save);
			ResetPalette = new Gtk.Action ("ResetPalette", Catalog.GetString ("Reset to Default"), null, Stock.RevertToSaved);
			ResizePalette = new Gtk.Action ("ResizePalette", Catalog.GetString ("Set Number of Colors"), null, "Menu.Image.Resize.png");

			AddinManager = new Gtk.Action ("AddinManager", Catalog.GetString ("Add-in Manager"), null, "Menu.Edit.Addins.png");
			
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
			//menu.Append (InvertSelection.CreateAcceleratedMenuItem (Gdk.Key.I, Gdk.ModifierType.ControlMask));
			
			menu.AppendSeparator ();
			Gtk.Action menu_action = new Gtk.Action ("Palette", Mono.Unix.Catalog.GetString ("Palette"), null, null);
			Menu palette_menu = (Menu) menu.AppendItem (menu_action.CreateSubMenuItem ()).Submenu;
			palette_menu.Append (LoadPalette.CreateMenuItem ());
			palette_menu.Append (SavePalette.CreateMenuItem ());
			palette_menu.Append (ResetPalette.CreateMenuItem ());
			palette_menu.Append (ResizePalette.CreateMenuItem ());

			menu.AppendSeparator ();
			menu.Append (AddinManager.CreateMenuItem ());
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

			PintaCore.Workspace.ActiveDocumentChanged += WorkspaceActiveDocumentChanged;
		}
		#endregion

		#region Action Handlers
		private void HandlePintaCoreActionsEditFillSelectionActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			Cairo.ImageSurface old = doc.CurrentLayer.Surface.Clone ();

			using (var g = new Cairo.Context (doc.CurrentLayer.Surface)) {
				g.AppendPath(doc.Selection.SelectionPath);
				g.FillRule = FillRule.EvenOdd;

				g.Color = PintaCore.Palette.PrimaryColor;
				g.Fill ();
			}

			doc.Workspace.Invalidate ();
			doc.History.PushNewItem (new SimpleHistoryItem ("Menu.Edit.FillSelection.png", Catalog.GetString ("Fill Selection"), old, doc.CurrentLayerIndex));
		}

		private void HandlePintaCoreActionsEditSelectAllActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			SelectionHistoryItem hist = new SelectionHistoryItem (Stock.SelectAll, Catalog.GetString ("Select All"));
			hist.TakeSnapshot ();

			doc.ResetSelectionPath ();
			doc.ShowSelection = true;

			doc.History.PushNewItem (hist);
			doc.Workspace.Invalidate ();
		}

		private void HandlePintaCoreActionsEditEraseSelectionActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			Cairo.ImageSurface old = doc.CurrentLayer.Surface.Clone ();

			using (var g = new Cairo.Context (doc.CurrentLayer.Surface)) {
				g.AppendPath(doc.Selection.SelectionPath);
				g.FillRule = FillRule.EvenOdd;

				g.Operator = Cairo.Operator.Clear;
				g.Fill ();
			}

			doc.Workspace.Invalidate ();

			if (sender is string && (sender as string) == "Cut")
				doc.History.PushNewItem (new SimpleHistoryItem (Stock.Cut, Catalog.GetString ("Cut"), old, doc.CurrentLayerIndex));
			else
				doc.History.PushNewItem (new SimpleHistoryItem ("Menu.Edit.EraseSelection.png", Catalog.GetString ("Erase Selection"), old, doc.CurrentLayerIndex));
		}

		private void HandlePintaCoreActionsEditDeselectActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			SelectionHistoryItem hist = new SelectionHistoryItem ("Menu.Edit.Deselect.png", Catalog.GetString ("Deselect"));
			hist.TakeSnapshot ();

			doc.ResetSelectionPath ();

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

			ImageSurface src = doc.GetClippedLayer (doc.CurrentLayerIndex);

			Gdk.Rectangle rect = doc.GetSelectedBounds (true);
			
			ImageSurface dest = new ImageSurface (Format.Argb32, rect.Width, rect.Height);

			using (Context g = new Context (dest)) {
				g.SetSourceSurface (src, -rect.X, -rect.Y);
				g.Paint ();
			}
			
			cb.Image = dest.ToPixbuf ();

			(src as IDisposable).Dispose ();
			(dest as IDisposable).Dispose ();
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
			Document doc = PintaCore.Workspace.ActiveDocument;
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
		}

		private void HandlerPintaCoreActionsEditRedoActivated (object sender, EventArgs e)
		{
			if (PintaCore.Tools.CurrentTool.TryHandleRedo ())
				return;
			Document doc = PintaCore.Workspace.ActiveDocument;
			doc.History.Redo ();
		}

		private void HandlerPintaCoreActionsEditLoadPaletteActivated (object sender, EventArgs e)
		{
			var fcd = new Gtk.FileChooserDialog (Catalog.GetString ("Open Palette File"), PintaCore.Chrome.MainWindow,
													FileChooserAction.Open, Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
													Gtk.Stock.Open, Gtk.ResponseType.Ok);

			FileFilter ff = new FileFilter ();
			ff.AddPattern ("*.txt");
			ff.AddPattern ("*.gpl");
			ff.Name = Catalog.GetString ("Palette files (*.txt, *.gpl)");
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
				try {
					lastPaletteDir = fcd.CurrentFolder;
					PintaCore.Palette.CurrentPalette.Load (fcd.Filename);
				} catch {
					MessageDialog md = new MessageDialog (PintaCore.Chrome.MainWindow, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, Catalog.GetString ("Could not open palette file: {0}.\nPlease verify that you are trying to open a valid GIMP or Paint.NET palette."), fcd.Filename);
					md.Title = Catalog.GetString ("Error");
					
					md.Run ();
					md.Destroy ();
				}
			}

			fcd.Destroy ();
		}

		private void HandlerPintaCoreActionsEditSavePaletteActivated (object sender, EventArgs e)
		{
			var fcd = new Gtk.FileChooserDialog (Catalog.GetString ("Save Palette File"), PintaCore.Chrome.MainWindow,
													FileChooserAction.Save, Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
													Gtk.Stock.Save, Gtk.ResponseType.Ok);

			FileFilter ffPDN = new FileFilter ();
			ffPDN.AddPattern ("*.txt");
			ffPDN.Name = Catalog.GetString ("Paint.NET palette (*.txt)");
			fcd.AddFilter (ffPDN);
			
			FileFilter ffGIMP = new FileFilter ();
			ffGIMP.AddPattern ("*.gpl");
			ffGIMP.Name = Catalog.GetString ("GIMP palette (*.gpl)");
			fcd.AddFilter (ffGIMP);
			
			fcd.AlternativeButtonOrder = new int[] { (int) ResponseType.Ok, (int) ResponseType.Cancel };

			if (lastPaletteDir != null)
				fcd.SetCurrentFolder (lastPaletteDir);
			
			int response = fcd.Run ();
		
			if (response == (int) Gtk.ResponseType.Ok) {
				Palette.FileFormat format = (fcd.Filter == ffPDN) ? Palette.FileFormat.PDN : Palette.FileFormat.GIMP;
				PintaCore.Palette.CurrentPalette.Save (fcd.Filename, format);
			}

			fcd.Destroy ();
		}

		private void HandlerPintaCoreActionsEditResetPaletteActivated (object sender, EventArgs e)
		{
			PintaCore.Palette.CurrentPalette.LoadDefault ();
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
