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

using System;
using Gtk;

namespace Pinta.Core;

/// <summary>
/// An integer-valued option that creates a spin button with a set minimum and maximum.
/// </summary>
public class IntegerOption : ToolbarOption
{
	private string name;
	private Box widget;
	private SpinButton spin_button;
	// this is intentionally an "int" and not a "long" because the settings manager
	// only supports System.Int32 - if a wider type is needed, also extend the 
	// settings manager so that settings can be saved there
	private int value;
	private ValueChange value_change_callback;

	public delegate void ValueChange (int newValue);

	public IntegerOption (string name, int minimum, int maximum, int initialValue, string labelText, ValueChange valueChangeCallback)
	{
		this.name = name;
		value_change_callback = valueChangeCallback;
		widget = Box.New (Orientation.Horizontal, 0);
		spin_button = GtkExtensions.CreateToolBarSpinButton (minimum, maximum, 1, initialValue);
		spin_button.OnValueChanged += (btn, ev) => {
			value = (int) btn.Value;
			value_change_callback (value);
		};
		Label label = Label.New (string.Format ("{0}: ", labelText));
		widget.Append (label);
		widget.Append (spin_button);
		valueChangeCallback (initialValue);
	}

	public string GetUniqueName() {
		return name;
	}

	public Widget GetWidget ()
	{
		return widget;
	}

	public object GetValue ()
	{
		return value;
	}

	public void SetValue(object value)
	{
		if (value is int v) {
			this.value = v;
			spin_button.Value = v;
			value_change_callback (this.value);
		} else {
			Console.WriteLine("Unable to set value " + value.ToString() + " for integer toolbar option " + name + ", cannot be cast to int");
		}
	}

}
