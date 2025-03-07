// 
// SpinButtonEntryDialog.cs
//  
// Author:
//       Maia Kozheva <sikon@ubuntu.com>
// 
// Copyright (c) 2010 Maia Kozheva <sikon@ubuntu.com>
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

using Pinta.Core;

namespace Pinta;

public sealed class SpinButtonEntryDialog : Gtk.Dialog
{
	private readonly Gtk.SpinButton spin_button;

	public SpinButtonEntryDialog (
		string title,
		Gtk.Window parent,
		string labelText,
		int min,
		int max,
		int current)
	{
		Gtk.Label labelControl = Gtk.Label.New (labelText);
		labelControl.Xalign = 0;

		Gtk.SpinButton spinButton = Gtk.SpinButton.NewWithRange (min, max, 1);
		spinButton.Value = current;
		spinButton.SetActivatesDefaultImmediate (true);

		Gtk.Box hbox = new () { Spacing = 6 };
		hbox.SetOrientation (Gtk.Orientation.Horizontal);
		hbox.Append (labelControl);
		hbox.Append (spinButton);

		// --- Initialization (Gtk.Box)

		Gtk.Box content_area = this.GetContentAreaBox ();
		content_area.SetAllMargins (12);
		content_area.Append (hbox);

		// --- Initialization (Gtk.Window)

		Title = title;
		TransientFor = parent;
		Modal = true;

		// --- Initialization (Gtk.Dialog)

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		// --- References to keep

		spin_button = spinButton;
	}

	public int GetValue ()
		=> spin_button.GetValueAsInt ();
}
