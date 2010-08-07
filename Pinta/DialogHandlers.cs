// 
// FileActionHandler.cs
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
using Pinta.Core;
using Gtk;
using Mono.Unix;


namespace Pinta
{
	public class DialogHandlers
	{
		private MainWindow main_window;

		private const string markup = "<span weight=\"bold\" size=\"larger\">{0}</span>\n\n{1}";

		public DialogHandlers (MainWindow window)
		{
			main_window = window;
			
			PintaCore.Actions.File.New.Activated += HandlePintaCoreActionsFileNewActivated;
			PintaCore.Actions.File.NewScreenshot.Activated += HandlePintaCoreActionsFileNewScreenshotActivated;
			PintaCore.Actions.File.ModifyCompression += new EventHandler<ModifyCompressionEventArgs> (FileActions_ModifyCompression);
			PintaCore.Actions.File.Close.Activated += HandlePintaCoreActionsFileCloseActivated;
			
			PintaCore.Actions.Edit.PasteIntoNewLayer.Activated += HandlerPintaCoreActionsEditPasteIntoNewLayerActivated;
			PintaCore.Actions.Edit.PasteIntoNewImage.Activated += HandlerPintaCoreActionsEditPasteIntoNewImageActivated;
			PintaCore.Actions.Edit.ResizePalette.Activated += HandlePintaCoreActionsEditResizePaletteActivated;
			
			PintaCore.Actions.Image.Resize.Activated += HandlePintaCoreActionsImageResizeActivated;
			PintaCore.Actions.Image.CanvasSize.Activated += HandlePintaCoreActionsImageCanvasSizeActivated;
			
			PintaCore.Actions.Layers.Properties.Activated += HandlePintaCoreActionsLayersPropertiesActivated;

			PintaCore.Actions.View.Rulers.Toggled += HandlePintaCoreActionsViewRulersToggled;
			PintaCore.Actions.View.Pixels.Activated += HandlePixelsActivated;
			PintaCore.Actions.View.Inches.Activated += HandleInchesActivated;
			PintaCore.Actions.View.Centimeters.Activated += HandleCentimetersActivated;
			PintaCore.Actions.View.UnitComboBox.ComboBox.Changed += HandleUnitComboBoxComboBoxChanged;
		}

		#region Handlers
		private void HandlePintaCoreActionsFileNewActivated (object sender, EventArgs e)
		{
			NewImageDialog dialog = new NewImageDialog ();

			dialog.ParentWindow = main_window.GdkWindow;
			dialog.WindowPosition = Gtk.WindowPosition.CenterOnParent;

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok)
				PintaCore.Actions.File.NewFile (new Gdk.Size (dialog.NewImageWidth, dialog.NewImageHeight));

			dialog.Destroy ();
		}

		private void HandlePintaCoreActionsFileNewScreenshotActivated (object sender, EventArgs e)
		{
			SpinButtonEntryDialog dialog = new SpinButtonEntryDialog (Catalog.GetString ("Take Screenshot"),
					PintaCore.Chrome.MainWindow, Catalog.GetString ("Delay before taking a screenshot (seconds):"), 0, 300, 0);

			dialog.WindowPosition = Gtk.WindowPosition.CenterOnParent;

			if (dialog.Run () == (int) Gtk.ResponseType.Ok) {
				GLib.Timeout.Add ((uint) dialog.GetValue () * 1000, () => {
					PintaCore.Actions.File.NewFileWithScreenshot ();
					
					if (!PintaCore.Chrome.MainWindow.IsActive)
						PintaCore.Chrome.MainWindow.UrgencyHint = true;
					
					return false;
				});
			}

			dialog.Destroy ();
		}

