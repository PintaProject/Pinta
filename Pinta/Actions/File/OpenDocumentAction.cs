// 
// OpenDocumentAction.cs
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

namespace Pinta.Actions;

internal sealed class OpenDocumentAction : IActionHandler
{
	private readonly FileActions file_actions;
	private readonly ChromeManager chrome_manager;
	private readonly WorkspaceManager workspace_manager;
	private readonly RecentFileManager recent_files;
	private readonly ImageConverterManager image_formats;
	internal OpenDocumentAction (
		FileActions fileActions,
		ChromeManager chromeManager,
		WorkspaceManager workspaceManager,
		RecentFileManager recentFiles,
		ImageConverterManager imageFormats)
	{
		file_actions = fileActions;
		chrome_manager = chromeManager;
		workspace_manager = workspaceManager;
		recent_files = recentFiles;
		image_formats = imageFormats;
	}

	void IActionHandler.Initialize ()
	{
		file_actions.Open.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		file_actions.Open.Activated -= Activated;
	}

	private void Activated (object sender, EventArgs e)
	{
		var imagesFilter = CreateImagesFilter ();
		var catchAllFilter = CreateCatchAllFilter ();

		var fcd = Gtk.FileChooserNative.New (
			Translations.GetString ("Open Image File"),
			chrome_manager.MainWindow,
			Gtk.FileChooserAction.Open,
			Translations.GetString ("Open"),
			Translations.GetString ("Cancel"));

		fcd.Modal = true;

		fcd.AddFilter (imagesFilter);
		fcd.AddFilter (catchAllFilter);

		if (recent_files.GetDialogDirectory () is Gio.File dir && dir.QueryExists (null))
			fcd.SetCurrentFolder (dir);

		fcd.SelectMultiple = true;

		fcd.OnResponse += (_, e) => {

			if (e.ResponseId != (int) Gtk.ResponseType.Accept)
				return;

			foreach (var file in fcd.GetFileList ()) {
				if (!workspace_manager.OpenFile (file))
					continue;
				recent_files.AddFile (file);
				var directory = file.GetParent ();
				if (directory is not null)
					recent_files.LastDialogDirectory = directory;
			}
		};

		fcd.Show ();
	}

	private static Gtk.FileFilter CreateCatchAllFilter ()
	{
		Gtk.FileFilter result = Gtk.FileFilter.New ();
		result.Name = Translations.GetString ("All files");
		result.AddPattern ("*");
		return result;
	}

	private Gtk.FileFilter CreateImagesFilter ()
	{
		Gtk.FileFilter result = Gtk.FileFilter.New ();

		result.Name = Translations.GetString ("Image files");

		foreach (var format in image_formats.Formats) {

			if (format.IsWriteOnly ())
				continue;

			foreach (var ext in format.Extensions)
				result.AddPattern ($"*.{ext}");

			// On Unix-like systems, file extensions are often considered optional.
			// Files can often also be identified by their MIME types.
			// Windows does not understand MIME types natively.
			// Adding a MIME filter on Windows would break the native file picker and force a GTK file picker instead.
			if (SystemManager.GetOperatingSystem () != OS.Windows) {
				foreach (var mime in format.Mimes)
					result.AddMimeType (mime);
			}
		}

		return result;
	}
}
