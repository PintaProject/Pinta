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
using Gtk;

namespace Pinta.Core
{
	public class ToolBarComboBox : ToolItem
	{
		public ComboBox ComboBox { get; private set; }
		public ListStore Model { get; private set; }
		public CellRendererText CellRendererText { get; private set;}

		public ToolBarComboBox (int width, int activeIndex, bool allowEntry, params string[] contents)
		{
			if (allowEntry)
				ComboBox = new ComboBoxEntry (contents);
			else {
				Model = new ListStore (typeof(string), typeof (object));
				if (contents != null) {
					foreach (string entry in contents) {
						Model.AppendValues (entry, null);
					}
				}
				ComboBox = new ComboBox ();
				ComboBox.Model = Model;
				CellRendererText = new CellRendererText();
				ComboBox.PackStart(CellRendererText, false);
				ComboBox.AddAttribute(CellRendererText,"text",0);
			}

			ComboBox.AddEvents ((int)Gdk.EventMask.ButtonPressMask);
			ComboBox.WidthRequest = width;
			
			if (activeIndex >= 0)
				ComboBox.Active = activeIndex;
			
			ComboBox.Show ();
			
			Add (ComboBox);
			Show ();
		}
	}
}