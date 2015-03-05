// 
// DockItemToolbar.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Components.Docking
{
	public class DockItemToolbar
	{
		DockItem parentItem;
		Gtk.Widget frame;
		Box box;
		PositionType position;
		bool empty = true;
		CustomFrame topFrame;
		
		internal DockItemToolbar (DockItem parentItem, PositionType position)
		{
			this.parentItem = parentItem;

			topFrame = new CustomFrame ();
			topFrame.SetPadding (3,3,3,3);

/*			switch (position) {
				case PositionType.Top:
					frame.SetMargins (0, 0, 1, 1); 
					frame.SetPadding (0, 2, 2, 0); 
					break;
				case PositionType.Bottom:
					frame.SetMargins (0, 1, 1, 1);
					frame.SetPadding (2, 2, 2, 0); 
					break;
				case PositionType.Left:
					frame.SetMargins (0, 1, 1, 0);
					frame.SetPadding (0, 0, 2, 2); 
					break;
				case PositionType.Right:
					frame.SetMargins (0, 1, 0, 1);
					frame.SetPadding (0, 0, 2, 2); 
					break;
			}*/

			this.position = position;
			if (position == PositionType.Top || position == PositionType.Bottom)
				box = new HBox (false, 3);
			else
				box = new VBox (false, 3);
			box.Show ();
//			frame = box;
			frame = topFrame;
			topFrame.Add (box);

//			topFrame.GradientBackround = true;
		}

		internal void SetStyle (DockVisualStyle style)
		{
			topFrame.BackgroundColor = style.PadBackgroundColor.Value;
		}

		public DockItem DockItem {
			get { return parentItem; }
		}
		
		internal Widget Container {
			get { return frame; }
		}
		
		public PositionType Position {
			get { return this.position; }
		}
		
		public void Add (Widget widget)
		{
			Add (widget, false);
		}
		
		public void Add (Widget widget, bool fill)
		{
			Add (widget, fill, -1);
		}
		
		public void Add (Widget widget, bool fill, int padding)
		{
			Add (widget, fill, padding, -1);
		}
		
		void Add (Widget widget, bool fill, int padding, int index)
		{
			int defaultPadding = 3;
			
			if (widget is Button) {
				((Button)widget).Relief = ReliefStyle.None;
				((Button)widget).FocusOnClick = false;
				defaultPadding = 0;
			}
			else if (widget is Entry) {
				((Entry)widget).HasFrame = false;
			}
			else if (widget is ComboBox) {
				((ComboBox)widget).HasFrame = false;
			}
			else if (widget is VSeparator)
				((VSeparator)widget).HeightRequest = 10;
			
			if (padding == -1)
				padding = defaultPadding;
			
			box.PackStart (widget, fill, fill, (uint)padding);
			if (empty) {
				empty = false;
				frame.Show ();
			}
			if (index != -1) {
				Box.BoxChild bc = (Box.BoxChild) box [widget];
				bc.Position = index;
			}
		}
		
		public void Insert (Widget w, int index)
		{
			Add (w, false, 0, index);
		}
		
		public void Remove (Widget widget)
		{
			box.Remove (widget);
		}
		
		public bool Visible {
			get {
				return empty || frame.Visible;
			}
			set {
				frame.Visible = value;
			}
		}
		
		public bool Sensitive {
			get { return frame.Sensitive; }
			set { frame.Sensitive = value; }
		}
		
		public void ShowAll ()
		{
			frame.ShowAll ();
		}
		
		public Widget[] Children {
			get { return box.Children; }
		}
	}
	
	public class DockToolButton: Gtk.Button
	{
		public DockToolButton (string stockId)
		{
			Image = new Gtk.Image (stockId, IconSize.Menu);
			Image.Show ();
		}
		
		public DockToolButton (string stockId, string label)
		{
			Label = label;
			Image = new Gtk.Image (stockId, IconSize.Menu);
			Image.Show ();
		}
	}
}

