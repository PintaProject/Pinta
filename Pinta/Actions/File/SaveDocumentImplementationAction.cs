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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gtk;
using Pinta.Core;

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
			if (e.SaveAs || !e.Document.HasFile) {
				e.Cancel = !SaveFileAs (e.Document);
			} else {
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
				DoOverwriteConfirmation = true,
				LocalOnly = false
			};

			fcd.SetCurrentFolderFile (PintaCore.System.GetDialogDirectory ());

			if (document.HasFile)
				fcd.SetFile (document.File);
			else {
				// Append the default extension, producing e.g. "Unsaved Image 1.png"
				var default_ext = PintaCore.System.ImageFormats.GetDefaultSaveFormat ().Extensions.First ();
				fcd.CurrentName = $"{document.DisplayName}.{default_ext}";
			}

			// Add all the formats we support to the save dialog
			var filetypes = new Dictionary<FileFilter, FormatDescriptor> ();
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
				format_desc = PintaCore.System.ImageFormats.GetFormatByFile (document.DisplayName);

			if (format_desc == null)
				format_desc = PintaCore.System.ImageFormats.GetDefaultSaveFormat ();

			fcd.Filter = format_desc.Filter;

			while ((ResponseType) fcd.Run () == ResponseType.Accept) {
				var file = fcd.File;

				// Note that we can't use file.GetDisplayName() because the file doesn't exist.
				var display_name = file.Parent.GetRelativePath (file);

				// Always follow the extension rather than the file type drop down
				// ie: if the user chooses to save a "jpeg" as "foo.png", we are going
				// to assume they just didn't update the dropdown and really want png
				var format = PintaCore.System.ImageFormats.GetFormatByFile (display_name) ?? filetypes[fcd.Filter];

				var directory = file.Parent;
				if (directory is not null)
					PintaCore.System.LastDialogDirectory = directory;

				// If saving the file failed or was cancelled, let the user select
				// a different file type.
				if (!SaveFile (document, file, format, PintaCore.Chrome.MainWindow))
					continue;

				//The user is saving the Document to a new file, so technically it
				//hasn't been saved to its associated file in this session.
				document.HasBeenSavedInSession = false;

				RecentManager.Default.AddFull (file.Uri.ToString (), PintaCore.System.RecentData);
				PintaCore.System.ImageFormats.SetDefaultFormat (format.Extensions.First ());

				document.File = file;
				return true;
			}

			return false;
		}

		private bool SaveFile (Document document, GLib.IFile? file, FormatDescriptor? format, Window parent)
		{
			if (file is null)
				file = document.File;

			if (file is null)
				throw new ArgumentException ("Attempted to save a document with no associated file");

			if (format == null)
				format = PintaCore.System.ImageFormats.GetFormatByFile (file.GetDisplayName ());

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

			document.File = file;

			PintaCore.Tools.DoAfterSave (document);

			// Mark the document as clean following the tool's after-save handler, which might
			// adjust history (e.g. undo changes that were committed before saving).
			document.Workspace.History.SetClean ();

			//Now the Document has been saved to the file it's associated with in this session.
			document.HasBeenSavedInSession = true;

			return true;
		}
	}
}
