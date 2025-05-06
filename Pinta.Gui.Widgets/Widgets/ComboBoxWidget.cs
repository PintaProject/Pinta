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
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class ComboBoxWidget : Gtk.Box
{
	private readonly Gtk.Label title_label;
	private readonly Gtk.ComboBoxText combo_box;

	public ComboBoxWidget (IEnumerable<string> entries)
	{
		const int SPACING = 6;

		Gtk.Label titleLabel = new ();
		titleLabel.AddCssClass (AdwaitaStyles.Title4);

		Gtk.Box labelAndLine = new () { Spacing = SPACING };
		labelAndLine.SetOrientation (Gtk.Orientation.Horizontal);
		labelAndLine.Append (titleLabel);

		Gtk.ComboBoxText comboBox = CreateComboBox (entries);

		// --- Initialization (Gtk.Box)

		SetOrientation (Gtk.Orientation.Vertical);
		Spacing = SPACING;

		Append (labelAndLine);
		Append (comboBox);

		// --- References to keep

		title_label = titleLabel;
		combo_box = comboBox;
	}

	private Gtk.ComboBoxText CreateComboBox (IEnumerable<string> entries)
	{
		Gtk.ComboBoxText result = new ();

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
