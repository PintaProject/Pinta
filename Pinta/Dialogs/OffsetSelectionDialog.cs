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

public sealed class OffsetSelectionDialog : Gtk.Dialog
{
	private readonly HScaleSpinButtonWidget offset_spinbox;

	public int Offset
		=> offset_spinbox.ValueAsInt;

	public OffsetSelectionDialog (ChromeManager chrome)
	{
		DefaultWidth = 400;
		DefaultHeight = 100;

		Title = Translations.GetString ("Offset Selection");
		TransientFor = chrome.MainWindow;
		Modal = true;

		Resizable = false;

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		offset_spinbox = new HScaleSpinButtonWidget (0) {
			Label = Translations.GetString ("Offset"),
			MaximumValue = 100,
			MinimumValue = -100,
		};

		Gtk.Box content_area = this.GetContentAreaBox ();
		content_area.WidthRequest = 400;
		content_area.SetAllMargins (6);
		content_area.Spacing = 6;
		content_area.Append (offset_spinbox);
	}
}

