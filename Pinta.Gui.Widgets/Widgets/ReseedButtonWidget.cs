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
using Gtk;

namespace Pinta.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem(true)]
	public class ReseedButtonWidget : FilledAreaBin
	{
        private Button button1;

		public ReseedButtonWidget ()
		{
			Build ();
			
			button1.Clicked += delegate (object sender, EventArgs e) {
				OnClicked ();
			};
		}

		#region Protected Methods
		protected void OnClicked ()
		{
			if (Clicked != null)
				Clicked (this, EventArgs.Empty);
		}
		#endregion

		#region Public Events
		public event EventHandler Clicked;
        #endregion

        private void Build ()
        {
            // Section label + line
            var hbox1 = new HBox (false, 6);

            var label = new Label ();
            label.LabelProp = Mono.Unix.Catalog.GetString ("Random Noise");

            hbox1.PackStart (label, false, false, 0);
            hbox1.PackStart (new HSeparator (), true, true, 0);

            // Reseed button
            button1 = new Button ();
            button1.WidthRequest = 88;
            button1.CanFocus = true;
            button1.UseUnderline = true;
            button1.Label = Mono.Unix.Catalog.GetString ("Reseed");

            var hbox2 = new HBox (false, 6);
            hbox2.PackStart (button1, false, false, 0);

            // Main layout
            var vbox = new VBox (false, 6);

            vbox.Add (hbox1);
            vbox.Add (hbox2);

            Add (vbox);

            vbox.ShowAll ();
        }
    }
}

