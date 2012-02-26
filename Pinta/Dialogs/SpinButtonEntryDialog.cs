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

using Gtk;

namespace Pinta
{
	public class SpinButtonEntryDialog: Dialog
	{
		private SpinButton spinButton;
	
		public SpinButtonEntryDialog (string title, Window parent, string label, int min, int max, int current)
			: base (title, parent, DialogFlags.Modal, Stock.Cancel, ResponseType.Cancel, Stock.Ok, ResponseType.Ok)
		{
			BorderWidth = 6;
			VBox.Spacing = 3;
			HBox hbox = new HBox ();
			hbox.Spacing = 6;
			
			Label lbl = new Label (label);
			lbl.Xalign = 0;
			hbox.PackStart (lbl);
			
			spinButton = new SpinButton (min, max, 1);
			spinButton.Value = current;
			hbox.PackStart (spinButton);
			
			hbox.ShowAll ();
			VBox.Add (hbox);

			AlternativeButtonOrder = new int[] { (int) ResponseType.Ok, (int) ResponseType.Cancel };
			DefaultResponse = ResponseType.Ok;
			spinButton.ActivatesDefault = true;
		}
		
		public int GetValue ()
		{
			return spinButton.ValueAsInt;
		}
	}
}