		private void HandlePintaCoreActionsFileCloseActivated (object sender, EventArgs e)
		{
			if (PintaCore.Workspace.ActiveDocument.IsDirty) {
				var primary = Catalog.GetString ("Save the changes to image \"{0}\" before closing?");
				var secondary = Catalog.GetString ("If you don't save, all changes will be permanently lost.");
				var message = string.Format (markup, primary, secondary);

				var md = new MessageDialog (PintaCore.Chrome.MainWindow, DialogFlags.Modal,
							    MessageType.Question, ButtonsType.None, true,
							    message, System.IO.Path.GetFileName (PintaCore.Workspace.ActiveDocument.Filename));

				md.AddButton (Catalog.GetString ("Close without saving"), ResponseType.No);
				md.AddButton (Stock.Cancel, ResponseType.Cancel);
				md.AddButton (Stock.Save, ResponseType.Yes);

				// so that user won't accidentally overwrite
				md.DefaultResponse = ResponseType.Cancel;

				ResponseType response = (ResponseType)md.Run ();
				md.Destroy ();

				if (response == ResponseType.Yes) {
					PintaCore.Actions.File.Save.Activate ();
					
					// If the image is still dirty, the user
					// must have cancelled the Save dialog
					if (!PintaCore.Workspace.ActiveDocument.IsDirty)
						PintaCore.Workspace.CloseActiveDocument ();
				} else if (response == ResponseType.No) {
					PintaCore.Workspace.CloseActiveDocument ();
				}
			} else {
				PintaCore.Workspace.CloseActiveDocument ();
			}
		}

		private void HandlePintaCoreActionsEditResizePaletteActivated (object sender, EventArgs e)
		{
			SpinButtonEntryDialog dialog = new SpinButtonEntryDialog (Catalog.GetString ("Resize Palette"),
					PintaCore.Chrome.MainWindow, Catalog.GetString ("New palette size:"), 1, 96,
					PintaCore.Palette.CurrentPalette.Count);
			
			if (dialog.Run () == (int) ResponseType.Ok) {
				PintaCore.Palette.CurrentPalette.Resize (dialog.GetValue ());
			}
			
			dialog.Destroy ();
		}

