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

public sealed class ToolBarComboBox : Gtk.Box
{
	public Gtk.ComboBoxText ComboBox { get; }

	public ToolBarComboBox (int width, int activeIndex, bool allowEntry)
		: this (width, activeIndex, allowEntry, [])
	{ }

	public ToolBarComboBox (int width, int activeIndex, bool allowEntry, params string[] contents)
		: this (width, activeIndex, allowEntry, (IEnumerable<string>) contents)
	{ }

	public ToolBarComboBox (int width, int activeIndex, bool allowEntry, IEnumerable<string> contents)
	{
		Gtk.ComboBoxText comboBox =
			allowEntry
			? Gtk.ComboBoxText.NewWithEntry ()
			: new ();

		comboBox.CanFocus = allowEntry;

		foreach (string entry in contents)
			comboBox.AppendText (entry);

		comboBox.WidthRequest = width;

		if (activeIndex >= 0)
			comboBox.Active = activeIndex;

		comboBox.OnChanged += ComboBox_OnChanged;

		// --- Initialization (Gtk.Widget)

		Hexpand = false;

		// --- Initialization (Gtk.Box)

		SetOrientation (Gtk.Orientation.Horizontal);
		Spacing = 0;

		Append (comboBox);

		// --- References to keep

		ComboBox = comboBox;
	}

	private void ComboBox_OnChanged (Gtk.ComboBox sender, EventArgs args)
	{
		// Return focus to the canvas after selecting a combobox item, which normally focuses the entry widget.
		// We don't want this if the user is actually typing in the entry, of course.

		if (!sender.HasEntry)
			return;

		Gtk.Widget? entryText = sender.GetEntry ().GetFirstChild ();

		if (entryText is null) {
			Console.Error.WriteLine ("Failed to find child text widget for Gtk.Entry");
			return;
		}

		if (!entryText.HasFocus) {
			GLib.Functions.IdleAdd (0, () => {
				if (PintaCore.Workspace.HasOpenDocuments)
					PintaCore.Workspace.ActiveWorkspace.GrabFocusToCanvas ();

				return false;
			});
		}
	}
}
