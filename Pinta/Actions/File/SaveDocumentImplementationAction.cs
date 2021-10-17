// 
// SaveDocumentImplmentationAction.cs
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
using System.Linq;
using Gtk;
using Pinta.Core;
using System.IO;
using System.Collections.Generic;

namespace Pinta.Actions
{
	class SaveDocumentImplmentationAction : IActionHandler
	{
		private const string markup = "<span weight=\"bold\" size=\"larger\">{0}</span>\n\n{1}";

		#region IActionHandler Members
		public void Initialize ()
		{
			PintaCore.Actions.File.SaveDocument += Activated;
		}

		public void Uninitialize ()
		{
			PintaCore.Actions.File.SaveDocument -= Activated;
		}
		#endregion

		private void Activated (object? sender, DocumentCancelEventArgs e)
		{
			// Prompt for a new filename for "Save As", or a document that hasn't been saved before
			if (e.SaveAs || !e.Document.HasFile)
			{
				e.Cancel = !SaveFileAs (e.Document);
			}
			else
			{
				// Document hasn't changed, don't re-save it
				if (!e.Document.IsDirty)
					return;

				// If the document already has a filename, just re-save it
				e.Cancel = !SaveFile (e.Document, null, null, PintaCore.Chrome.MainWindow);
			}
		}

		// This is actually both for "Save As" and saving a file that never
		// been saved before.  Either way, we need to prompt for a filename.
		private bool SaveFileAs (Document document)
		{
			using var fcd = new FileChooserNative (
				Translations.GetString ("Save Image File"),
				PintaCore.Chrome.MainWindow,
				FileChooserAction.Save,
				Translations.GetString ("Save"),
				Translations.GetString ("Cancel")) {
				DoOverwriteConfirmation = true
			};

			fcd.SetCurrentFolder (PintaCore.System.GetDialogDirectory ());

			if (document.HasFile)
				fcd.SetFilename (document.PathAndFileName);
			else
				fcd.CurrentName = document.Filename;

			var filetypes = new Dictionary<FileFilter, FormatDescriptor> ();

			// Add all the formats we support to the save dialog
			foreach (var format in PintaCore.System.ImageFormats.Formats) {
				if (!format.IsReadOnly ()) {
					fcd.AddFilter (format.Filter);
					filetypes.Add (format.Filter, format);

					// Set the filter to anything we found
					// We want to ensure that *something* is selected in the filetype
					fcd.Filter = format.Filter;
				}
			}

			// If we already have a format, set it to the default.
			// If not, default to jpeg
			FormatDescriptor? format_desc = null;

			if (document.HasFile)
				format_desc = PintaCore.System.ImageFormats.GetFormatByFile (document.Filename);

			if (format_desc == null) {
				format_desc = PintaCore.System.ImageFormats.GetDefaultSaveFormat ();

				// Gtk doesn't like it if we set the file name to an extension that we don't have
				// a filter for, so we change the extension to our default extension.
				if (document.HasFile)
					fcd.SetFilename (Path.ChangeExtension (document.PathAndFileName, format_desc.Extensions[0]));
			}

			fcd.Filter = format_desc.Filter;

			fcd.AddNotification ("filter", OnFilterChanged);

			// Replace GTK's ConfirmOverwrite with our own, for UI consistency
			fcd.ConfirmOverwrite += (eventSender, eventArgs) => {
				if (this.ConfirmOverwrite (fcd, fcd.Filename))
					eventArgs.RetVal = FileChooserConfirmation.AcceptFilename;
				else
					eventArgs.RetVal = FileChooserConfirmation.SelectAgain;
			};

			while ((ResponseType)fcd.Run () == ResponseType.Accept) {
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

				var directory = System.IO.Path.GetDirectoryName (file);
				if (directory is not null)
					PintaCore.System.LastDialogDirectory = directory;

				// If saving the file failed or was cancelled, let the user select
				// a different file type.
				if (!SaveFile (document, file, format, PintaCore.Chrome.MainWindow))
					continue;

				//The user is saving the Document to a new file, so technically it
				//hasn't been saved to its associated file in this session.
				document.HasBeenSavedInSession = false;

				RecentManager.Default.AddFull (fcd.Uri, PintaCore.System.RecentData);
				PintaCore.System.ImageFormats.SetDefaultFormat (Path.GetExtension (file));

				document.HasFile = true;
				document.PathAndFileName = file;
				return true;
			}

			return false;
		}

