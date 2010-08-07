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
			fact.AddDefault ();
			
			Undo = new Gtk.Action ("Undo", Catalog.GetString ("Undo"), null, Stock.Undo);
			Redo = new Gtk.Action ("Redo", Catalog.GetString ("Redo"), null, Stock.Redo);
			Cut = new Gtk.Action ("Cut", Catalog.GetString ("Cut"), null, Stock.Cut);
			Copy = new Gtk.Action ("Copy", Catalog.GetString ("Copy"), null, Stock.Copy);
			Paste = new Gtk.Action ("Paste", Catalog.GetString ("Paste"), null, Stock.Paste);
			PasteIntoNewLayer = new Gtk.Action ("PasteIntoNewLayer", Catalog.GetString ("Paste Into New Layer"), null, Stock.Paste);
			PasteIntoNewImage = new Gtk.Action ("PasteIntoNewImage", Catalog.GetString ("Paste Into New Image"), null, Stock.Paste);
			EraseSelection = new Gtk.Action ("EraseSelection", Catalog.GetString ("Erase Selection"), null, "Menu.Edit.EraseSelection.png");
			FillSelection = new Gtk.Action ("FillSelection", Catalog.GetString ("Fill Selection"), null, "Menu.Edit.FillSelection.png");
			InvertSelection = new Gtk.Action ("InvertSelection", Catalog.GetString ("Invert Selection"), null, "Menu.Edit.InvertSelection.png");
			SelectAll = new Gtk.Action ("SelectAll", Catalog.GetString ("Select All"), null, Stock.SelectAll);
			Deselect = new Gtk.Action ("Deselect", Catalog.GetString ("Deselect"), null, "Menu.Edit.Deselect.png");
			
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
			menu.Append (Redo.CreateAcceleratedMenuItem (Gdk.Key.Y, Gdk.ModifierType.ControlMask));
			menu.AppendSeparator ();
			menu.Append (Cut.CreateAcceleratedMenuItem (Gdk.Key.X, Gdk.ModifierType.ControlMask));
			menu.Append (Copy.CreateAcceleratedMenuItem (Gdk.Key.C, Gdk.ModifierType.ControlMask));
			menu.Append (Paste.CreateAcceleratedMenuItem (Gdk.Key.V, Gdk.ModifierType.ControlMask));
			menu.Append (PasteIntoNewLayer.CreateAcceleratedMenuItem (Gdk.Key.V, Gdk.ModifierType.ShiftMask));
			menu.Append (PasteIntoNewImage.CreateAcceleratedMenuItem (Gdk.Key.V, Gdk.ModifierType.Mod1Mask));
			menu.AppendSeparator ();
			
			Gtk.Action menu_action = new Gtk.Action ("Palette", Mono.Unix.Catalog.GetString ("Palette"), null, null);
			Menu palette_menu = (Menu) menu.AppendItem (menu_action.CreateSubMenuItem ()).Submenu;
			palette_menu.Append (LoadPalette.CreateMenuItem ());
			palette_menu.Append (SavePalette.CreateMenuItem ());
			palette_menu.Append (ResetPalette.CreateMenuItem ());
			palette_menu.Append (ResizePalette.CreateMenuItem ());
			
			menu.AppendSeparator ();
			menu.Append (EraseSelection.CreateAcceleratedMenuItem (Gdk.Key.Delete, Gdk.ModifierType.None));
			menu.Append (FillSelection.CreateAcceleratedMenuItem (Gdk.Key.BackSpace, Gdk.ModifierType.None));
			//menu.Append (InvertSelection.CreateAcceleratedMenuItem (Gdk.Key.I, Gdk.ModifierType.ControlMask));
			menu.Append (SelectAll.CreateAcceleratedMenuItem (Gdk.Key.A, Gdk.ModifierType.ControlMask));
			menu.Append (Deselect.CreateAcceleratedMenuItem (Gdk.Key.D, Gdk.ModifierType.ControlMask));
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
			Paste.Activated += HandlerPintaCoreActionsEditPasteActivated;
			Copy.Activated += HandlerPintaCoreActionsEditCopyActivated;
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
			PintaCore.Layers.FinishSelection ();
			
			Cairo.ImageSurface old = PintaCore.Layers.CurrentLayer.Surface.Clone ();
			
			using (Cairo.Context g = new Cairo.Context (PintaCore.Layers.CurrentLayer.Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.FillRule = Cairo.FillRule.EvenOdd;
				g.Color = PintaCore.Palette.PrimaryColor;
				g.Fill ();
			}
			
			PintaCore.Workspace.Invalidate ();
			PintaCore.History.PushNewItem (new SimpleHistoryItem ("Menu.Edit.FillSelection.png", Catalog.GetString ("Fill Selection"), old, PintaCore.Layers.CurrentLayerIndex));
		}

		private void HandlePintaCoreActionsEditSelectAllActivated (object sender, EventArgs e)
		{
			PintaCore.Layers.FinishSelection ();

			SelectionHistoryItem hist = new SelectionHistoryItem (Stock.SelectAll, Catalog.GetString ("Select All"));
			hist.TakeSnapshot ();

			PintaCore.Layers.ResetSelectionPath ();
			PintaCore.Layers.ShowSelection = true;

			PintaCore.History.PushNewItem (hist);
			PintaCore.Workspace.Invalidate ();
		}

		private void HandlePintaCoreActionsEditEraseSelectionActivated (object sender, EventArgs e)
		{
			PintaCore.Layers.FinishSelection ();

			Cairo.ImageSurface old = PintaCore.Layers.CurrentLayer.Surface.Clone ();

			using (Cairo.Context g = new Cairo.Context (PintaCore.Layers.CurrentLayer.Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.FillRule = Cairo.FillRule.EvenOdd;
				g.Operator = Cairo.Operator.Clear;
				g.Fill ();
			}
			
			PintaCore.Workspace.Invalidate ();
			PintaCore.History.PushNewItem (new SimpleHistoryItem ("Menu.Edit.EraseSelection.png", Catalog.GetString ("Erase Selection"), old, PintaCore.Layers.CurrentLayerIndex));
		}

		private void HandlePintaCoreActionsEditDeselectActivated (object sender, EventArgs e)
		{
			PintaCore.Layers.FinishSelection ();

			SelectionHistoryItem hist = new SelectionHistoryItem ("Menu.Edit.Deselect.png", Catalog.GetString ("Deselect"));
			hist.TakeSnapshot ();
			
			PintaCore.Layers.ResetSelectionPath ();
			
			PintaCore.History.PushNewItem (hist);
			PintaCore.Workspace.Invalidate ();
		}

		private void HandlerPintaCoreActionsEditPasteActivated (object sender, EventArgs e)
		{
			PintaCore.Layers.FinishSelection ();

			Cairo.ImageSurface old = PintaCore.Layers.CurrentLayer.Surface.Clone ();

			Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			Gdk.Pixbuf image = cb.WaitForImage ();

			if (image == null)
				return;

			Path p;
			
			using (Cairo.Context g = new Cairo.Context (PintaCore.Layers.CurrentLayer.Surface)) {
				g.DrawPixbuf (image, new Cairo.Point (0, 0));
				p = g.CreateRectanglePath (new Rectangle (0, 0, image.Width, image.Height));
			}

			PintaCore.Layers.SelectionPath = p;
			PintaCore.Layers.ShowSelection = true;
			
			PintaCore.Workspace.Invalidate ();
			
			PintaCore.History.PushNewItem (new SimpleHistoryItem (Stock.Paste, Catalog.GetString ("Paste"), old, PintaCore.Layers.CurrentLayerIndex));
		}

		private void HandlerPintaCoreActionsEditCopyActivated (object sender, EventArgs e)
		{
			PintaCore.Layers.FinishSelection ();

			ImageSurface src = PintaCore.Layers.GetClippedLayer (PintaCore.Layers.CurrentLayerIndex);
			
			Gdk.Rectangle rect = PintaCore.Layers.SelectionPath.GetBounds ();
			
			ImageSurface dest = new ImageSurface (Format.Argb32, rect.Width, rect.Height);

			using (Context g = new Context (dest)) {
				g.SetSourceSurface (src, -rect.X, -rect.Y);
				g.Paint ();
			}
			
			Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			cb.Image = dest.ToPixbuf ();

			(src as IDisposable).Dispose ();
			(dest as IDisposable).Dispose ();
		}
		
		private void HandlerPintaCoreActionsEditCutActivated (object sender, EventArgs e)
		{
			PintaCore.Layers.FinishSelection ();
			
			// Copy selection
			HandlerPintaCoreActionsEditCopyActivated (sender, e);

			// Erase selection
			Cairo.ImageSurface old = PintaCore.Layers.CurrentLayer.Surface.Clone ();

			using (Cairo.Context g = new Cairo.Context (PintaCore.Layers.CurrentLayer.Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.FillRule = Cairo.FillRule.EvenOdd;
				g.Operator = Cairo.Operator.Clear;
				g.Fill ();
			}

			PintaCore.Workspace.Invalidate ();
			PintaCore.History.PushNewItem (new SimpleHistoryItem ("Menu.Edit.EraseSelection.png", Catalog.GetString ("Erase Selection"), old, PintaCore.Layers.CurrentLayerIndex));
		}

		private void HandlerPintaCoreActionsEditUndoActivated (object sender, EventArgs e)
		{
			PintaCore.History.Undo ();
		}

		private void HandlerPintaCoreActionsEditRedoActivated (object sender, EventArgs e)
		{
			PintaCore.History.Redo ();
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
