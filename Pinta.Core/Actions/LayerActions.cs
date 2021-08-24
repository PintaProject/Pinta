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
using System.IO;

namespace Pinta.Core
{
	public class LayerActions
	{
		public Command AddNewLayer { get; private set; }
		public Command DeleteLayer { get; private set; }
		public Command DuplicateLayer { get; private set; }
		public Command MergeLayerDown { get; private set; }
		public Command ImportFromFile { get; private set; }
		public Command FlipHorizontal { get; private set; }
		public Command FlipVertical { get; private set; }
		public Command RotateZoom { get; private set; }
		public Command MoveLayerUp { get; private set; }
		public Command MoveLayerDown { get; private set; }
		public Command Properties { get; private set; }
		
		public LayerActions ()
		{
			AddNewLayer = new Command ("addnewlayer", Translations.GetString ("Add New Layer"), null, Resources.Icons.LayerNew);
			DeleteLayer = new Command ("deletelayer", Translations.GetString ("Delete Layer"), null, Resources.Icons.LayerDelete);
			DuplicateLayer = new Command ("duplicatelayer", Translations.GetString ("Duplicate Layer"), null, Resources.Icons.LayerDuplicate);
			MergeLayerDown = new Command ("mergelayerdown", Translations.GetString ("Merge Layer Down"), null, Resources.Icons.LayerMergeDown);
			ImportFromFile = new Command ("importfromfile", Translations.GetString ("Import from File..."), null, Resources.Icons.LayerImport);
			FlipHorizontal = new Command ("fliphorizontal", Translations.GetString ("Flip Horizontal"), null, Resources.Icons.LayerFlipHorizontal);
            FlipVertical = new Command ("flipvertical", Translations.GetString ("Flip Vertical"), null, Resources.Icons.LayerFlipVertical);
            RotateZoom = new Command ("RotateZoom", Translations.GetString ("Rotate / Zoom Layer..."), null, Resources.Icons.LayerRotateZoom);
            MoveLayerUp = new Command ("movelayerup", Translations.GetString ("Move Layer Up"), null, Resources.Icons.LayerMoveUp);
			MoveLayerDown = new Command ("movelayerdown", Translations.GetString ("Move Layer Down"), null, Resources.Icons.LayerMoveDown);
			Properties = new Command ("properties", Translations.GetString ("Layer Properties..."), null, Resources.Icons.LayerProperties);
		}

		#region Initialization
		public void RegisterActions(Gtk.Application app, GLib.Menu menu)
		{
			app.AddAccelAction(AddNewLayer, "<Primary><Shift>N");
			menu.AppendItem(AddNewLayer.CreateMenuItem());

			app.AddAccelAction(DeleteLayer, "<Primary><Shift>Delete");
			menu.AppendItem(DeleteLayer.CreateMenuItem());

			app.AddAccelAction(DuplicateLayer, "<Primary><Shift>D");
			menu.AppendItem(DuplicateLayer.CreateMenuItem());

			app.AddAccelAction(MergeLayerDown, "<Primary>M");
			menu.AppendItem(MergeLayerDown.CreateMenuItem());

			app.AddAction(ImportFromFile);
			menu.AppendItem(ImportFromFile.CreateMenuItem());

			var flip_section = new GLib.Menu();
			menu.AppendSection(null, flip_section);

			app.AddAction(FlipHorizontal);
			flip_section.AppendItem(FlipHorizontal.CreateMenuItem());

			app.AddAction(FlipVertical);
			flip_section.AppendItem(FlipVertical.CreateMenuItem());

			app.AddAction(RotateZoom);
			flip_section.AppendItem(RotateZoom.CreateMenuItem());

			var prop_section = new GLib.Menu();
			menu.AppendSection(null, prop_section);

			app.AddAccelAction(Properties, "F4");
			prop_section.AppendItem(Properties.CreateMenuItem());

			app.AddAction (MoveLayerDown);
			app.AddAction (MoveLayerUp);
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
		private void EnableOrDisableLayerActions (object? sender, EventArgs e)
		{
			if (PintaCore.Workspace.HasOpenDocuments && PintaCore.Workspace.ActiveDocument.Layers.UserLayers.Count > 1) {
				PintaCore.Actions.Layers.DeleteLayer.Sensitive = true;
				PintaCore.Actions.Image.Flatten.Sensitive = true;
			} else {
				PintaCore.Actions.Layers.DeleteLayer.Sensitive = false;
				PintaCore.Actions.Image.Flatten.Sensitive = false;
			}

			if (PintaCore.Workspace.HasOpenDocuments && PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayerIndex > 0) {
				PintaCore.Actions.Layers.MergeLayerDown.Sensitive = true;
				PintaCore.Actions.Layers.MoveLayerDown.Sensitive = true;
			} else {
				PintaCore.Actions.Layers.MergeLayerDown.Sensitive = false;
				PintaCore.Actions.Layers.MoveLayerDown.Sensitive = false;
			}

			if (PintaCore.Workspace.HasOpenDocuments && PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayerIndex < PintaCore.Workspace.ActiveDocument.Layers.UserLayers.Count - 1)
				PintaCore.Actions.Layers.MoveLayerUp.Sensitive = true;
			else
				PintaCore.Actions.Layers.MoveLayerUp.Sensitive = false;
		}

		private void HandlePintaCoreActionsLayersImportFromFileActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			using var fcd = new FileChooserNative (
				Translations.GetString ("Open Image File"),
				PintaCore.Chrome.MainWindow,
				FileChooserAction.Open,
				Translations.GetString ("Open"),
				Translations.GetString ("Cancel"));

			fcd.SetCurrentFolder (PintaCore.System.GetDialogDirectory ());

			// Add image files filter
			var ff = new FileFilter ();
			foreach (var format in PintaCore.System.ImageFormats.Formats) {
				if (!format.IsWriteOnly ()) {
					foreach (var ext in format.Extensions)
						ff.AddPattern (string.Format ("*.{0}", ext));
				}
			}

			ff.Name = Translations.GetString ("Image files");
			fcd.AddFilter (ff);

			var response = (ResponseType) fcd.Run ();
			if (response == ResponseType.Accept) {

				string file = fcd.Filename;

				string? directory = Path.GetDirectoryName (file);
				if (directory is not null)
					PintaCore.System.LastDialogDirectory = directory;

				// Open the image and add it to the layers
				UserLayer layer = doc.Layers.AddNewLayer (System.IO.Path.GetFileName (file));

				using (var fs = new FileStream (file, FileMode.Open))
				using (var bg = new Pixbuf (fs))
				using (var g = new Cairo.Context (layer.Surface)) {
					Gdk.CairoHelper.SetSourcePixbuf (g, bg, 0, 0);
					g.Paint ();
				}

				doc.Layers.SetCurrentUserLayer (layer);

				AddLayerHistoryItem hist = new AddLayerHistoryItem (Resources.Icons.LayerImport, Translations.GetString ("Import From File"), doc.Layers.IndexOf (layer));
				doc.History.PushNewItem (hist);

				doc.Workspace.Invalidate ();
			}

		}

