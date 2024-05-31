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
	private readonly Label title_label;
	private readonly ComboBoxText combo_box;

	public ComboBoxWidget (IEnumerable<string> entries)
	{
		const int spacing = 6;

		// Section label + line
		Label titleLabel = new ();
		titleLabel.AddCssClass (AdwaitaStyles.Title4);
		Box labelAndLine = new () { Spacing = spacing };
		labelAndLine.SetOrientation (Orientation.Horizontal);
		labelAndLine.Append (titleLabel);

		// Combobox
		ComboBoxText comboBox = CreateComboBox (entries);

		// Storing references
		title_label = titleLabel;
		combo_box = comboBox;

		// Main layout
		SetOrientation (Orientation.Vertical);
		Spacing = spacing;
		Append (labelAndLine);
		Append (comboBox);
	}

	private ComboBoxText CreateComboBox (IEnumerable<string> entries)
	{
		ComboBoxText result = new ();

		foreach (var s in entries)
			result.AppendText (s);

		result.OnChanged += (_, _) => Changed?.Invoke (this, EventArgs.Empty);

		return result;
	}

	public string Label {
		get => title_label.GetText ();
		set => title_label.SetText (value);
	}

	public int Active {
		get => combo_box.Active;
		set => combo_box.Active = value;
	}

	public string ActiveText
		=> combo_box.GetActiveText ()!;

	public event EventHandler? Changed;
}
