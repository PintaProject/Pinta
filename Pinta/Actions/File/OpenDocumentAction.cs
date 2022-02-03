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

namespace Pinta.Actions
{
	class OpenDocumentAction : IActionHandler
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
			using var fcd = new FileChooserNative (
				Translations.GetString ("Open Image File"),
				PintaCore.Chrome.MainWindow,
				FileChooserAction.Open,
				Translations.GetString ("Open"),
				Translations.GetString ("Cancel"));

			// Add image files filter
			var ff = new FileFilter {
				Name = Translations.GetString ("Image files")
			};

			foreach (var format in PintaCore.System.ImageFormats.Formats) {
				if (!format.IsWriteOnly ()) {
					foreach (var ext in format.Extensions)
						ff.AddPattern (string.Format ("*.{0}", ext));
				}
			}

			fcd.AddFilter (ff);

			var ff2 = new FileFilter {
				Name = Translations.GetString ("All files")
			};
			ff2.AddPattern ("*");
			fcd.AddFilter (ff2);

			fcd.SetCurrentFolderFile (PintaCore.System.GetDialogDirectory ());
			fcd.SelectMultiple = true;
			fcd.LocalOnly = false;

			var response = (ResponseType) fcd.Run ();

			if (response == ResponseType.Accept) {
				PintaCore.System.LastDialogDirectory = fcd.CurrentFolderFile;

				foreach (var file in fcd.Files) {
					if (PintaCore.Workspace.OpenFile (file)) {
						RecentManager.Default.AddFull (file.Uri.ToString (), PintaCore.System.RecentData);

						var directory = file.Parent;
						if (directory is not null) {
							PintaCore.System.LastDialogDirectory = directory;
						}
					}
				}
			}
		}
	}
}
