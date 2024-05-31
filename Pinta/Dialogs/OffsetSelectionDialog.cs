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

using Gtk;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta;

public sealed class OffsetSelectionDialog : Dialog
{
	private readonly HScaleSpinButtonWidget offset_spinbox;

	public int Offset => offset_spinbox.ValueAsInt;

	public OffsetSelectionDialog ()
	{
		Title = Translations.GetString ("Offset Selection");
		TransientFor = PintaCore.Chrome.MainWindow;
		Modal = true;
		this.AddCancelOkButtons ();
		this.SetDefaultResponse (ResponseType.Ok);

		Resizable = false;

		var content_area = this.GetContentAreaBox ();
		content_area.WidthRequest = 400;
		content_area.SetAllMargins (6);
		content_area.Spacing = 6;

		offset_spinbox = new HScaleSpinButtonWidget {
			Label = Translations.GetString ("Offset")
		};
		InitSpinBox (offset_spinbox);
		content_area.Append (offset_spinbox);

		DefaultWidth = 400;
		DefaultHeight = 100;
	}

	private static void InitSpinBox (HScaleSpinButtonWidget spinbox)
	{
		spinbox.DefaultValue = 0;
		spinbox.MaximumValue = 100;
		spinbox.MinimumValue = -100;
	}
}

