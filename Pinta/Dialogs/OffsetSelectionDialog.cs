//
// OffsetSelectionDialog.cs
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

using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta;

[GObject.Subclass<Gtk.Dialog>]
public sealed partial class OffsetSelectionDialog
{
	private readonly HScaleSpinButtonWidget offset_spinbox = new (initialValue: 0);

	public int Offset
		=> offset_spinbox.ValueAsInt;

	public static OffsetSelectionDialog New (IChromeService chrome)
	{
		OffsetSelectionDialog dialog = NewWithProperties ([]);
		dialog.TransientFor = chrome.MainWindow;
		return dialog;
	}

	partial void Initialize ()
	{
		DefaultWidth = 400;
		DefaultHeight = 100;

		Title = Translations.GetString ("Offset Selection");
		Modal = true;

		Resizable = false;

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		offset_spinbox.Label = Translations.GetString ("Offset");
		offset_spinbox.MinimumValue = -100;
		offset_spinbox.MaximumValue = 100;

		Gtk.Box contentArea = this.GetContentAreaBox ();
		contentArea.WidthRequest = 400;
		contentArea.SetAllMargins (6);
		contentArea.Spacing = 6;
		contentArea.Append (offset_spinbox);
	}
}

