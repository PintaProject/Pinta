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
using System.Threading.Tasks;
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
		NewImageDialogOptions options = await GetDialogOptions ();

		NewImageOptions? response = await PromptNewImage (options);

		if (response == null)
			return;

		NewImageOptions newImageOptions = response.Value;

		workspace.NewDocument (
			newImageOptions.NewImageSize,
			newImageOptions.NewImageBackgroundColor);

		settings.PutSetting (SettingNames.NEW_IMAGE_WIDTH, newImageOptions.NewImageSize.Width);
		settings.PutSetting (SettingNames.NEW_IMAGE_HEIGHT, newImageOptions.NewImageSize.Height);
		settings.PutSetting (SettingNames.NEW_IMAGE_BACKGROUND, newImageOptions.NewImageBackgroundType);
	}

	private async Task<NewImageDialogOptions> GetDialogOptions ()
	{
		// Try to get the dimensions of an image on the clipboard
		// for the initial width and height values on the NewImageDialog

		Gdk.Texture? clipboardTexture = await
			GdkExtensions
			.GetDefaultClipboard ()
			.ReadTextureAsync ();

		if (clipboardTexture is not null)
			return new (
				Size: new (
					Width: clipboardTexture.Width,
					Height: clipboardTexture.Height),

				Background: BackgroundType.White,
				UsingClipboard: true);

		// An image was not on the clipboard,
		// so use saved dimensions from settings
		return new (
			Size: new (
				Width: settings.GetSetting<int> (SettingNames.NEW_IMAGE_WIDTH, 800),
				Height: settings.GetSetting<int> (SettingNames.NEW_IMAGE_HEIGHT, 600)),
			Background: settings.GetSetting<BackgroundType> (
				SettingNames.NEW_IMAGE_BACKGROUND,
				BackgroundType.White),
			UsingClipboard: false);
	}

	private async Task<NewImageOptions?> PromptNewImage (NewImageDialogOptions options)
	{
		using NewImageDialog dialog = new (
			chrome,
			palette,
			options.Size,
			options.Background,
			options.UsingClipboard);

		try {
			Gtk.ResponseType response = await dialog.RunAsync ();

			if (response != Gtk.ResponseType.Ok)
				return null;

			return new (
				dialog.NewImageSize,
				dialog.NewImageBackground,
				dialog.NewImageBackgroundType);

		} finally {
			dialog.Destroy ();
		}
	}
}
