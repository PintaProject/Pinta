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
	private readonly ActionManager actions;
	private readonly WorkspaceManager workspace;
	private readonly SettingsManager settings;
	private readonly PaletteManager palette;
	private readonly ChromeManager chrome;
	internal NewDocumentAction (
		ActionManager actions,
		ChromeManager chrome,
		PaletteManager palette,
		SettingsManager settings,
		WorkspaceManager workspace)
	{
		this.actions = actions;
		this.chrome = chrome;
		this.palette = palette;
		this.settings = settings;
		this.workspace = workspace;
	}

	void IActionHandler.Initialize ()
	{
		actions.File.New.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		actions.File.New.Activated -= Activated;
	}

	private async void Activated (object sender, EventArgs e)
	{
		Size imageSize;
		NewImageDialog.BackgroundType bg_type;
		bool using_clipboard;

		// Try to get the dimensions of an image on the clipboard
		// for the initial width and height values on the NewImageDialog
		Gdk.Texture? cb_texture = await GdkExtensions.GetDefaultClipboard ().ReadTextureAsync ();
		if (cb_texture is null) {
			// An image was not on the clipboard,
			// so use saved dimensions from settings
			imageSize = new (
				Width: settings.GetSetting<int> (SettingNames.NEW_IMAGE_WIDTH, 800),
				Height: settings.GetSetting<int> (SettingNames.NEW_IMAGE_HEIGHT, 600));
			bg_type = settings.GetSetting<NewImageDialog.BackgroundType> (
				SettingNames.NEW_IMAGE_BACKGROUND,
				NewImageDialog.BackgroundType.White);
			using_clipboard = false;
		} else {
			imageSize = new (
				Width: cb_texture.Width,
				Height: cb_texture.Height);
			bg_type = NewImageDialog.BackgroundType.White;
			using_clipboard = true;
		}

		using NewImageDialog dialog = new (
			chrome,
			palette,
			imageSize,
			bg_type,
			using_clipboard);

		try {
			Gtk.ResponseType response = await dialog.RunAsync ();

			if (response != Gtk.ResponseType.Ok)
				return;

			workspace.NewDocument (
				actions,
				dialog.NewImageSize,
				dialog.NewImageBackground);

			settings.PutSetting (SettingNames.NEW_IMAGE_WIDTH, dialog.NewImageWidth);
			settings.PutSetting (SettingNames.NEW_IMAGE_HEIGHT, dialog.NewImageHeight);
			settings.PutSetting (SettingNames.NEW_IMAGE_BACKGROUND, dialog.NewImageBackgroundType);

		} finally {
			dialog.Destroy ();
		}
	}
}
