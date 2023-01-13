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
using System.Diagnostics.CodeAnalysis;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class ReseedButtonWidget : Box
	{
		private Button button;

		public event EventHandler? Clicked;

		public ReseedButtonWidget ()
		{
			Build ();

			button.OnClicked += (_, _) => Clicked?.Invoke (this, EventArgs.Empty);
		}

		[MemberNotNull (nameof (button))]
		private void Build ()
		{
			const int spacing = 6;

			// Section label + line
			var label = Label.New (Pinta.Core.Translations.GetString ("Random Noise"));
			label.AddCssClass (AdwaitaStyles.Title4);
			label.Hexpand = false;
			label.Halign = Align.Start;

			// Reseed button
			button = new Button {
				WidthRequest = 88,
				CanFocus = true,
				UseUnderline = true,
				Label = Pinta.Core.Translations.GetString ("Reseed"),
				Hexpand = false,
				Halign = Align.Start
			};

			// Main layout
			Orientation = Orientation.Vertical;
			Spacing = spacing;
			Append (label);
			Append (button);
		}
	}
}

