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
using Pinta.Core;

namespace Pinta;

public sealed class SpinButtonEntryDialog : Dialog
{
	private readonly SpinButton spin_button;

	public SpinButtonEntryDialog (string title, Window parent, string label, int min, int max, int current)
	{
		Title = title;
		TransientFor = parent;
		Modal = true;
		this.AddCancelOkButtons ();
		this.SetDefaultResponse (ResponseType.Ok);

		var hbox = new Box () { Spacing = 6 };
		hbox.SetOrientation (Orientation.Horizontal);

		var lbl = Label.New (label);
		lbl.Xalign = 0;
		hbox.Append (lbl);

		spin_button = SpinButton.NewWithRange (min, max, 1);
		spin_button.Value = current;
		hbox.Append (spin_button);

		var content_area = this.GetContentAreaBox ();
		content_area.SetAllMargins (12);
		content_area.Append (hbox);

		spin_button.SetActivatesDefaultImmediate (true);
	}

	public int GetValue ()
	{
		return spin_button.GetValueAsInt ();
	}
}
