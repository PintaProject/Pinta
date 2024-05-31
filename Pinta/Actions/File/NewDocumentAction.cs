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

namespace Pinta.Actions;

internal sealed class NewDocumentAction : IActionHandler
{
	void IActionHandler.Initialize ()
	{
		PintaCore.Actions.File.New.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		PintaCore.Actions.File.New.Activated -= Activated;
	}

	private async void Activated (object sender, EventArgs e)
	{
		int imgWidth;
		int imgHeight;
		NewImageDialog.BackgroundType bg_type;
		bool using_clipboard;

		// Try to get the dimensions of an image on the clipboard
		// for the initial width and height values on the NewImageDialog
		Gdk.Texture? cb_texture = await GdkExtensions.GetDefaultClipboard ().ReadTextureAsync ();
		if (cb_texture is null) {
			// An image was not on the clipboard,
			// so use saved dimensions from settings
			imgWidth = PintaCore.Settings.GetSetting<int> ("new-image-width", 800);
			imgHeight = PintaCore.Settings.GetSetting<int> ("new-image-height", 600);
			bg_type = PintaCore.Settings.GetSetting<NewImageDialog.BackgroundType> (
				"new-image-bg", NewImageDialog.BackgroundType.White);
			using_clipboard = false;
		} else {
			imgWidth = cb_texture.Width;
			imgHeight = cb_texture.Height;
			bg_type = NewImageDialog.BackgroundType.White;
			using_clipboard = true;
		}

		NewImageDialog dialog = new (imgWidth, imgHeight, bg_type, using_clipboard);

		dialog.OnResponse += (_, e) => {

			int response = e.ResponseId;

			if (response == (int) Gtk.ResponseType.Ok) {
				PintaCore.Workspace.NewDocument (new Size (dialog.NewImageWidth, dialog.NewImageHeight), dialog.NewImageBackground);

				PintaCore.Settings.PutSetting ("new-image-width", dialog.NewImageWidth);
				PintaCore.Settings.PutSetting ("new-image-height", dialog.NewImageHeight);
				PintaCore.Settings.PutSetting ("new-image-bg", dialog.NewImageBackgroundType);
			}

			dialog.Destroy ();
		};

		dialog.Present ();
	}
}
