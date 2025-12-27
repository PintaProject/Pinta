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

namespace Pinta.Core;

/// <summary>
/// An integer-valued option that creates a spin button with a set minimum and maximum.
/// </summary>
public class IntegerOption : ToolOption
{
	private string name;

	// this is intentionally an "int" and not a "long" because the settings manager
	// only supports System.Int32 - if a wider type is needed, also extend the 
	// settings manager so that settings can be saved there
	private int value;
	public int Minimum { get; private set; }
	public int Maximum { get; private set; }
	public string LabelText { get; private set; }

	public int Value {
		get => value;
		set {
			this.value = value;
			OnValueChanged?.Invoke (Value);
		}
	}

	public delegate void ValueChange (int newValue);
	public event ValueChange? OnValueChanged;

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
		Value = initialValue;
	}

	public string GetUniqueName ()
	{
		return name;
	}

	public void LoadValueFromSettings (ISettingsService settingsService)
	{
		int invalidValue = Minimum - 1;
		int savedValue = settingsService.GetSetting<int> (name, invalidValue);
		if (savedValue != invalidValue) {
			Value = savedValue;
		}
	}

	public void SaveValueToSettings (ISettingsService settingsService)
	{
		settingsService.PutSetting (name, Value);
	}
}
