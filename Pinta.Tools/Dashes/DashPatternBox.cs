// 
// DashPatternBox.cs
// 
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2013 Andrew Davis, GSoC 2013
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

namespace Pinta.Tools;

public sealed class DashPatternBox
{
	private bool dash_change_setup = false;

	private Label? dash_pattern_label;
	private Separator? dash_pattern_sep;

	public ToolBarComboBox? ComboBox { get; private set; }

	/// <summary>
	/// Sets up the DashPatternBox in the Toolbar.
	/// 
	/// Note that the dash pattern change event response code must be created manually outside of the DashPatternBox
	/// (using the returned Gtk.ComboBox from the SetupToolbar method) so that each tool that uses it
	/// can react to the change in pattern according to its usage.
	/// 
	/// Returns null if the DashPatternBox has already been setup; otherwise, returns the DashPatternBox itself.
	/// </summary>
	/// <param name="tb">The Toolbar to add the DashPatternBox to.</param>
	/// <returns>null if the DashPatternBox has already been setup; otherwise, returns the DashPatternBox itself.</returns>
	public Gtk.ComboBoxText? SetupToolbar (Box tb)
	{
		dash_pattern_sep ??= GtkExtensions.CreateToolBarSeparator ();

		tb.Append (dash_pattern_sep);

		if (dash_pattern_label == null) {
			var dashString = Translations.GetString ("Dash");
			dash_pattern_label = Label.New ($" {dashString}: ");
		}

		tb.Append (dash_pattern_label);

		ComboBox ??= new ToolBarComboBox (50, 0, true,
				"-", " -", " --", " ---", "  -", "   -", " - --", " - - --------", " - - ---- - ----");

		tb.Append (ComboBox);

		if (dash_change_setup) {
			return null;
		} else {
			dash_change_setup = true;

			return ComboBox.ComboBox;
		}
	}
}
