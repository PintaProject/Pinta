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
using System.Linq;
using System.Threading.Tasks;
using Pinta.Core;

namespace Pinta.Actions;

internal sealed class SaveDocumentImplmentationAction : IActionHandler
{
	const string RESPONSE_CANCEL = "cancel";
	const string RESPONSE_FLATTEN = "flatten";

	private readonly FileActions file;
	private readonly ImageActions image;
	private readonly ChromeManager chrome;
	private readonly ImageConverterManager image_formats;
	private readonly RecentFileManager recent_files;
	private readonly ToolManager tools;
	internal SaveDocumentImplmentationAction (
		FileActions file,
		ImageActions image,
		ChromeManager chrome,
		ImageConverterManager imageFormats,
		RecentFileManager recentFiles,
		ToolManager tools)
	{
		this.file = file;
		this.image = image;
		this.chrome = chrome;
		image_formats = imageFormats;
		recent_files = recentFiles;
		this.tools = tools;
	}

	void IActionHandler.Initialize ()
	{
		file.SaveDocument += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		file.SaveDocument -= Activated;
	}

	private async Task<bool> Activated (FileActions sender, DocumentSaveEventArgs e)
	{
		// Prompt for a new filename for "Save As", or a document that hasn't been saved before
		if (e.SaveAs || !e.Document.HasFile) {
			return await SaveFileAs (e.Document);
		}

		// Document hasn't changed, don't re-save it
		if (!e.Document.IsDirty)
			return true;

		// If the document already has a filename, just re-save it
		return await SaveFile (e.Document, null, null, chrome.MainWindow);
	}

	// This is actually both for "Save As" and saving a file that never
	// been saved before.  Either way, we need to prompt for a filename.
	private async Task<bool> SaveFileAs (Document document)
	{
		var fcd = Gtk.FileChooserNative.New (
			Translations.GetString ("Save Image File"),
			chrome.MainWindow,
			Gtk.FileChooserAction.Save,
			Translations.GetString ("Save"),
			Translations.GetString ("Cancel"));

		if (document.HasFile)
			fcd.SetFile (document.File!);
		else {
			if (recent_files.GetDialogDirectory () is Gio.File dir && dir.QueryExists (null))
				fcd.SetCurrentFolder (dir);

			// Append the default extension, producing e.g. "Unsaved Image 1.png"
			string default_ext = image_formats.GetDefaultSaveFormat ().Extensions.First ();
			fcd.SetCurrentName ($"{document.DisplayName}.{default_ext}");
		}

		// Add all the formats we support to the save dialog
		Dictionary<Gtk.FileFilter, FormatDescriptor> filetypes = [];
		foreach (var format in image_formats.Formats) {

			if (!format.IsExportAvailable ())
				continue;

			fcd.AddFilter (format.Filter);
			filetypes.Add (format.Filter, format);

			// Set the filter to anything we found
			// We want to ensure that *something* is selected in the filetype
			fcd.Filter = format.Filter;
		}

		// If we already have a format, set it to the default.
		// If not, default to jpeg
		FormatDescriptor? format_desc = null;
		FormatDescriptor? previous_format_desc = null;

		if (document.HasFile) {
			previous_format_desc = image_formats.GetFormatByFile (document.DisplayName);
			format_desc = previous_format_desc;
		}

		if (format_desc == null || !format_desc.IsExportAvailable ())
			format_desc = image_formats.GetDefaultSaveFormat ();

		fcd.Filter = format_desc.Filter;

		while (await fcd.RunAsync () == Gtk.ResponseType.Accept) {

			Gio.File file = fcd.GetFile ()!;

			// Note that we can't use file.GetDisplayName() because the file doesn't exist.
			string displayName = file.GetParent ()!.GetRelativePath (file)!;

			// Always follow the extension rather than the file type drop down
			// ie: if the user chooses to save a "jpeg" as "foo.png", we are going
			// to assume they just didn't update the dropdown and really want png
			FormatDescriptor? format = image_formats.GetFormatByFile (displayName);
			if (format is null) {
				if (fcd.Filter is not null)
					format = filetypes[fcd.Filter];
				else // Somehow, no file filter was selected...
					format = image_formats.GetDefaultSaveFormat ();
			}

			// If the format doesn't support layers but the previous one did, ask to flatten the image
			if (!format.SupportsLayers
				&& previous_format_desc?.SupportsLayers == true
				&& document.Layers.Count () > 1) {

				string heading = Translations.GetString ("This format does not support layers. Flatten image?");
				string body = Translations.GetString ("Flattening the image will merge all layers into a single layer.");

				using Adw.MessageDialog dialog = Adw.MessageDialog.New (chrome.MainWindow, heading, body);
				dialog.AddResponse (RESPONSE_CANCEL, Translations.GetString ("_Cancel"));
				dialog.AddResponse (RESPONSE_FLATTEN, Translations.GetString ("Flatten"));
				dialog.SetResponseAppearance (RESPONSE_FLATTEN, Adw.ResponseAppearance.Suggested);

				dialog.CloseResponse = RESPONSE_CANCEL;
				dialog.DefaultResponse = RESPONSE_FLATTEN;

				string response = await dialog.RunAsync ();

				if (response == RESPONSE_CANCEL)
					continue;

				// Flatten the image
				tools.Commit ();
				image.Flatten.Activate ();
			}

			Gio.File? directory = file.GetParent ();

			if (directory is not null)
				recent_files.LastDialogDirectory = directory;

			// If saving the file failed or was cancelled, let the user select
			// a different file type.
			if (!await SaveFile (document, file, format, chrome.MainWindow)) {
				// Re-set the current name and directory
				fcd.SetCurrentName (displayName);
				fcd.SetCurrentFolder (directory);
				continue;
			}

			//The user is saving the Document to a new file, so technically it
			//hasn't been saved to its associated file in this session.
			document.HasBeenSavedInSession = false;

			recent_files.AddFile (file);
			image_formats.SetDefaultFormat (format.Extensions.First ());

			document.File = file;
			document.FileType = format.Extensions.First ();
			return true;
		}

		return false;
	}

