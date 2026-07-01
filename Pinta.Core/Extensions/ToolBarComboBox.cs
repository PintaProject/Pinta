//
// ToolBarComboBox.cs
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

using System;
using System.Collections.Generic;

namespace Pinta.Core;

[GObject.Subclass<Gtk.Box>]
public sealed partial class ToolBarComboBox
{
	public Gtk.ComboBoxText ComboBox { get; private set; } = null!; // NRT - set in factory method

	public static ToolBarComboBox New (int width, int activeIndex, bool allowEntry)
		=> New (width, activeIndex, allowEntry, []);

	public static ToolBarComboBox New (int width, int activeIndex, bool allowEntry, params string[] contents)
		=> New (width, activeIndex, allowEntry, (IEnumerable<string>) contents);

	public static ToolBarComboBox New (int width, int activeIndex, bool allowEntry, IEnumerable<string> contents)
	{
		Gtk.ComboBoxText comboBox =
			allowEntry
			? Gtk.ComboBoxText.NewWithEntry ()
			: Gtk.ComboBoxText.New ();

		comboBox.CanFocus = allowEntry;

		foreach (string entry in contents)
			comboBox.AppendText (entry);

		comboBox.WidthRequest = width;

		if (activeIndex >= 0)
			comboBox.Active = activeIndex;

		comboBox.OnChanged += OnComboBoxChanged;

		ToolBarComboBox widget = NewWithProperties ([]);
		widget.Append (comboBox);
		widget.ComboBox = comboBox;
		return widget;
	}

	partial void Initialize ()
	{
		Hexpand = false;
		SetOrientation (Gtk.Orientation.Horizontal);
		Spacing = 0;

		// The combobox is inserted by the factory methods.
		// TODO - replace deprecated Gtk.ComboBox with a wrapper around Gtk.Dropdown,
		// and create a separate class for editable dropdowns.
	}

	private static void OnComboBoxChanged (Gtk.ComboBox comboBox, EventArgs __)
	{
		// Return focus to the canvas after selecting a combobox item, which normally focuses the entry widget.
		// We don't want this if the user is actually typing in the entry, of course.

		if (!comboBox.HasEntry)
			return;

		if (!comboBox.GetEntry ().IsEditingText ()) {
			GLib.Functions.IdleAdd (0, () => {
				if (PintaCore.Workspace.HasOpenDocuments)
					PintaCore.Workspace.ActiveWorkspace.GrabFocusToCanvas ();

				return false;
			});
		}
	}
}
