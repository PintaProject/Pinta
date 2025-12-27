//
// ToolOptionWidgetService.cs
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

using System.Collections.Generic;
using Gtk;
using Pinta.Core;

namespace Pinta.Tools;

/// <summary>
/// This class manages the widgets belonging to tool options.
/// </summary>
public static class ToolOptionWidgetService
{
	private static Dictionary<ToolOption, Widget> tool_option_widgets = new Dictionary<ToolOption, Widget> ();

	/// <summary>
	/// For provided tool option, either create the appropriate GTK widget, or if
	/// it has already been created, retrieve it.
	/// </summary>
	/// <param name="toolOption">
	/// Method that takes a single integer and returns nothing that will
	/// afterwards be called whenever the value of the option changes.
	/// </param>
	/// <returns>
	/// A new or previously created GTK widget that can be displayed to the
	/// user in order to set the option.
	/// </returns>
	public static Widget GetWidgetForOption (ToolOption toolOption)
	{
		if (tool_option_widgets.TryGetValue (toolOption, out var widget)) {
			return widget;
		}
		Box box = Box.New (Orientation.Horizontal, 0);
		if (toolOption is IntegerOption integerOption) {
			int optionValue = integerOption.Value;
			SpinButton spin_button = GtkExtensions.CreateToolBarSpinButton (integerOption.Minimum, integerOption.Maximum, 1, optionValue);
			spin_button.OnValueChanged += (btn, ev) => {
				integerOption.Value = (int) spin_button.Value;
			};
			integerOption.OnValueChanged += v => spin_button.Value = v;
			Label label = Label.New (string.Format ("{0}: ", integerOption.LabelText));
			box.Append (label);
			box.Append (spin_button);
			integerOption.Value = optionValue; // to force callbacks to be called
		}
		tool_option_widgets.Add (toolOption, box);
		return box;
	}
}
