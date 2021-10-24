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

namespace Pinta.Gui.Widgets
{
	public class ReseedButtonWidget : FilledAreaBin
	{
		private Button button;

		public event EventHandler? Clicked;

		public ReseedButtonWidget ()
		{
			Build ();

			button.Clicked += (_, _) => Clicked?.Invoke (this, EventArgs.Empty);
		}

		[MemberNotNull (nameof (button))]
		private void Build ()
		{
			// Section label + line
			var hbox1 = new HBox (false, 6);

			var label = new Label {
				LabelProp = Pinta.Core.Translations.GetString ("Random Noise")
			};

			hbox1.PackStart (label, false, false, 0);
			hbox1.PackStart (new HSeparator (), true, true, 0);

			// Reseed button
			button = new Button {
				WidthRequest = 88,
				CanFocus = true,
				UseUnderline = true,
				Label = Pinta.Core.Translations.GetString ("Reseed")
			};

			var hbox2 = new HBox (false, 6);

			hbox2.PackStart (button, false, false, 0);

			// Main layout
			var vbox = new VBox (false, 6) {
				hbox1,
				hbox2
			};

			Add (vbox);

			vbox.ShowAll ();
		}
	}
}

