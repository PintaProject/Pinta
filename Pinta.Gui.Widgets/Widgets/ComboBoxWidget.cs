// 
// ComboBoxWidget.cs
//  
// Author:
//       Olivier Dufour <olivier.duff@gmail.com>
// 
// Copyright (c) 2010 Olivier Dufour
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
using System.Collections.Generic;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class ComboBoxWidget : Box
{
	private readonly Label label;
	private readonly ComboBoxText combobox;

	public ComboBoxWidget (IEnumerable<string> entries)
	{
		const int spacing = 6;

		// Section label + line
		var hbox1 = new Box { Spacing = spacing };
		hbox1.SetOrientation (Orientation.Horizontal);

		label = new Label ();
		label.AddCssClass (AdwaitaStyles.Title4);
		hbox1.Append (label);

		// Combobox
		combobox = new ComboBoxText ();

		// Main layout
		// Main layout
		SetOrientation (Orientation.Vertical);
		Spacing = spacing;
		Append (hbox1);
		Append (combobox);

		foreach (var s in entries)
			combobox.AppendText (s);

		combobox.OnChanged += (_, _) => Changed?.Invoke (this, EventArgs.Empty);
	}

	public string Label {
		get => label.GetText ();
		set => label.SetText (value);
	}

	public int Active {
		get => combobox.Active;
		set => combobox.Active = value;
	}

	public string ActiveText => combobox.GetActiveText ()!;

	public event EventHandler? Changed;
}
