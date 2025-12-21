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
using System.Collections.Generic;

namespace Pinta.Core;

/// <summary>
/// An integer-valued option that creates a spin button with a set minimum and maximum.
/// </summary>
public class IntegerOption : ToolOption
{
	private string name;
	public int Minimum { get; private set; }
	public int Maximum { get; private set; }
	public string LabelText { get; private set; }

	// this is intentionally an "int" and not a "long" because the settings manager
	// only supports System.Int32 - if a wider type is needed, also extend the 
	// settings manager so that settings can be saved there
	private int value;
	private List<ValueChange> value_change_callbacks;
	private bool saved_value_already_loaded;

	public delegate void ValueChange (int newValue);

	/// <summary>
	/// Construct a new integer option.
	/// </summary>
	/// <param name="name">
	/// Application-wide unique name of the option, must not collide with name
	/// of any setting in settings.xml.
	/// </param>
	/// <param name="minimum">
	/// Minimum possible value the user should be allowed to set.
	/// </param>
	/// <param name="maximum">
	/// Maximum possible value the user should be allowed to set.
	/// </param>
	/// <param name="initialValue">
	/// Initial value the option should have if none can be found in the
	/// settings.
	/// </param>
	/// <param name="labelText">
	/// Text that should be displayed in the UI to label the option.
	/// This class does not do any translation, so the translation method
	/// needs to be called by the calling code.
	/// </param>
	public IntegerOption (string name, int minimum, int maximum, int initialValue, string labelText)
	{
		this.name = name;
		Minimum = minimum;
		Maximum = maximum;
		LabelText = labelText;
		value_change_callbacks = [];
		saved_value_already_loaded = false;
		SetValue (initialValue);
	}

	public string GetUniqueName ()
	{
		return name;
	}

	public object GetValue ()
	{
		return value;
	}

	public void SetValue (object value)
	{
		if (value is int v) {
			this.value = v;
			foreach (var callback in value_change_callbacks) {
				callback (this.value);
			}
		} else {
			Console.WriteLine ("Unable to set value " + value.ToString () + " for integer toolbar option " + name + ", cannot be cast to int");
		}
	}

	public void SetSavedValue (ISettingsService settingsService)
	{
		if (saved_value_already_loaded) {
			return;
		}
		int invalidValue = Minimum - 1;
		int savedValue = settingsService.GetSetting<int> (name, invalidValue);
		if (savedValue != invalidValue) {
			SetValue (savedValue);
		}
		saved_value_already_loaded = true;
	}

	/// <summary>
	/// Register a method that should be called when the value has changed.
	/// </summary>
	/// <param name="valueChangeCallback">
	/// Method that takes a single integer and returns nothing that will
	/// afterwards be called whenever the value of the option changes.
	/// </param>
	public void RegisterValueChangeCallback (ValueChange valueChangeCallback)
	{
		value_change_callbacks.Add (valueChangeCallback);
	}

}
