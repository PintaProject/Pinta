// 
// ReseedButtonWidget.cs
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
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class ReseedButtonWidget : Gtk.Box
{
	private readonly Gtk.Label section_label;
	private readonly Gtk.Button reseed_button;

	public event EventHandler? Clicked;

	public ReseedButtonWidget ()
	{
		const int spacing = 6;

		Gtk.Label sectionLabel = CreateSectionLabel ();
		Gtk.Button reseedButton = CreateReseedButton ();

		// Main layout
		SetOrientation (Gtk.Orientation.Vertical);
		Spacing = spacing;
		Append (sectionLabel);
		Append (reseedButton);

		// --- References to keep

		section_label = sectionLabel;
		reseed_button = reseedButton;
	}

	private Gtk.Button CreateReseedButton ()
	{
		Gtk.Button result = new () {
			WidthRequest = 88,
			CanFocus = true,
			UseUnderline = true,
			Label = Translations.GetString ("Reseed"),
			Hexpand = false,
			Halign = Gtk.Align.Start,
		};

		result.OnClicked += (_, _) => Clicked?.Invoke (this, EventArgs.Empty);

		return result;
	}

	private static Gtk.Label CreateSectionLabel ()
	{
		Gtk.Label result = new ();
		result.AddCssClass (AdwaitaStyles.Title4);
		result.Hexpand = false;
		result.Halign = Gtk.Align.Start;
		return result;
	}

	public string Label {
		get => section_label.GetText ();
		set => section_label.SetText (value);
	}
}

