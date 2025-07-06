//
// PasteIntoNewImageAction.cs
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

internal sealed class PasteIntoNewImageAction : IActionHandler
{
	private readonly ActionManager actions;
	private readonly ChromeManager chrome;
	private readonly WorkspaceManager workspace;
	internal PasteIntoNewImageAction (
		ActionManager actions,
		ChromeManager chrome,
		WorkspaceManager workspace)
	{
		this.actions = actions;
		this.chrome = chrome;
		this.workspace = workspace;
	}

	void IActionHandler.Initialize ()
	{
		actions.Edit.PasteIntoNewImage.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		actions.Edit.PasteIntoNewImage.Activated -= Activated;
	}

	private async void Activated (object sender, EventArgs e)
	{
		var cb = GdkExtensions.GetDefaultClipboard ();
		Gdk.Texture? cb_texture = await cb.ReadTextureAsync ();

		if (cb_texture is not null)
			workspace.NewDocumentFromImage (cb_texture.ToSurface ());
		else
			await PasteAction.ShowClipboardEmptyDialog (chrome);
	}
}
