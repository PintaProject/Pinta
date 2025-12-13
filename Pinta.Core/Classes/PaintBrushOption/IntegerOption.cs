//
// IntegerOption.cs
//
// Author:
//       Paul Korecky <https://github.com/spaghetti22>
//
// Copyright (c) 2025 Paul Korecky
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

namespace Pinta.Core;

/// <summary>
/// An integer-valued option that creates a spin button with a set minimum and maximum.
/// </summary>
public class IntegerOption : PaintBrushOption
{
	private Box widget;

	public delegate void ValueChange (long newValue);

	public IntegerOption (long minimum, long maximum, long initialValue, string labelText, ValueChange valueChangeCallback)
	{
		widget = Box.New (Orientation.Horizontal, 0);
		SpinButton spinButton = GtkExtensions.CreateToolBarSpinButton (minimum, maximum, 1, initialValue);
		spinButton.OnValueChanged += (btn, ev) => {
			valueChangeCallback ((long) btn.Value);
		};
		Label label = Label.New (string.Format (" {0}: ", Translations.GetString (labelText)));
		widget.Append (label);
		widget.Append (spinButton);
		valueChangeCallback (initialValue);
	}

	public Widget GetWidget ()
	{
		return widget;
	}

}