	private async Task<bool> SaveFile (Document document, Gio.File? file, FormatDescriptor? format, Gtk.Window parent)
	{
		file ??= document.File;

		if (file is null)
			throw new ArgumentException ("Attempted to save a document with no associated file", nameof (file));

		if (format == null) {

			if (string.IsNullOrEmpty (document.FileType))
				throw new ArgumentException ($"{nameof (document.FileType)} must contain value.", nameof (document));

			format = image_formats.GetFormatByExtension (document.FileType);
		}

		if (format == null || !format.IsExportAvailable ()) {

			await chrome.ShowMessageDialog (
				parent,
				Translations.GetString ("Pinta does not support saving images in this file format."),
				file.GetDisplayName ());

			return false;
		}

		// Commit any pending changes
		tools.Commit ();

		try {
			format.Exporter.Export (document, file, parent);

		} catch (GLib.GException e) when (e.Message == "Image too large to be saved as ICO") {

			string primary = Translations.GetString ("Image too large");
			string secondary = Translations.GetString ("ICO files can not be larger than 255 x 255 pixels.");

			await chrome.ShowMessageDialog (parent, primary, secondary);

			return false;

		} catch (GLib.GException e) when (e.Message.Contains ("Permission denied") && e.Message.Contains ("Failed to open")) {

			string primary = Translations.GetString ("Failed to save image");

			// Translators: {0} is the name of a file that the user does not have write permission for.
			string secondary = Translations.GetString ("You do not have access to modify '{0}'. The file or folder may be read-only.", file);

			await chrome.ShowMessageDialog (parent, primary, secondary);

			return false;

		} catch (OperationCanceledException) {

			return false;
		}

		document.File = file;
		document.FileType = format.Extensions.First ();

		tools.DoAfterSave (document);

		// Mark the document as clean following the tool's after-save handler, which might
		// adjust history (e.g. undo changes that were committed before saving).
		document.Workspace.History.SetClean ();

		//Now the Document has been saved to the file it's associated with in this session.
		document.HasBeenSavedInSession = true;

		return true;
	}
}
