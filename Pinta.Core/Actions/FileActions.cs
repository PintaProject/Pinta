// 
// FileActions.cs
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
using System.Collections.Generic;
using System.IO;
using Gdk;
using Gtk;
using Mono.Unix;

namespace Pinta.Core
{
	public class FileActions
	{
		private const string markup = "<span weight=\"bold\" size=\"larger\">{0}</span>\n\n{1}";
	
		public Gtk.Action New { get; private set; }
		public Gtk.Action NewScreenshot { get; private set; }
		public Gtk.Action Open { get; private set; }
		public Gtk.Action OpenRecent { get; private set; }
		public Gtk.Action Close { get; private set; }
		public Gtk.Action Save { get; private set; }
		public Gtk.Action SaveAs { get; private set; }
		public Gtk.Action Print { get; private set; }
		public Gtk.Action Exit { get; private set; }
		
		private IList<FormatDescriptor> formats;
		private IDictionary<string, FormatDescriptor> formatsByExt;
		private RecentData recentData;
		private string lastDialogDir;
		
		public event EventHandler BeforeQuit;
		public event EventHandler<ModifyCompressionEventArgs> ModifyCompression;
		
		public FileActions ()
		{
			New = new Gtk.Action ("New", Catalog.GetString ("New..."), null, Stock.New);
			NewScreenshot = new Gtk.Action ("NewScreenshot", Catalog.GetString ("New Screenshot..."), null, Stock.Fullscreen);
			Open = new Gtk.Action ("Open", Catalog.GetString ("Open..."), null, Stock.Open);
			OpenRecent = new RecentAction ("OpenRecent", Catalog.GetString ("Open Recent"), null, Stock.Open, RecentManager.Default);
			
			RecentFilter recentFilter = new RecentFilter ();
			recentFilter.AddApplication ("Pinta");
			
			(OpenRecent as RecentAction).AddFilter (recentFilter);
			
			recentData = new RecentData ();
			recentData.AppName = "Pinta";
			recentData.AppExec = GetExecutablePathname ();
			recentData.MimeType = "image/*";
			
			formats = new List<FormatDescriptor> ();
			formatsByExt = new SortedDictionary<string, FormatDescriptor> ();
			FillFormats ();
			
			lastDialogDir = System.Environment.GetFolderPath (Environment.SpecialFolder.MyPictures);

			Close = new Gtk.Action ("Close", Catalog.GetString ("Close"), null, Stock.Close);
			Save = new Gtk.Action ("Save", Catalog.GetString ("Save"), null, Stock.Save);
			SaveAs = new Gtk.Action ("SaveAs", Catalog.GetString ("Save As..."), null, Stock.SaveAs);
			Print = new Gtk.Action ("Print", Catalog.GetString ("Print"), null, Stock.Print);
			Exit = new Gtk.Action ("Exit", Catalog.GetString ("Quit"), null, Stock.Quit);

			New.ShortLabel = Catalog.GetString ("New");
			Open.ShortLabel = Catalog.GetString ("Open");
			Open.IsImportant = true;
			Save.IsImportant = true;
			
			Close.Sensitive = false;
			Print.Sensitive = false;
		}

		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
			menu.Append (New.CreateAcceleratedMenuItem (Gdk.Key.N, Gdk.ModifierType.ControlMask));
			menu.Append (NewScreenshot.CreateMenuItem ());
			menu.Append (Open.CreateAcceleratedMenuItem (Gdk.Key.O, Gdk.ModifierType.ControlMask));
			menu.Append (OpenRecent.CreateMenuItem ());
			menu.Append (Close.CreateAcceleratedMenuItem (Gdk.Key.W, Gdk.ModifierType.ControlMask));
			menu.AppendSeparator ();
			menu.Append (Save.CreateAcceleratedMenuItem (Gdk.Key.S, Gdk.ModifierType.ControlMask));
			menu.Append (SaveAs.CreateAcceleratedMenuItem (Gdk.Key.S, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.AppendSeparator ();
			//menu.Append (Print.CreateAcceleratedMenuItem (Gdk.Key.P, Gdk.ModifierType.ControlMask));
			//menu.AppendSeparator ();
			menu.Append (Exit.CreateAcceleratedMenuItem (Gdk.Key.Q, Gdk.ModifierType.ControlMask));
		}
		
		public void RegisterHandlers ()
		{
			Open.Activated += HandlePintaCoreActionsFileOpenActivated;
			(OpenRecent as RecentAction).ItemActivated += HandleOpenRecentItemActivated;
			Save.Activated += HandlePintaCoreActionsFileSaveActivated;
			SaveAs.Activated += HandlePintaCoreActionsFileSaveAsActivated;
			Exit.Activated += HandlePintaCoreActionsFileExitActivated;
		}
		
		private void FillFormats ()
		{
			foreach (var format in Pixbuf.Formats) {
				string formatName = format.Name.ToLowerInvariant ();
				string[] extensions = (formatName == "jpeg") ? new string[] { "jpg", "jpeg" } : new string[] { formatName };
				GdkPixbufFormat importer = new GdkPixbufFormat (format.Name.ToLowerInvariant ());
				IImageExporter exporter;
			
				if (formatName == "jpeg")
					exporter = importer = new JpegFormat ();
				else if (formatName == "tga")
					exporter = new TgaExporter ();
				else if (format.IsWritable)
					exporter = importer;
				else
					exporter = null;
			
				formats.Add (new FormatDescriptor (formatName, formatName.ToUpperInvariant(), extensions, importer, exporter));
			}
			
			OraFormat oraHandler = new OraFormat ();
			formats.Add (new FormatDescriptor ("ora", "OpenRaster", new string[] { "ora" }, oraHandler, oraHandler));
			
			foreach (var fd in formats) {
				foreach (var ext in fd.Extensions) {
					formatsByExt.Add (string.Format (".{0}", ext), fd);
				}
			}
		}
		
		#endregion

		#region Public Methods
		public void NewFile (Size imageSize)
		{
			PintaCore.Workspace.CreateAndActivateDocument (null, imageSize);
			PintaCore.Workspace.ActiveDocument.HasFile = false;
			PintaCore.Workspace.ActiveWorkspace.CanvasSize = imageSize;

			// Start with an empty white layer
			Layer background = PintaCore.Workspace.ActiveDocument.AddNewLayer (Catalog.GetString ("Background"));
			
			using (Cairo.Context g = new Cairo.Context (background.Surface)) {
				g.SetSourceRGB (1, 1, 1);
				g.Paint ();
			}

			PintaCore.Workspace.ActiveWorkspace.History.PushNewItem (new BaseHistoryItem (Stock.New, Catalog.GetString ("New Image")));
			PintaCore.Workspace.ActiveDocument.IsDirty = false;
			PintaCore.Actions.View.ZoomToWindow.Activate ();
		}
		
		public void NewFileWithScreenshot ()
		{
			Screen screen = Screen.Default;
			Pixbuf pb = Pixbuf.FromDrawable (screen.RootWindow, screen.RootWindow.Colormap, 0, 0, 0, 0, screen.Width, screen.Height);
			NewFile (new Size (screen.Width, screen.Height));

			using (Cairo.Context g = new Cairo.Context (PintaCore.Layers[0].Surface)) {
				CairoHelper.SetSourcePixbuf (g, pb, 0, 0);
				g.Paint ();
			}

			(pb as IDisposable).Dispose ();
			PintaCore.Workspace.ActiveDocument.IsDirty = true;
		}
		
		public bool OpenFile (string file)
		{
			bool fileOpened = false;
			
			try {
				// Open the image and add it to the layers
				IImageImporter importer = formatsByExt[System.IO.Path.GetExtension (file).ToLowerInvariant ()].Importer;
				importer.Import (PintaCore.Layers, file);

				PintaCore.Workspace.ActiveDocument.Pathname = System.IO.Path.GetFullPath (file);
				PintaCore.Workspace.ActiveWorkspace.History.PushNewItem (new BaseHistoryItem (Stock.Open, Catalog.GetString ("Open Image")));
				PintaCore.Workspace.ActiveDocument.IsDirty = false;
				PintaCore.Actions.View.ZoomToWindow.Activate ();
				PintaCore.Workspace.Invalidate ();
				
				fileOpened = true;
			} catch {
				MessageDialog md = new MessageDialog (PintaCore.Chrome.MainWindow, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, Catalog.GetString ("Could not open file: {0}"), file);
				md.Title = Catalog.GetString ("Error");
				
				md.Run ();
				md.Destroy ();
			}
			
			return fileOpened;
		}
		#endregion
		
		static string GetExecutablePathname()
		{
			string executablePathName = System.Environment.GetCommandLineArgs ()[0];
			executablePathName = System.IO.Path.GetFullPath (executablePathName);
			
			return executablePathName;
		}
		
		void AddRecentFileUri (string uri)
		{
			RecentManager.Default.AddFull (uri, recentData);
		}
		
		internal int PromptJpegCompressionLevel ()
		{
			ModifyCompressionEventArgs e = new ModifyCompressionEventArgs (85);
			
			if (ModifyCompression != null)
				ModifyCompression (this, e);
				
			return e.Cancel ? -1 : e.Quality;
		}
		
		#region Action Handlers
		private void HandleOpenRecentItemActivated (object sender, EventArgs e)
		{
			string fileUri = (sender as RecentAction).CurrentUri;

			OpenFile (new Uri (fileUri).LocalPath);

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
			
			fcd.AlternativeButtonOrder = new int[] { (int) ResponseType.Ok, (int) ResponseType.Cancel };
			fcd.SetCurrentFolder (lastDialogDir);

			int response = fcd.Run ();
		
			if (response == (int)Gtk.ResponseType.Ok) {
				lastDialogDir = fcd.CurrentFolder;

				if (OpenFile (fcd.Filename)) {
					AddRecentFileUri (fcd.Uri);

					PintaCore.Workspace.ActiveDocument.HasFile = true;
				}
			}

			fcd.Destroy ();
		}
		
		private void HandlePintaCoreActionsFileSaveActivated (object sender, EventArgs e)
		{
			if (PintaCore.Workspace.ActiveDocument.HasFile)
				SaveFile (PintaCore.Workspace.ActiveDocument.Pathname, null);
			else
				HandlePintaCoreActionsFileSaveAsActivated (null, EventArgs.Empty);
		}
		
		private bool ConfirmOverwrite (FileChooserDialog fcd, string file) {
			string primary = Catalog.GetString ("A file named \"{0}\" already exists. Do you want to replace it?");
			string secondary = Catalog.GetString ("The file already exists in \"{1}\". Replacing it will overwrite its contents.");
			string message = string.Format (markup, primary, secondary);

			MessageDialog md = new MessageDialog (fcd, DialogFlags.Modal | DialogFlags.DestroyWithParent,
				MessageType.Question, ButtonsType.None,
				true, message, System.IO.Path.GetFileName (file), fcd.CurrentFolder);

			md.AddButton (Stock.Cancel, ResponseType.Cancel);
			md.AddButton (Stock.Save, ResponseType.Ok);
			md.DefaultResponse = ResponseType.Cancel;
			md.AlternativeButtonOrder = new int[] { (int) ResponseType.Ok, (int) ResponseType.Cancel };

			int response = md.Run ();
			md.Destroy ();
			
			return response == (int) ResponseType.Ok;
		}
		
		private void HandlePintaCoreActionsFileSaveAsActivated (object sender, EventArgs e)
		{
			var fcd = new FileChooserDialog (Mono.Unix.Catalog.GetString ("Save Image File"),
			                                                       PintaCore.Chrome.MainWindow,
			                                                       FileChooserAction.Save,
			                                                       Gtk.Stock.Cancel,
			                                                       Gtk.ResponseType.Cancel,
			                                                       Gtk.Stock.Save, Gtk.ResponseType.Ok);
			
			fcd.DoOverwriteConfirmation = true;
			fcd.SetCurrentFolder (lastDialogDir);
			fcd.AlternativeButtonOrder = new int[] { (int) ResponseType.Ok, (int) ResponseType.Cancel };
			bool hasFile = PintaCore.Workspace.ActiveDocument.HasFile;
			string currentExt = null;

			if (hasFile) {
				fcd.SetFilename (PintaCore.Workspace.ActiveDocument.Pathname);
				currentExt = Path.GetExtension(PintaCore.Workspace.Filename.ToLowerInvariant ());
			}
			
			Dictionary<FileFilter, FormatDescriptor> filetypes = new Dictionary<FileFilter, FormatDescriptor> ();
						
			// Add all the formats we support to the save dialog
			foreach (var format in formats) {
				if (!format.IsReadOnly ()) {
					fcd.AddFilter (format.Filter);
					filetypes.Add (format.Filter, format);
					
					if ((hasFile && formatsByExt[currentExt] == format) || (!hasFile && format.Name == "jpeg")) {
						fcd.Filter = format.Filter;
					}
				}
			}
			
			// Replace GTK's ConfirmOverwrite with our own, for UI consistency
			fcd.ConfirmOverwrite += (eventSender, eventArgs) => {
				if (this.ConfirmOverwrite (fcd, fcd.Filename)) {
					eventArgs.RetVal = FileChooserConfirmation.AcceptFilename;
				} else {
					eventArgs.RetVal = FileChooserConfirmation.SelectAgain;
				}
			};
			
			while (fcd.Run () == (int) Gtk.ResponseType.Ok) {
				FormatDescriptor format = filetypes[fcd.Filter];
				string file = fcd.Filename;
				
				if (string.IsNullOrEmpty (System.IO.Path.GetExtension (file))) {
					// No extension; add one from the format descriptor.
					file = string.Format ("{0}.{1}", file, format.Extensions[0]);
					fcd.CurrentName = System.IO.Path.GetFileName (file);
					
					// We also need to display an overwrite confirmation message manually,
					// because MessageDialog won't do this for us in this case.
					if (File.Exists (file) && !ConfirmOverwrite (fcd, file)) {
						continue;
					}
				}
				
				lastDialogDir = fcd.CurrentFolder;
				SaveFile (file, format);
				AddRecentFileUri (fcd.Uri);

				PintaCore.Workspace.ActiveDocument.HasFile = true;
				PintaCore.Workspace.ActiveDocument.Pathname = file;
				break;
			}

			fcd.Destroy ();
		}

		private void HandlePintaCoreActionsFileExitActivated (object sender, EventArgs e)
		{
			while (PintaCore.Workspace.HasOpenDocuments) {
				int count = PintaCore.Workspace.OpenDocuments.Count;
				
				Close.Activate ();
				
				// If we still have the same number of open documents,
				// the user cancelled on a Save prompt.
				if (count == PintaCore.Workspace.OpenDocuments.Count)
					return;
			}
			
			if (BeforeQuit != null)
				BeforeQuit (this, EventArgs.Empty);
				
			Application.Quit ();
		}
		#endregion
		
		#region Private Methods
		private void SaveFile (string file, FormatDescriptor format)
		{
			if (format == null) {
				string ext = System.IO.Path.GetExtension (file);
				// Is the fallback to PNG even necessary?
				format = formatsByExt[string.IsNullOrEmpty (ext) ? ".png" : ext];
			}
			
			if (format == null || format.IsReadOnly ()) {
				MessageDialog md = new MessageDialog (PintaCore.Chrome.MainWindow, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, Catalog.GetString ("Pinta does not support saving images in this file format."), file);
				md.Title = Catalog.GetString ("Error");
				
				md.Run ();
				md.Destroy ();
				return;
			}
			
			format.Exporter.Export (PintaCore.Layers, file);

			PintaCore.Workspace.Filename = Path.GetFileName (file);
			PintaCore.Workspace.IsDirty = false;
		}
		#endregion
	}
}
