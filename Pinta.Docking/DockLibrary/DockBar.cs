//
// DockBar.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using Gtk;
using System.Collections.Generic;

namespace MonoDevelop.Components.Docking
{
	public class DockBar: Gtk.EventBox
	{
		Gtk.PositionType position;
		Box box;
		DockFrame frame;
		Label filler;
		bool alwaysVisible;
		
		internal DockBar (DockFrame frame, Gtk.PositionType position)
		{
			frame.ShadedContainer.Add (this);
			VisibleWindow = false;
			this.frame = frame;
			this.position = position;
			Gtk.Alignment al = new Alignment (0,0,0,0);
			if (Orientation == Gtk.Orientation.Horizontal)
				box = new HBox ();
			else
				box = new VBox ();
			
			uint sizePadding = 1;
			uint startPadding = 6;
			switch (Frame.CompactGuiLevel) {
				case 1: sizePadding = 2; break;
				case 4: startPadding = 3; break;
				case 5: startPadding = 0; sizePadding = 0; break;
			}
			
			switch (position) {
				case PositionType.Top: al.BottomPadding = sizePadding; al.LeftPadding = al.RightPadding = startPadding; break;
				case PositionType.Bottom: al.TopPadding = sizePadding; al.LeftPadding = al.RightPadding = startPadding; break;
				case PositionType.Left: al.RightPadding = sizePadding; al.TopPadding = al.BottomPadding = startPadding; break;
				case PositionType.Right: al.LeftPadding = sizePadding; al.TopPadding = al.BottomPadding = startPadding; break;
			}
			
			box.Spacing = 3;
			al.Add (box);
			Add (al);
			
			filler = new Label ();
			filler.WidthRequest = 4;
			filler.HeightRequest = 4;
			box.PackEnd (filler);
			
			ShowAll ();
			UpdateVisibility ();
		}
		
		public bool IsExtracted {
			get { return OriginalBar != null; }
		}
		
		internal DockBar OriginalBar { get; set; }
		
		public bool AlwaysVisible {
			get { return this.alwaysVisible; }
			set { this.alwaysVisible = value; UpdateVisibility (); }
		}
		
		
		internal Gtk.Orientation Orientation {
			get {
				return (position == PositionType.Left || position == PositionType.Right) ? Gtk.Orientation.Vertical : Gtk.Orientation.Horizontal;
			}
		}
		
		internal Gtk.PositionType Position {
			get {
				return position;
			}
		}

		internal DockFrame Frame {
			get {
				return frame;
			}
		}
		
		internal DockBarItem AddItem (DockItem item, int size)
		{
			DockBarItem it = new DockBarItem (this, item, size);
			box.PackStart (it, false, false, 0);
			it.ShowAll ();
			UpdateVisibility ();
			it.Shown += OnItemVisibilityChanged;
			it.Hidden += OnItemVisibilityChanged;
			return it;
		}
		
		void OnItemVisibilityChanged (object o, EventArgs args)
		{
			UpdateVisibility ();
		}
		
		internal void OnCompactLevelChanged ()
		{
			UpdateVisibility ();
			if (OriginalBar != null)
				OriginalBar.UpdateVisibility ();
		}
		
		internal void UpdateVisibility ()
		{
			filler.Visible = (Frame.CompactGuiLevel < 3);
			int visibleCount = 0;
			foreach (Gtk.Widget w in box.Children) {
				if (w.Visible)
					visibleCount++;
			}
			Visible = alwaysVisible || filler.Visible || visibleCount > 0;
		}
		
		internal void RemoveItem (DockBarItem it)
		{
			box.Remove (it);
			it.Shown -= OnItemVisibilityChanged;
			it.Hidden -= OnItemVisibilityChanged;
			UpdateVisibility ();
		}
		
		internal void UpdateTitle (DockItem item)
		{
			foreach (Widget w in box.Children) {
				DockBarItem it = w as DockBarItem;
				if (it != null && it.DockItem == item) {
					it.UpdateTab ();
					break;
				}
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			frame.ShadedContainer.DrawBackground (this);
			return base.OnExposeEvent (evnt);
		}
	}
}

