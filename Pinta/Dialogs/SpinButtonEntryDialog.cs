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

using System.Diagnostics.CodeAnalysis;
using Pinta.Core;

namespace Pinta;

[GObject.Subclass<Gtk.Dialog>]
public sealed partial class SpinButtonEntryDialog
{
	private Gtk.Label label;
	private Gtk.SpinButton spin_button;

	[MemberNotNull (nameof (label), nameof (spin_button))]
	partial void Initialize ()
	{
		Gtk.Label labelControl = Gtk.Label.New (null);
		labelControl.Xalign = 0;

		Gtk.SpinButton spinButton = Gtk.SpinButton.NewWithRange (0, 0, 1);
		spinButton.SetActivatesDefaultImmediate (true);

		BoxStyle spacedHorizontal = new (
			orientation: Gtk.Orientation.Horizontal,
			spacing: 6);

		Gtk.Box hbox = GtkExtensions.Box (
			spacedHorizontal,
			[
				labelControl,
				spinButton,
			]);

		// --- Initialization (Gtk.Box)

		Gtk.Box content_area = this.GetContentAreaBox ();
		content_area.SetAllMargins (12);
		content_area.Append (hbox);

		// --- Initialization (Gtk.Window)

		Modal = true;

		// --- Initialization (Gtk.Dialog)

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		// --- References to keep

		label = labelControl;
		spin_button = spinButton;
	}

	public static new SpinButtonEntryDialog New () => NewWithProperties ([]);

	public string LabelText {
		get => label.GetText ();
		set => label.SetText (value);
	}

	public void SetRange (int min, int max)
	{
		spin_button.SetRange (min, max);
	}

	public int Value {
		get => spin_button.GetValueAsInt ();
		set => spin_button.SetValue (value);
	}
}
