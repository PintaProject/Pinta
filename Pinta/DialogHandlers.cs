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
using System.Collections.Generic;
using System.IO;
using Gdk;


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
			PintaCore.Actions.File.Open.Activated += HandlePintaCoreActionsFileOpenActivated;
			(PintaCore.Actions.File.OpenRecent as RecentAction).ItemActivated += HandleOpenRecentItemActivated;
			PintaCore.Actions.File.Save.Activated += HandlePintaCoreActionsFileSaveActivated;
			PintaCore.Actions.File.SaveAs.Activated += HandlePintaCoreActionsFileSaveAsActivated;

			PintaCore.Actions.File.Close.Activated += HandlePintaCoreActionsFileCloseActivated;
			
			PintaCore.Actions.Edit.PasteIntoNewLayer.Activated += HandlerPintaCoreActionsEditPasteIntoNewLayerActivated;
			PintaCore.Actions.Edit.PasteIntoNewImage.Activated += HandlerPintaCoreActionsEditPasteIntoNewImageActivated;
			PintaCore.Actions.Edit.ResizePalette.Activated += HandlePintaCoreActionsEditResizePaletteActivated;
			
			PintaCore.Actions.Image.Resize.Activated += HandlePintaCoreActionsImageResizeActivated;
			PintaCore.Actions.Image.CanvasSize.Activated += HandlePintaCoreActionsImageCanvasSizeActivated;
			
			PintaCore.Actions.Layers.Properties.Activated += HandlePintaCoreActionsLayersPropertiesActivated;

			PintaCore.Actions.View.ToolBar.Toggled += HandlePintaCoreActionsViewToolbarToggled;
			PintaCore.Actions.View.Rulers.Toggled += HandlePintaCoreActionsViewRulersToggled;
			PintaCore.Actions.View.Pixels.Activated += HandlePixelsActivated;
			PintaCore.Actions.View.Inches.Activated += HandleInchesActivated;
			PintaCore.Actions.View.Centimeters.Activated += HandleCentimetersActivated;

			PintaCore.Actions.Window.CloseAll.Activated += HandleCloseAllActivated;
			PintaCore.Actions.Window.SaveAll.Activated += HandleSaveAllActivated;

			PintaCore.Actions.File.SaveDocument += Workspace_SaveDocument;

			InitializeFileActions ();
		}

		#region Handlers
		private void HandlePintaCoreActionsFileNewActivated (object sender, EventArgs e)
		{
			NewImageDialog dialog = new NewImageDialog ();

			dialog.NewImageWidth = PintaCore.Settings.GetSetting<int> ("new-image-width", 800);
			dialog.NewImageHeight = PintaCore.Settings.GetSetting<int> ("new-image-height", 600);

			dialog.ParentWindow = main_window.GdkWindow;
			dialog.WindowPosition = Gtk.WindowPosition.CenterOnParent;

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok) {
				PintaCore.Workspace.NewDocument (new Gdk.Size (dialog.NewImageWidth, dialog.NewImageHeight), false);

				PintaCore.Settings.PutSetting ("new-image-width", dialog.NewImageWidth);
				PintaCore.Settings.PutSetting ("new-image-height", dialog.NewImageHeight);
				PintaCore.Settings.SaveSettings ();
			}

			dialog.Destroy ();
		}

		private void HandlePintaCoreActionsFileNewScreenshotActivated (object sender, EventArgs e)
		{
			int delay = PintaCore.Settings.GetSetting<int> ("screenshot-delay", 0);

			SpinButtonEntryDialog dialog = new SpinButtonEntryDialog (Catalog.GetString ("Take Screenshot"),
					PintaCore.Chrome.MainWindow, Catalog.GetString ("Delay before taking a screenshot (seconds):"), 0, 300, delay);

			if (dialog.Run () == (int)Gtk.ResponseType.Ok) {
				delay = dialog.GetValue ();

				PintaCore.Settings.PutSetting ("screenshot-delay", delay);
				PintaCore.Settings.SaveSettings ();

				GLib.Timeout.Add ((uint)delay * 1000, () => {
					Screen screen = Screen.Default;
					Document doc = PintaCore.Workspace.NewDocument (new Size (screen.Width, screen.Height), false);

					using (Pixbuf pb = Pixbuf.FromDrawable (screen.RootWindow, screen.RootWindow.Colormap, 0, 0, 0, 0, screen.Width, screen.Height)) {
						using (Cairo.Context g = new Cairo.Context (doc.Layers[0].Surface)) {
							CairoHelper.SetSourcePixbuf (g, pb, 0, 0);
							g.Paint ();
						}
					}

					doc.IsDirty = true;

					if (!PintaCore.Chrome.MainWindow.IsActive) {
						PintaCore.Chrome.MainWindow.UrgencyHint = true;

						// Don't flash forever
						GLib.Timeout.Add (3 * 1000, () => PintaCore.Chrome.MainWindow.UrgencyHint = false);
					}
					
					return false;
				});
			}

			dialog.Destroy ();
		}

		private void HandlePintaCoreActionsFileSaveActivated (object sender, EventArgs e)
		{
			PintaCore.Workspace.ActiveDocument.Save ();
		}

		private void HandlePintaCoreActionsFileSaveAsActivated (object sender, EventArgs e)
		{
			SaveFileAs (PintaCore.Workspace.ActiveDocument);
		}

		private void HandlePintaCoreActionsFileCloseActivated (object sender, EventArgs e)
		{
			if (PintaCore.Workspace.ActiveDocument.IsDirty) {
				var primary = Catalog.GetString ("Save the changes to image \"{0}\" before closing?");
				var secondary = Catalog.GetString ("If you don't save, all changes will be permanently lost.");
				var message = string.Format (markup, primary, secondary);

				var md = new MessageDialog (PintaCore.Chrome.MainWindow, DialogFlags.Modal,
							    MessageType.Warning, ButtonsType.None, true,
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
				PintaCore.Tools.Commit ();

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

				PintaCore.Workspace.NewDocument (size, true);
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

		private void HandlePintaCoreActionsViewToolbarToggled (object sender, EventArgs e)
		{
			main_window.ToggleToolbar (((ToggleAction)sender).Active);
		}

		private void HandlePintaCoreActionsViewRulersToggled (object sender, EventArgs e)
		{
			if (((ToggleAction)sender).Active)
				main_window.ShowRulers ();
			else
				main_window.HideRulers ();
		}

		private void HandleCentimetersActivated (object sender, EventArgs e)
		{
			if (main_window.hruler.Metric != MetricType.Centimeters)
				main_window.ChangeRulersUnit (Gtk.MetricType.Centimeters);
		}

		private void HandleInchesActivated (object sender, EventArgs e)
		{
			if (main_window.hruler.Metric != MetricType.Inches)
				main_window.ChangeRulersUnit (Gtk.MetricType.Inches);
		}

		private void HandlePixelsActivated (object sender, EventArgs e)
		{
			if (main_window.hruler.Metric != MetricType.Pixels)
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

		private void Workspace_SaveDocument (object sender, DocumentCancelEventArgs e)
		{
			// Document hasn't changed, don't re-save it
			if (!e.Document.IsDirty)
				return;

			if (e.Document.HasFile)
				// If the document already has a filename, just re-save it
				e.Cancel = !SaveFile (e.Document, null, null);
			else
				// The document has never been saved before
				e.Cancel = !SaveFileAs (e.Document);
		}

		private void HandleOpenRecentItemActivated (object sender, EventArgs e)
		{
			string fileUri = (sender as RecentAction).CurrentUri;

			PintaCore.Workspace.OpenFile (new Uri (fileUri).LocalPath);

			PintaCore.Workspace.ActiveDocument.HasFile = true;
		}

		private void HandlePintaCoreActionsFileOpenActivated (object sender, EventArgs e)
		{
			var fcd = new Gtk.FileChooserDialog (Catalog.GetString ("Open Image File"), PintaCore.Chrome.MainWindow,
							    FileChooserAction.Open, Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
							    Gtk.Stock.Open, Gtk.ResponseType.Ok);

			// Add image files filter
			FileFilter ff = new FileFilter ();
			ff.AddPixbufFormats ();
			ff.AddPattern ("*.ora");
			ff.Name = Catalog.GetString ("Image files");
			fcd.AddFilter (ff);

			FileFilter ff2 = new FileFilter ();
			ff2.Name = Catalog.GetString ("All files");
			ff2.AddPattern ("*.*");
			fcd.AddFilter (ff2);

			fcd.AlternativeButtonOrder = new int[] { (int)ResponseType.Ok, (int)ResponseType.Cancel };
			fcd.SetCurrentFolder (lastDialogDir);

			int response = fcd.Run ();

			if (response == (int)Gtk.ResponseType.Ok) {
				lastDialogDir = fcd.CurrentFolder;

				if (PintaCore.Workspace.OpenFile (fcd.Filename)) {
					RecentManager.Default.AddFull (fcd.Uri, recentData);
					PintaCore.Workspace.ActiveDocument.HasFile = true;
				}
			}

			fcd.Destroy ();
		}
		#endregion

		private void HandleSaveAllActivated (object sender, EventArgs e)
		{
			foreach (Document doc in PintaCore.Workspace.OpenDocuments) {
				if (!doc.IsDirty)
					continue;

				PintaCore.Workspace.SetActiveDocument (doc);

				// Loop through all of these until we get a cancel
				if (!doc.Save ())
					break;
			}
		}

		private void HandleCloseAllActivated (object sender, EventArgs e)
		{
			while (PintaCore.Workspace.HasOpenDocuments) {
				int count = PintaCore.Workspace.OpenDocuments.Count;

				PintaCore.Actions.File.Close.Activate ();

				// If we still have the same number of open documents,
				// the user cancelled on a Save prompt.
				if (count == PintaCore.Workspace.OpenDocuments.Count)
					return;
			}
		}

		#region Private Methods
		public void ClipboardEmptyError ()
		{
			var primary = Catalog.GetString ("Paste cancelled");
			var secondary = Catalog.GetString ("The clipboard does not contain an image");
			var markup = "<span weight=\"bold\" size=\"larger\">{0}</span>\n\n{1}\n";
			markup = string.Format (markup, primary, secondary);

			var md = new MessageDialog (PintaCore.Chrome.MainWindow, DialogFlags.Modal,
						    MessageType.Error, ButtonsType.None, true,
						    markup);

			md.AddButton (Stock.Ok, ResponseType.Yes);

			md.Run ();
			md.Destroy ();
		}
		#endregion

		#region File Handling
		private string lastDialogDir;
		private RecentData recentData;

		private void InitializeFileActions ()
		{
			lastDialogDir = System.Environment.GetFolderPath (Environment.SpecialFolder.MyPictures);

			recentData = new RecentData ();
			recentData.AppName = "Pinta";
			recentData.AppExec = PintaCore.System.GetExecutablePathName ();
			recentData.MimeType = "image/*";
		}

		// This is actually both for "Save As" and saving a file that never
		// been saved before.  Either way, we need to prompt for a filename.
		private bool SaveFileAs (Document document)
		{
			var fcd = new FileChooserDialog (Mono.Unix.Catalog.GetString ("Save Image File"),
									       PintaCore.Chrome.MainWindow,
									       FileChooserAction.Save,
									       Gtk.Stock.Cancel,
									       Gtk.ResponseType.Cancel,
									       Gtk.Stock.Save, Gtk.ResponseType.Ok);

			fcd.DoOverwriteConfirmation = true;
			fcd.SetCurrentFolder (lastDialogDir);
			fcd.AlternativeButtonOrder = new int[] { (int)ResponseType.Ok, (int)ResponseType.Cancel };

			bool hasFile = document.HasFile;

			if (hasFile)
				fcd.SetFilename (document.PathAndFileName);

			Dictionary<FileFilter, FormatDescriptor> filetypes = new Dictionary<FileFilter, FormatDescriptor> ();

			// Add all the formats we support to the save dialog
			foreach (var format in PintaCore.System.ImageFormats.Formats) {
				if (!format.IsReadOnly ()) {
					fcd.AddFilter (format.Filter);
					filetypes.Add (format.Filter, format);
				}
			}

			// If we already have a format, set it to the default.
			// If not, default to jpeg
			if (hasFile)
				fcd.Filter = PintaCore.System.ImageFormats.GetFormatByFile (document.Filename).Filter;
			else
				fcd.Filter = PintaCore.System.ImageFormats.GetDefaultFormat ().Filter;

			// Replace GTK's ConfirmOverwrite with our own, for UI consistency
			fcd.ConfirmOverwrite += (eventSender, eventArgs) => {
				if (this.ConfirmOverwrite (fcd, fcd.Filename))
					eventArgs.RetVal = FileChooserConfirmation.AcceptFilename;
				else
					eventArgs.RetVal = FileChooserConfirmation.SelectAgain;
			};

			while (fcd.Run () == (int)Gtk.ResponseType.Ok) {
				FormatDescriptor format = filetypes[fcd.Filter];
				string file = fcd.Filename;

				if (string.IsNullOrEmpty (Path.GetExtension (file))) {
					// No extension; add one from the format descriptor.
					file = string.Format ("{0}.{1}", file, format.Extensions[0]);
					fcd.CurrentName = Path.GetFileName (file);

					// We also need to display an overwrite confirmation message manually,
					// because MessageDialog won't do this for us in this case.
					if (File.Exists (file) && !ConfirmOverwrite (fcd, file))
						continue;
				}

				// Always follow the extension rather than the file type drop down
				// ie: if the user chooses to save a "jpeg" as "foo.png", we are going
				// to assume they just didn't update the dropdown and really want png
				var format_type = PintaCore.System.ImageFormats.GetFormatByFile (file);

				if (format_type != null)
					format = format_type;

				lastDialogDir = fcd.CurrentFolder;
				SaveFile (document, file, format);
				RecentManager.Default.AddFull (fcd.Uri, recentData);
				PintaCore.System.ImageFormats.SetDefaultFormat (Path.GetExtension (file));

				document.HasFile = true;
				document.PathAndFileName = file;

				fcd.Destroy (); 
				return true;
			}

			fcd.Destroy ();
			return false;
		}

		private bool SaveFile (Document document, string file, FormatDescriptor format)
		{
			if (string.IsNullOrEmpty (file))
				file = document.PathAndFileName;

			if (format == null)
				format = PintaCore.System.ImageFormats.GetFormatByFile (file);

			if (format == null || format.IsReadOnly ()) {
				MessageDialog md = new MessageDialog (PintaCore.Chrome.MainWindow, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, Catalog.GetString ("Pinta does not support saving images in this file format."), file);
				md.Title = Catalog.GetString ("Error");

				md.Run ();
				md.Destroy ();
				return false;
			}

			// Commit any pending changes
			PintaCore.Tools.Commit ();

			format.Exporter.Export (document, file);

			document.Filename = Path.GetFileName (file);
			document.IsDirty = false;

			return true;
		}

		private bool ConfirmOverwrite (FileChooserDialog fcd, string file)
		{
			string primary = Catalog.GetString ("A file named \"{0}\" already exists. Do you want to replace it?");
			string secondary = Catalog.GetString ("The file already exists in \"{1}\". Replacing it will overwrite its contents.");
			string message = string.Format (markup, primary, secondary);

			MessageDialog md = new MessageDialog (fcd, DialogFlags.Modal | DialogFlags.DestroyWithParent,
				MessageType.Question, ButtonsType.None,
				true, message, System.IO.Path.GetFileName (file), fcd.CurrentFolder);

			md.AddButton (Stock.Cancel, ResponseType.Cancel);
			md.AddButton (Stock.Save, ResponseType.Ok);
			md.DefaultResponse = ResponseType.Cancel;
			md.AlternativeButtonOrder = new int[] { (int)ResponseType.Ok, (int)ResponseType.Cancel };

			int response = md.Run ();
			md.Destroy ();

			return response == (int)ResponseType.Ok;
		}
		#endregion
	}
}

