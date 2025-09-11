// 
// ResizePaletteAction.cs
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

internal sealed class ResizePaletteAction : IActionHandler
{
	private readonly EditActions edit;
	private readonly ChromeManager chrome;
	private readonly PaletteManager palette;
	internal ResizePaletteAction (
		EditActions edit,
		ChromeManager chrome,
		PaletteManager palette)
	{
		this.edit = edit;
		this.chrome = chrome;
		this.palette = palette;
	}

	void IActionHandler.Initialize ()
	{
		edit.ResizePalette.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		edit.ResizePalette.Activated -= Activated;
	}

	private async void Activated (object sender, EventArgs e)
	{
		int? response = await PromptResize ();
		if (!response.HasValue) return;
		int newSize = response.Value;
		palette.CurrentPalette.Resize (newSize);
	}

	private async Task<int?> PromptResize ()
	{
		using SpinButtonEntryDialog dialog = new (
			Translations.GetString ("Resize Palette"),
			chrome.MainWindow,
			Translations.GetString ("New palette size:"),
			1,
			96,
			palette.CurrentPalette.Colors.Count);
		try {
			Gtk.ResponseType response = await dialog.RunAsync ();
			if (response != Gtk.ResponseType.Ok) return null;
			return dialog.GetValue ();
		} finally {
			dialog.Destroy ();
		}
	}
}
