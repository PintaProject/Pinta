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
using Gdk;
using Gtk;
using Mono.Unix;
using System.IO;

namespace Pinta.Core
{
	public class LayerActions
	{
		public Gtk.Action AddNewLayer { get; private set; }
		public Gtk.Action DeleteLayer { get; private set; }
		public Gtk.Action DuplicateLayer { get; private set; }
		public Gtk.Action MergeLayerDown { get; private set; }
		public Gtk.Action ImportFromFile { get; private set; }
		public Gtk.Action FlipHorizontal { get; private set; }
		public Gtk.Action FlipVertical { get; private set; }
		public Gtk.Action RotateZoom { get; private set; }
		public Gtk.Action MoveLayerUp { get; private set; }
		public Gtk.Action MoveLayerDown { get; private set; }
		public Gtk.Action Properties { get; private set; }
		
		public LayerActions ()
		{
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add ("Menu.Layers.AddNewLayer.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Layers.AddNewLayer.png")));
			fact.Add ("Menu.Layers.DeleteLayer.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Layers.DeleteLayer.png")));
			fact.Add ("Menu.Layers.DuplicateLayer.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Layers.DuplicateLayer.png")));
			fact.Add ("Menu.Layers.MergeLayerDown.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Layers.MergeLayerDown.png")));
			fact.Add ("Menu.Layers.MoveLayerDown.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Layers.MoveLayerDown.png")));
			fact.Add ("Menu.Layers.MoveLayerUp.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Layers.MoveLayerUp.png")));
			fact.Add ("Menu.Layers.FlipHorizontal.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Layers.FlipHorizontal.png")));
			fact.Add ("Menu.Layers.FlipVertical.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Layers.FlipVertical.png")));
			fact.Add ("Menu.Layers.ImportFromFile.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Layers.ImportFromFile.png")));
			fact.Add ("Menu.Layers.LayerProperties.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Layers.LayerProperties.png")));
			fact.Add ("Menu.Layers.RotateZoom.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Layers.RotateZoom.png")));
			fact.AddDefault ();
			
			AddNewLayer = new Gtk.Action ("AddNewLayer", Catalog.GetString ("Add New Layer"), null, "Menu.Layers.AddNewLayer.png");
			DeleteLayer = new Gtk.Action ("DeleteLayer", Catalog.GetString ("Delete Layer"), null, "Menu.Layers.DeleteLayer.png");
			DuplicateLayer = new Gtk.Action ("DuplicateLayer", Catalog.GetString ("Duplicate Layer"), null, "Menu.Layers.DuplicateLayer.png");
			MergeLayerDown = new Gtk.Action ("MergeLayerDown", Catalog.GetString ("Merge Layer Down"), null, "Menu.Layers.MergeLayerDown.png");
			ImportFromFile = new Gtk.Action ("ImportFromFile", Catalog.GetString ("Import from File..."), null, "Menu.Layers.ImportFromFile.png");
			FlipHorizontal = new Gtk.Action ("FlipHorizontal", Catalog.GetString ("Flip Horizontal"), null, "Menu.Layers.FlipHorizontal.png");
			FlipVertical = new Gtk.Action ("FlipVertical", Catalog.GetString ("Flip Vertical"), null, "Menu.Layers.FlipVertical.png");
			RotateZoom = new Gtk.Action ("RotateZoom", Catalog.GetString ("Rotate / Zoom Layer..."), null, "Menu.Layers.RotateZoom.png");
			MoveLayerUp = new Gtk.Action ("MoveLayerUp", Catalog.GetString ("Move Layer Up"), null, "Menu.Layers.MoveLayerUp.png");
			MoveLayerDown = new Gtk.Action ("MoveLayerDown", Catalog.GetString ("Move Layer Down"), null, "Menu.Layers.MoveLayerDown.png");
			Properties = new Gtk.Action ("Properties", Catalog.GetString ("Layer Properties..."), null, "Menu.Layers.LayerProperties.png");

			RotateZoom.Sensitive = false;
		}

		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
			menu.Append (AddNewLayer.CreateAcceleratedMenuItem (Gdk.Key.N, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.Append (DeleteLayer.CreateAcceleratedMenuItem (Gdk.Key.Delete, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.Append (DuplicateLayer.CreateAcceleratedMenuItem (Gdk.Key.D, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.Append (MergeLayerDown.CreateAcceleratedMenuItem (Gdk.Key.M, Gdk.ModifierType.ControlMask));
			menu.Append (ImportFromFile.CreateMenuItem ());
			menu.AppendSeparator ();
			menu.Append (FlipHorizontal.CreateMenuItem ());
			menu.Append (FlipVertical.CreateMenuItem ());
			menu.Append (RotateZoom.CreateMenuItem ());
			menu.AppendSeparator ();
			menu.Append (Properties.CreateAcceleratedMenuItem (Gdk.Key.F4, Gdk.ModifierType.None));
		}

		public void CreateLayerWindowToolBar (Gtk.Toolbar toolbar)
		{
			toolbar.AppendItem (AddNewLayer.CreateToolBarItem ());
			toolbar.AppendItem (DeleteLayer.CreateToolBarItem ());
			toolbar.AppendItem (DuplicateLayer.CreateToolBarItem ());
			toolbar.AppendItem (MergeLayerDown.CreateToolBarItem ());
			toolbar.AppendItem (MoveLayerUp.CreateToolBarItem ());
			toolbar.AppendItem (MoveLayerDown.CreateToolBarItem ());
			toolbar.AppendItem (Properties.CreateToolBarItem ());
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
			
			PintaCore.Layers.LayerAdded += EnableOrDisableLayerActions;
			PintaCore.Layers.LayerRemoved += EnableOrDisableLayerActions;
			PintaCore.Layers.SelectedLayerChanged += EnableOrDisableLayerActions;
			
			EnableOrDisableLayerActions (null, EventArgs.Empty);
		}
		#endregion

		#region Action Handlers
		private void EnableOrDisableLayerActions (object sender, EventArgs e)
		{
			if (PintaCore.Workspace.HasOpenDocuments && PintaCore.Workspace.ActiveDocument.UserLayers.Count > 1) {
				PintaCore.Actions.Layers.DeleteLayer.Sensitive = true;
				PintaCore.Actions.Image.Flatten.Sensitive = true;
			} else {
				PintaCore.Actions.Layers.DeleteLayer.Sensitive = false;
				PintaCore.Actions.Image.Flatten.Sensitive = false;
			}

			if (PintaCore.Workspace.HasOpenDocuments && PintaCore.Workspace.ActiveDocument.CurrentUserLayerIndex > 0) {
				PintaCore.Actions.Layers.MergeLayerDown.Sensitive = true;
				PintaCore.Actions.Layers.MoveLayerDown.Sensitive = true;
			} else {
				PintaCore.Actions.Layers.MergeLayerDown.Sensitive = false;
				PintaCore.Actions.Layers.MoveLayerDown.Sensitive = false;
			}

			if (PintaCore.Workspace.HasOpenDocuments && PintaCore.Workspace.ActiveDocument.CurrentUserLayerIndex < PintaCore.Workspace.ActiveDocument.UserLayers.Count - 1)
				PintaCore.Actions.Layers.MoveLayerUp.Sensitive = true;
			else
				PintaCore.Actions.Layers.MoveLayerUp.Sensitive = false;
		}

		private void HandlePintaCoreActionsLayersImportFromFileActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			var fcd = new Gtk.FileChooserDialog (Catalog.GetString ("Open Image File"),PintaCore.Chrome.MainWindow,
			                                     FileChooserAction.Open, Stock.Cancel, ResponseType.Cancel,
			                                     Stock.Open, ResponseType.Ok);

            		fcd.SetCurrentFolder (PintaCore.System.GetDialogDirectory ());
			fcd.AlternativeButtonOrder = new int[] { (int) ResponseType.Ok, (int) ResponseType.Cancel };

			fcd.AddImagePreview ();

			int response = fcd.Run ();
			
			if (response == (int)Gtk.ResponseType.Ok) {
				
				string file = fcd.Filename;
				PintaCore.System.LastDialogDirectory = fcd.CurrentFolder;
				
				// Open the image and add it to the layers
				UserLayer layer = doc.AddNewLayer(System.IO.Path.GetFileName(file));

				using (var fs = new FileStream (file, FileMode.Open))
					using (Pixbuf bg = new Pixbuf (fs))
						using (Cairo.Context g = new Cairo.Context (layer.Surface)) {
							CairoHelper.SetSourcePixbuf (g, bg, 0, 0);
							g.Paint ();
						}

				doc.SetCurrentUserLayer (layer);

				AddLayerHistoryItem hist = new AddLayerHistoryItem ("Menu.Layers.ImportFromFile.png", Catalog.GetString ("Import From File"), doc.UserLayers.IndexOf (layer));
				doc.History.PushNewItem (hist);

				doc.Workspace.Invalidate ();
			}
			
			fcd.Destroy ();
		}

		private void HandlePintaCoreActionsLayersFlipVerticalActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			doc.CurrentUserLayer.FlipVertical ();
			doc.Workspace.Invalidate ();
			doc.History.PushNewItem (new InvertHistoryItem (InvertType.FlipLayerVertical, doc.CurrentUserLayerIndex));
		}

		private void HandlePintaCoreActionsLayersFlipHorizontalActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			doc.CurrentUserLayer.FlipHorizontal ();
			doc.Workspace.Invalidate ();
			doc.History.PushNewItem (new InvertHistoryItem (InvertType.FlipLayerHorizontal, doc.CurrentUserLayerIndex));
		}

		private void HandlePintaCoreActionsLayersMoveLayerUpActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			SwapLayersHistoryItem hist = new SwapLayersHistoryItem ("Menu.Layers.MoveLayerUp.png", Catalog.GetString ("Move Layer Up"), doc.CurrentUserLayerIndex, doc.CurrentUserLayerIndex + 1);

			doc.MoveCurrentLayerUp ();
			doc.History.PushNewItem (hist);
		}

		private void HandlePintaCoreActionsLayersMoveLayerDownActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			SwapLayersHistoryItem hist = new SwapLayersHistoryItem ("Menu.Layers.MoveLayerDown.png", Catalog.GetString ("Move Layer Down"), doc.CurrentUserLayerIndex, doc.CurrentUserLayerIndex - 1);

			doc.MoveCurrentLayerDown ();
			doc.History.PushNewItem (hist);
		}

		private void HandlePintaCoreActionsLayersMergeLayerDownActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			int bottomLayerIndex = doc.CurrentUserLayerIndex - 1;
			var oldBottomSurface = doc.UserLayers[bottomLayerIndex].Surface.Clone ();

			CompoundHistoryItem hist = new CompoundHistoryItem ("Menu.Layers.MergeLayerDown.png", Catalog.GetString ("Merge Layer Down"));
			DeleteLayerHistoryItem h1 = new DeleteLayerHistoryItem (string.Empty, string.Empty, doc.CurrentUserLayer, doc.CurrentUserLayerIndex);

			doc.MergeCurrentLayerDown ();

			SimpleHistoryItem h2 = new SimpleHistoryItem (string.Empty, string.Empty, oldBottomSurface, bottomLayerIndex);
			hist.Push (h1);
			hist.Push (h2);

			doc.History.PushNewItem (hist);
		}

		private void HandlePintaCoreActionsLayersDuplicateLayerActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			UserLayer l = doc.DuplicateCurrentLayer();
			
			// Make new layer the current layer
			doc.SetCurrentUserLayer (l);

			AddLayerHistoryItem hist = new AddLayerHistoryItem ("Menu.Layers.DuplicateLayer.png", Catalog.GetString ("Duplicate Layer"), doc.UserLayers.IndexOf (l));
			doc.History.PushNewItem (hist);
		}

		private void HandlePintaCoreActionsLayersDeleteLayerActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			DeleteLayerHistoryItem hist = new DeleteLayerHistoryItem ("Menu.Layers.DeleteLayer.png", Catalog.GetString ("Delete Layer"), doc.CurrentUserLayer, doc.CurrentUserLayerIndex);

			doc.DeleteLayer (doc.CurrentUserLayerIndex, false);

			doc.History.PushNewItem (hist);
		}

		private void HandlePintaCoreActionsLayersAddNewLayerActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			UserLayer l = doc.AddNewLayer(string.Empty);

			// Make new layer the current layer
			doc.SetCurrentUserLayer (l);

			AddLayerHistoryItem hist = new AddLayerHistoryItem ("Menu.Layers.AddNewLayer.png", Catalog.GetString ("Add New Layer"), doc.UserLayers.IndexOf (l));
			doc.History.PushNewItem (hist);
		}
		#endregion
	}
}
