// 
// NewDocumentAction.cs
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

namespace Pinta.Actions
{
	class NewDocumentAction : IActionHandler
	{
		#region IActionHandler Members
		public void Initialize ()
		{
			PintaCore.Actions.File.New.Activated += Activated;
		}

		public void Uninitialize ()
		{
			PintaCore.Actions.File.New.Activated -= Activated;
		}
		#endregion

		private void Activated (object sender, EventArgs e)
		{
			int imgWidth = 0;
			int imgHeight = 0;
			var bg_type = NewImageDialog.BackgroundType.White;
            var using_clipboard = true;
			
			// Try to get the dimensions of an image on the clipboard
			// for the initial width and height values on the NewImageDialog
			if (!GetClipboardImageSize (out imgWidth, out imgHeight))
			{
				// An image was not on the clipboard,
				// so use saved dimensions from settings
				imgWidth = PintaCore.Settings.GetSetting<int> ("new-image-width", 800);
				imgHeight = PintaCore.Settings.GetSetting<int> ("new-image-height", 600);
				bg_type = PintaCore.Settings.GetSetting<NewImageDialog.BackgroundType> (
					"new-image-bg", NewImageDialog.BackgroundType.White);
                using_clipboard = false;
            }

			var dialog = new NewImageDialog (imgWidth, imgHeight, bg_type, using_clipboard);

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok) {
				PintaCore.Workspace.NewDocument (new Gdk.Size (dialog.NewImageWidth, dialog.NewImageHeight), dialog.NewImageBackground);

				PintaCore.Settings.PutSetting ("new-image-width", dialog.NewImageWidth);
				PintaCore.Settings.PutSetting ("new-image-height", dialog.NewImageHeight);
				PintaCore.Settings.PutSetting ("new-image-bg", dialog.NewImageBackgroundType);
				PintaCore.Settings.SaveSettings ();
			}

			dialog.Destroy ();
		}

		/// <summary>
		/// Gets the width and height of an image on the clipboard,
		/// if available.
		/// </summary>
		/// <param name="width">Destination for the image width.</param>
		/// <param name="height">Destination for the image height.</param>
		/// <returns>True if dimensions were available, false otherwise.</returns>
		private static bool GetClipboardImageSize (out int width, out int height)
		{
			bool clipboardUsed = false;
			width = height = 0;

			Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			if (cb.WaitIsImageAvailable ())
			{
				Gdk.Pixbuf image = cb.WaitForImage ();
				if (image != null)
				{
					clipboardUsed = true;
					width = image.Width;
					height = image.Height;
					image.Dispose ();
				}
			}

			cb.Dispose ();

			return clipboardUsed;
		}
	}
}
