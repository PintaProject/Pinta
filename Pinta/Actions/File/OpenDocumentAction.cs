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
using Gtk;
using Pinta.Core;

namespace Pinta.Actions;

internal sealed class OpenDocumentAction : IActionHandler
{
	#region IActionHandler Members
	public void Initialize ()
	{
		PintaCore.Actions.File.Open.Activated += Activated;
	}

	public void Uninitialize ()
	{
		PintaCore.Actions.File.Open.Activated -= Activated;
	}
	#endregion

	private void Activated (object sender, EventArgs e)
	{
		var fcd = FileChooserNative.New (
			Translations.GetString ("Open Image File"),
			PintaCore.Chrome.MainWindow,
			FileChooserAction.Open,
			Translations.GetString ("Open"),
			Translations.GetString ("Cancel"));
		fcd.Modal = true;


		// Add image files filter
		var ff = FileFilter.New ();
		ff.Name = Translations.GetString ("Image files");

		foreach (var format in PintaCore.ImageFormats.Formats) {

			if (format.IsWriteOnly ())
				continue;

			foreach (var ext in format.Extensions)
				ff.AddPattern ($"*.{ext}");

			// On Unix-like systems, file extensions are often considered optional.
			// Files can often also be identified by their MIME types.
			// Windows does not understand MIME types natively.
			// Adding a MIME filter on Windows would break the native file picker and force a GTK file picker instead.
			if (SystemManager.GetOperatingSystem () != OS.Windows) {
				foreach (var mime in format.Mimes)
					ff.AddMimeType (mime);
			}
		}

		fcd.AddFilter (ff);

		var ff2 = FileFilter.New ();
		ff2.Name = Translations.GetString ("All files");
		ff2.AddPattern ("*");
		fcd.AddFilter (ff2);

		if (PintaCore.RecentFiles.GetDialogDirectory () is Gio.File dir && dir.QueryExists (null))
			fcd.SetCurrentFolder (dir);

		fcd.SelectMultiple = true;

		fcd.OnResponse += (_, e) => {
			if (e.ResponseId != (int) Gtk.ResponseType.Accept)
				return;
			foreach (var file in fcd.GetFileList ()) {
				if (!PintaCore.Workspace.OpenFile (file))
					continue;
				PintaCore.RecentFiles.AddFile (file);
				var directory = file.GetParent ();
				if (directory is not null)
					PintaCore.RecentFiles.LastDialogDirectory = directory;
			}
		};

		fcd.Show ();
	}
}
