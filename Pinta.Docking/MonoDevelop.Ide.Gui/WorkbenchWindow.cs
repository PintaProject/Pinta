// 
// WorkbenchWindow.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using Pinta.Docking;

namespace Pinta.Docking.Gui
{
	public class WorkbenchWindow: Gtk.Window
	{
		List<TopLevelChild> topLevels = new List<TopLevelChild> ();
		
		public WorkbenchWindow (): base (Gtk.WindowType.Toplevel)
		{
			GtkWorkarounds.FixContainerLeak (this);
		}

		class TopLevelChild
		{
			public int X;
			public int Y;
			public Gtk.Widget Child;
		}
		
		public void AddTopLevelWidget (Gtk.Widget w, int x, int y)
		{
			w.Parent = this;
			TopLevelChild info = new TopLevelChild ();
			info.X = x;
			info.Y = y;
			info.Child = w;
			topLevels.Add (info);
		}
		
		public void RemoveTopLevelWidget (Gtk.Widget w)
		{
			foreach (TopLevelChild info in topLevels) {
				if (info.Child == w) {
					w.Unparent ();
					topLevels.Remove (info);
					break;
				}
			}
		}
		
		public void MoveTopLevelWidget (Gtk.Widget w, int x, int y)
		{
			foreach (TopLevelChild info in topLevels) {
				if (info.Child == w) {
					info.X = x;
					info.Y = y;
					QueueResize ();
					break;
				}
			}
		}
		
		public Gdk.Rectangle GetTopLevelPosition (Gtk.Widget w)
		{
			foreach (TopLevelChild info in topLevels) {
				if (info.Child == w) {
					Gtk.Requisition req = w.SizeRequest ();
					return new Gdk.Rectangle (info.X, info.Y, req.Width, req.Height);
				}
			}
			return new Gdk.Rectangle (0,0,0,0);
		}
		
		public Gdk.Rectangle GetCoordinates (Gtk.Widget w)
		{
			int px, py;
			if (!w.TranslateCoordinates (this, 0, 0, out px, out py))
				return new Gdk.Rectangle (0,0,0,0);

			Gdk.Rectangle rect = w.Allocation;
			rect.X = px - Allocation.X;
			rect.Y = py - Allocation.Y;
			return rect;
		}
		
		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			
			// Ignore the size of top levels. They are supposed to fit the available space
			foreach (TopLevelChild tchild in topLevels)
				tchild.Child.SizeRequest ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			
			foreach (TopLevelChild child in topLevels) {
				Gtk.Requisition req = child.Child.SizeRequest ();
				child.Child.SizeAllocate (new Gdk.Rectangle (allocation.X + child.X, allocation.Y + child.Y, req.Width, req.Height));
			}
		}
		
		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			if (Child != null)
				callback (Child);
			foreach (TopLevelChild child in topLevels)
				callback (child.Child);
		}
	}
}
