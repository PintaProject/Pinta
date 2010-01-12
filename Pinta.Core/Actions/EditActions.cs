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
		
		public EditActions ()
		{
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add ("Menu.Edit.Deselect.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Edit.Deselect.png")));
			fact.Add ("Menu.Edit.EraseSelection.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Edit.EraseSelection.png")));
			fact.Add ("Menu.Edit.FillSelection.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Edit.FillSelection.png")));
			fact.Add ("Menu.Edit.InvertSelection.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Edit.InvertSelection.png")));
			fact.Add ("Menu.Edit.SelectAll.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Edit.SelectAll.png")));
			fact.AddDefault ();
			
			Undo = new Gtk.Action ("Undo", Mono.Unix.Catalog.GetString ("Undo"), null, "gtk-undo");
			Redo = new Gtk.Action ("Redo", Mono.Unix.Catalog.GetString ("Redo"), null, "gtk-redo");
			Cut = new Gtk.Action ("Cut", Mono.Unix.Catalog.GetString ("Cut"), null, "gtk-cut");
			Copy = new Gtk.Action ("Copy", Mono.Unix.Catalog.GetString ("Copy"), null, "gtk-copy");
			Paste = new Gtk.Action ("Paste", Mono.Unix.Catalog.GetString ("Paste"), null, "gtk-paste");
			PasteIntoNewLayer = new Gtk.Action ("PasteIntoNewLayer", Mono.Unix.Catalog.GetString ("Paste Into New Layer"), null, "gtk-paste");
			PasteIntoNewImage = new Gtk.Action ("PasteIntoNewImage", Mono.Unix.Catalog.GetString ("Paste Into New Image"), null, "gtk-paste");
			EraseSelection = new Gtk.Action ("EraseSelection", Mono.Unix.Catalog.GetString ("Erase Selection"), null, "Menu.Edit.EraseSelection.png");
			FillSelection = new Gtk.Action ("FillSelection", Mono.Unix.Catalog.GetString ("Fill Selection"), null, "Menu.Edit.FillSelection.png");
			InvertSelection = new Gtk.Action ("InvertSelection", Mono.Unix.Catalog.GetString ("Invert Selection"), null, "Menu.Edit.InvertSelection.png");
			SelectAll = new Gtk.Action ("SelectAll", Mono.Unix.Catalog.GetString ("Select All"), null, "Menu.Edit.SelectAll.png");
			Deselect = new Gtk.Action ("Deselect", Mono.Unix.Catalog.GetString ("Deselect"), null, "Menu.Edit.Deselect.png");
			
			Undo.Sensitive = false;
			Redo.Sensitive = false;
			Cut.Sensitive = false;
			Copy.Sensitive = false;
			PasteIntoNewImage.Sensitive = false;
			InvertSelection.Sensitive = false;
		}

		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
			menu.Remove (menu.Children[1]);
			
			menu.Append (Undo.CreateAcceleratedMenuItem (Gdk.Key.Z, Gdk.ModifierType.ControlMask));
			menu.Append (Redo.CreateAcceleratedMenuItem (Gdk.Key.Y, Gdk.ModifierType.ControlMask));
			menu.AppendSeparator ();
			menu.Append (Cut.CreateAcceleratedMenuItem (Gdk.Key.X, Gdk.ModifierType.ControlMask));
			menu.Append (Copy.CreateAcceleratedMenuItem (Gdk.Key.C, Gdk.ModifierType.ControlMask));
			menu.Append (Paste.CreateAcceleratedMenuItem (Gdk.Key.V, Gdk.ModifierType.ControlMask));
			menu.Append (PasteIntoNewLayer.CreateAcceleratedMenuItem (Gdk.Key.V, Gdk.ModifierType.ShiftMask));
			menu.Append (PasteIntoNewImage.CreateAcceleratedMenuItem (Gdk.Key.V, Gdk.ModifierType.Mod1Mask));
			menu.AppendSeparator ();
			menu.Append (EraseSelection.CreateAcceleratedMenuItem (Gdk.Key.Delete, Gdk.ModifierType.None));
			menu.Append (FillSelection.CreateAcceleratedMenuItem (Gdk.Key.BackSpace, Gdk.ModifierType.None));
			menu.Append (InvertSelection.CreateAcceleratedMenuItem (Gdk.Key.I, Gdk.ModifierType.ControlMask));
			menu.Append (SelectAll.CreateAcceleratedMenuItem (Gdk.Key.A, Gdk.ModifierType.ControlMask));
			menu.Append (Deselect.CreateAcceleratedMenuItem (Gdk.Key.D, Gdk.ModifierType.ControlMask));
		}
	
		public void RegisterHandlers ()
		{
			Deselect.Activated += HandlePintaCoreActionsEditDeselectActivated;
			EraseSelection.Activated += HandlePintaCoreActionsEditEraseSelectionActivated;
			SelectAll.Activated += HandlePintaCoreActionsEditSelectAllActivated;
			FillSelection.Activated += HandlePintaCoreActionsEditFillSelectionActivated;
			PasteIntoNewLayer.Activated += HandlerPintaCoreActionsEditPasteIntoNewLayerActivated;
			Paste.Activated += HandlerPintaCoreActionsEditPasteActivated;
			Copy.Activated += HandlerPintaCoreActionsEditCopyActivated;
			Undo.Activated += HandlerPintaCoreActionsEditUndoActivated;
			Redo.Activated += HandlerPintaCoreActionsEditRedoActivated;
		}
		#endregion

		#region Action Handlers
		private void HandlePintaCoreActionsEditFillSelectionActivated (object sender, EventArgs e)
		{
			Cairo.ImageSurface old = PintaCore.Layers.CurrentLayer.Surface.Clone ();
			
			using (Cairo.Context g = new Cairo.Context (PintaCore.Layers.CurrentLayer.Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.Color = PintaCore.Palette.PrimaryColor;
				g.Fill ();
			}
			
			PintaCore.Chrome.DrawingArea.GdkWindow.Invalidate ();
			PintaCore.History.PushNewItem (new SimpleHistoryItem ("Menu.Edit.FillSelection.png", Mono.Unix.Catalog.GetString ("Fill Selection"), old, PintaCore.Layers.CurrentLayerIndex));
		}

		private void HandlePintaCoreActionsEditSelectAllActivated (object sender, EventArgs e)
		{
			PintaCore.Layers.ResetSelectionPath ();
			PintaCore.Layers.ShowSelection = true;
			
			PintaCore.Chrome.DrawingArea.GdkWindow.Invalidate ();
		}

		private void HandlePintaCoreActionsEditEraseSelectionActivated (object sender, EventArgs e)
		{
			Cairo.ImageSurface old = PintaCore.Layers.CurrentLayer.Surface.Clone ();

			using (Cairo.Context g = new Cairo.Context (PintaCore.Layers.CurrentLayer.Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.Operator = Cairo.Operator.Clear;
				g.Fill ();
			}
			
			PintaCore.Chrome.DrawingArea.GdkWindow.Invalidate ();
			PintaCore.History.PushNewItem (new SimpleHistoryItem ("Menu.Edit.EraseSelection.png", Mono.Unix.Catalog.GetString ("Erase Selection"), old, PintaCore.Layers.CurrentLayerIndex));
		}

		private void HandlePintaCoreActionsEditDeselectActivated (object sender, EventArgs e)
		{
			PintaCore.Layers.ResetSelectionPath ();
			
			PintaCore.Chrome.DrawingArea.GdkWindow.Invalidate ();
		}

		private void HandlerPintaCoreActionsEditPasteIntoNewLayerActivated (object sender, EventArgs e)
		{
			Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			Gdk.Pixbuf image = cb.WaitForImage ();

			if (image == null)
				return;

			Layer l = PintaCore.Layers.AddNewLayer (string.Empty);

			using (Cairo.Context g = new Cairo.Context (l.Surface))
				g.DrawPixbuf (image, new Cairo.Point (0, 0));

			PintaCore.Chrome.DrawingArea.GdkWindow.Invalidate ();
		}

		private void HandlerPintaCoreActionsEditPasteActivated (object sender, EventArgs e)
		{
			Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			Gdk.Pixbuf image = cb.WaitForImage ();

			if (image == null)
				return;

			using (Cairo.Context g = new Cairo.Context (PintaCore.Layers.CurrentLayer.Surface))
				g.DrawPixbuf (image, new Cairo.Point (0, 0));

			PintaCore.Chrome.DrawingArea.GdkWindow.Invalidate ();
		}

		private void HandlerPintaCoreActionsEditCopyActivated (object sender, EventArgs e)
		{
			//Cairo.ImageSurface surf = PintaCore.Layers.GetFlattenedImage ();
			
			//Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			//cb.Image = surf.ToPixbuf ();
			

			//if (image == null)
			//        return;

			//using (Cairo.Context g = new Cairo.Context (PintaCore.Layers.CurrentLayer.Surface))
			//        g.DrawPixbuf (image, new Cairo.Point (0, 0));

			//PintaCore.Chrome.DrawingArea.GdkWindow.Invalidate ();
		}

		private void HandlerPintaCoreActionsEditUndoActivated (object sender, EventArgs e)
		{
			PintaCore.History.Undo ();
		}

		private void HandlerPintaCoreActionsEditRedoActivated (object sender, EventArgs e)
		{
			PintaCore.History.Redo ();
		}
		#endregion
	}
}