		private void HandlePintaCoreActionsLayersFlipVerticalActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			doc.Layers.CurrentUserLayer.FlipVertical ();
			doc.Workspace.Invalidate ();
			doc.History.PushNewItem (new InvertHistoryItem (InvertType.FlipLayerVertical, doc.Layers.CurrentUserLayerIndex));
		}

		private void HandlePintaCoreActionsLayersFlipHorizontalActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			doc.Layers.CurrentUserLayer.FlipHorizontal ();
			doc.Workspace.Invalidate ();
			doc.History.PushNewItem (new InvertHistoryItem (InvertType.FlipLayerHorizontal, doc.Layers.CurrentUserLayerIndex));
		}

		private void HandlePintaCoreActionsLayersMoveLayerUpActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			SwapLayersHistoryItem hist = new SwapLayersHistoryItem (Resources.Icons.LayerMoveUp, Translations.GetString ("Move Layer Up"), doc.Layers.CurrentUserLayerIndex, doc.Layers.CurrentUserLayerIndex + 1);

			doc.Layers.MoveCurrentLayerUp ();
			doc.History.PushNewItem (hist);
		}

		private void HandlePintaCoreActionsLayersMoveLayerDownActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			SwapLayersHistoryItem hist = new SwapLayersHistoryItem (Resources.Icons.LayerMoveDown, Translations.GetString ("Move Layer Down"), doc.Layers.CurrentUserLayerIndex, doc.Layers.CurrentUserLayerIndex - 1);

			doc.Layers.MoveCurrentLayerDown ();
			doc.History.PushNewItem (hist);
		}

		private void HandlePintaCoreActionsLayersMergeLayerDownActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			int bottomLayerIndex = doc.Layers.CurrentUserLayerIndex - 1;
			var oldBottomSurface = doc.Layers.UserLayers[bottomLayerIndex].Surface.Clone ();

			CompoundHistoryItem hist = new CompoundHistoryItem (Resources.Icons.LayerMergeDown, Translations.GetString ("Merge Layer Down"));
			DeleteLayerHistoryItem h1 = new DeleteLayerHistoryItem (string.Empty, string.Empty, doc.Layers.CurrentUserLayer, doc.Layers.CurrentUserLayerIndex);

			doc.Layers.MergeCurrentLayerDown ();

			SimpleHistoryItem h2 = new SimpleHistoryItem (string.Empty, string.Empty, oldBottomSurface, bottomLayerIndex);
			hist.Push (h1);
			hist.Push (h2);

			doc.History.PushNewItem (hist);
		}

		private void HandlePintaCoreActionsLayersDuplicateLayerActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			UserLayer l = doc.Layers.DuplicateCurrentLayer();
			
			// Make new layer the current layer
			doc.Layers.SetCurrentUserLayer (l);

			AddLayerHistoryItem hist = new AddLayerHistoryItem (Resources.Icons.LayerDuplicate, Translations.GetString ("Duplicate Layer"), doc.Layers.IndexOf (l));
			doc.History.PushNewItem (hist);
		}

		private void HandlePintaCoreActionsLayersDeleteLayerActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			DeleteLayerHistoryItem hist = new DeleteLayerHistoryItem (Resources.Icons.LayerDelete, Translations.GetString ("Delete Layer"), doc.Layers.CurrentUserLayer, doc.Layers.CurrentUserLayerIndex);

			doc.Layers.DeleteLayer (doc.Layers.CurrentUserLayerIndex, false);

			doc.History.PushNewItem (hist);
		}

		private void HandlePintaCoreActionsLayersAddNewLayerActivated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			PintaCore.Tools.Commit ();

			UserLayer l = doc.Layers.AddNewLayer (string.Empty);

			// Make new layer the current layer
			doc.Layers.SetCurrentUserLayer (l);

			AddLayerHistoryItem hist = new AddLayerHistoryItem (Resources.Icons.LayerNew, Translations.GetString ("Add New Layer"), doc.Layers.IndexOf (l));
			doc.History.PushNewItem (hist);
		}
		#endregion
	}
}