		private void HandlerPintaCoreActionsEditPasteIntoNewLayerActivated (object sender, EventArgs e)
		{
			Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));

			if (cb.WaitIsImageAvailable ()) {
				PintaCore.Layers.FinishSelection ();

				Gdk.Pixbuf image = cb.WaitForImage ();

				Layer l = PintaCore.Layers.AddNewLayer (string.Empty);

				using (Cairo.Context g = new Cairo.Context (l.Surface))
					g.DrawPixbuf (image, new Cairo.Point (0, 0));

				// Make new layer the current layer
				PintaCore.Layers.SetCurrentLayer (l);

				PintaCore.Workspace.Invalidate ();

				AddLayerHistoryItem hist = new AddLayerHistoryItem (Stock.Paste, Catalog.GetString ("Paste Into New Layer"), PintaCore.Layers.IndexOf (l));
				PintaCore.History.PushNewItem (hist);
			} else {
				ClipboardEmptyError ();
			}
		}
		private void HandlerPintaCoreActionsEditPasteIntoNewImageActivated (object sender, EventArgs e)
		{
			Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));

			if (cb.WaitIsImageAvailable ()) {
				Gdk.Pixbuf image = cb.WaitForImage ();
				Gdk.Size size = new Gdk.Size (image.Width, image.Height);
				
				PintaCore.Actions.File.NewFile (size);
				PintaCore.Actions.Edit.Paste.Activate ();
			} else {
				ClipboardEmptyError ();
			}
		}

		private void HandlePintaCoreActionsImageResizeActivated (object sender, EventArgs e)
		{
			ResizeImageDialog dialog = new ResizeImageDialog ();

			dialog.ParentWindow = main_window.GdkWindow;
			dialog.WindowPosition = Gtk.WindowPosition.CenterOnParent;

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok)
				dialog.SaveChanges ();

			dialog.Destroy ();
		}
		
		private void HandlePintaCoreActionsImageCanvasSizeActivated (object sender, EventArgs e)
		{
			ResizeCanvasDialog dialog = new ResizeCanvasDialog ();

			dialog.ParentWindow = main_window.GdkWindow;
			dialog.WindowPosition = Gtk.WindowPosition.CenterOnParent;

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok)
				dialog.SaveChanges ();

			dialog.Destroy ();
		}
				
		private void HandlePintaCoreActionsLayersPropertiesActivated (object sender, EventArgs e)
		{
			var dialog = new LayerPropertiesDialog ();
			
			int response = dialog.Run ();		
			
			if (response == (int)Gtk.ResponseType.Ok
			    && dialog.AreLayerPropertiesUpdated) {
				
				var historyMessage = GetLayerPropertyUpdateMessage(
						dialog.InitialLayerProperties,
						dialog.UpdatedLayerProperties);				
				
				var historyItem = new UpdateLayerPropertiesHistoryItem(
					"Menu.Layers.LayerProperties.png",
					historyMessage,
					PintaCore.Layers.CurrentLayerIndex,
					dialog.InitialLayerProperties,
					dialog.UpdatedLayerProperties);
				
				PintaCore.Workspace.ActiveWorkspace.History.PushNewItem (historyItem);
				
				PintaCore.Workspace.ActiveWorkspace.Invalidate ();
				
			} else {
				
				var layer = PintaCore.Workspace.ActiveDocument.CurrentLayer;
				var initial = dialog.InitialLayerProperties;
				initial.SetProperties (layer);
				
				if (layer.Opacity != initial.Opacity)
					PintaCore.Workspace.ActiveWorkspace.Invalidate ();
			}
				
			dialog.Destroy ();
		}
		
		private string GetLayerPropertyUpdateMessage (
			LayerProperties initial,
			LayerProperties updated)
		{

			string ret = null;
			int count = 0;
			
			if (updated.Opacity != initial.Opacity) {
				ret = Catalog.GetString ("Layer Opacity");
				count++;
			}
				
			if (updated.Name != initial.Name) {
				ret = Catalog.GetString ("Rename Layer");
				count++;
			}
			
			if (updated.Hidden != initial.Hidden) {
				ret = (updated.Hidden) ? Catalog.GetString ("Hide Layer") : Catalog.GetString ("Show Layer");
				count++;
			}
			
			if (ret == null || count > 1)
				ret = Catalog.GetString ("Layer Properties");
			
			return ret;
		}

		internal void UpdateRulerVisibility ()
		{
			HandlePintaCoreActionsViewRulersToggled (PintaCore.Actions.View.Rulers, EventArgs.Empty);
		}

		private void HandlePintaCoreActionsViewRulersToggled (object sender, EventArgs e)
		{
			if (((ToggleAction)sender).Active)
				main_window.ShowRulers ();
			else
				main_window.HideRulers ();
		}

		private void HandleUnitComboBoxComboBoxChanged (object sender, EventArgs e)
		{
			switch (PintaCore.Actions.View.UnitComboBox.ComboBox.Active) {
				case 0://pixels
					main_window.ChangeRulersUnit (Gtk.MetricType.Pixels);
				break;
				case 1://inches
					main_window.ChangeRulersUnit (Gtk.MetricType.Inches);
				break;
				case 2://centimeters
					main_window.ChangeRulersUnit (Gtk.MetricType.Centimeters);
				break;

			}
		}

		private void HandleCentimetersActivated (object sender, EventArgs e)
		{
			main_window.ChangeRulersUnit (Gtk.MetricType.Centimeters);
		}

		private void HandleInchesActivated (object sender, EventArgs e)
		{
			main_window.ChangeRulersUnit (Gtk.MetricType.Inches);
		}

		private void HandlePixelsActivated (object sender, EventArgs e)
		{
			main_window.ChangeRulersUnit (Gtk.MetricType.Pixels);
		}

		private void FileActions_ModifyCompression (object sender, ModifyCompressionEventArgs e)
		{
			JpegCompressionDialog dlg = new JpegCompressionDialog (e.Quality);

			try {
				if (dlg.Run () == (int)Gtk.ResponseType.Ok)
					e.Quality = dlg.GetCompressionLevel ();
				else
					e.Cancel = true;
			} finally {
				dlg.Destroy ();
			}
		}
		#endregion

		#region Private Methods
		public void ClipboardEmptyError ()
		{
			var primary = Catalog.GetString ("Paste cancelled");
			var secondary = Catalog.GetString ("The clipboard does not contain an image");
			var markup = "<span weight=\"bold\" size=\"larger\">{0}</span>\n\n{1}\n";
			markup = string.Format (markup, primary, secondary);

			var md = new MessageDialog (PintaCore.Chrome.MainWindow, DialogFlags.Modal,
						    MessageType.Error, ButtonsType.None, true,
						    markup,
						    System.IO.Path.GetFileName (PintaCore.Workspace.Filename));

			md.AddButton (Stock.Ok, ResponseType.Yes);

			ResponseType response = (ResponseType)md.Run ();
			md.Destroy ();
		}
		#endregion
	}
}