		private bool SaveFile (Document document, string? file, FormatDescriptor? format, Window parent)
		{
			if (string.IsNullOrEmpty (file))
				file = document.PathAndFileName;

			if (format == null)
				format = PintaCore.System.ImageFormats.GetFormatByFile (file);

			if (format == null || format.IsReadOnly ()) {
				using var md = new MessageDialog (parent, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, Translations.GetString ("Pinta does not support saving images in this file format."), file);
				md.Title = Translations.GetString ("Error");

				md.Run ();
				return false;
			}

			// Commit any pending changes
			PintaCore.Tools.Commit ();

			try {
				format.Exporter.Export (document, file, parent);
			} catch (GLib.GException e) { // Errors from GDK
				if (e.Message == "Image too large to be saved as ICO") {
					string primary = Translations.GetString ("Image too large");
					string secondary = Translations.GetString ("ICO files can not be larger than 255 x 255 pixels.");
					string message = string.Format (markup, primary, secondary);

					using var md = new MessageDialog (parent, DialogFlags.Modal, MessageType.Error,
					ButtonsType.Ok, message);

					md.Run ();
					return false;
				} else if (e.Message.Contains ("Permission denied") && e.Message.Contains ("Failed to open")) {
					string primary = Translations.GetString ("Failed to save image");
					// Translators: {0} is the name of a file that the user does not have write permission for.
					string secondary = Translations.GetString ("You do not have access to modify '{0}'. The file or folder may be read-only.", file);
					string message = string.Format (markup, primary, secondary);

					using var md = new MessageDialog (parent, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, message);

					md.Run ();
					return false;
				} else {
					throw; // Only catch exceptions we know the reason for
				}
			} catch (OperationCanceledException) {
				return false;
			}

			document.Filename = Path.GetFileName (file);

			PintaCore.Tools.DoAfterSave(document);

			// Mark the document as clean following the tool's after-save handler, which might
			// adjust history (e.g. undo changes that were committed before saving).
			document.Workspace.History.SetClean ();

			//Now the Document has been saved to the file it's associated with in this session.
			document.HasBeenSavedInSession = true;

			return true;
		}

		private bool ConfirmOverwrite (IFileChooser fcd, string file)
		{
			string primary = Translations.GetString ("A file named \"{0}\" already exists. Do you want to replace it?");
			string secondary = Translations.GetString ("The file already exists in \"{1}\". Replacing it will overwrite its contents.");
			string message = string.Format (markup, primary, secondary);

			using var md = new MessageDialog (PintaCore.Chrome.MainWindow, DialogFlags.Modal | DialogFlags.DestroyWithParent,
				MessageType.Question, ButtonsType.None,
				true, message, System.IO.Path.GetFileName (file), fcd.CurrentFolder);

			// Use the standard button order for each OS.
			if (PintaCore.System.OperatingSystem == OS.Windows) {
				md.AddButton (Translations.GetString ("Replace"), ResponseType.Ok);
				md.AddButton (Stock.Cancel, ResponseType.Cancel);
			}
			else {
				md.AddButton (Stock.Cancel, ResponseType.Cancel);
				md.AddButton (Translations.GetString ("Replace"), ResponseType.Ok);
			}

			md.DefaultResponse = ResponseType.Cancel;

			int response = md.Run ();

			return response == (int)ResponseType.Ok;
		}

		private void OnFilterChanged (object o, GLib.NotifyArgs args)
		{
			var fcd = (IFileChooser) o;

			// Ensure that the file filter is never blank.
			if (fcd.Filter == null)
			{
				fcd.Filter = PintaCore.System.ImageFormats.GetDefaultSaveFormat ().Filter;
				return;
			}

			// find the FormatDescriptor
			FormatDescriptor format_desc = PintaCore.System.ImageFormats.Formats.Single (f => f.Filter == fcd.Filter);

			// adjust the filename
			var p = fcd.Filename ?? fcd.CurrentName;
			p = Path.ChangeExtension (Path.GetFileName (p), format_desc.Extensions[0]);
			fcd.CurrentName = p;
		}
	}
}
