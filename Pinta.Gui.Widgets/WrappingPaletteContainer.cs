// 
// WrappingPaletteContainer.cs
//  
// Author:
//       Don McComb <don.mccomb@gmail.com>
// 
// Copyright (c) 2014 Don McComb
//
// This class is loosely based on the FlowLayoutContainer found in the Ribbons
// Mono project created during the Google Summer of Code, 2007.
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
using System.Linq;
using System.Text;
using Gtk;

namespace Pinta.Gui.Widgets
{
    public class WrappingPaletteContainer : Container
    {
        private List<Widget> children;
		private Requisition[] childReqs;
        int iconSize = 16;

		/// <summary>Returns the number of children.</summary>
		public int NChildren
		{
			get { return children.Count; }
		}

		/// <summary>Default constructor.</summary>
        public WrappingPaletteContainer(int iconSize)
		{
			this.SetFlag (WidgetFlags.NoWindow);

			this.AddEvents ((int)(Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask));

			this.children = new List<Widget> ();

            this.iconSize = iconSize;
		}

		/// <summary>Adds a widget before all existing widgetw.</summary>
		/// <param name="w">The widget to add.</param>
		public void Prepend (Widget w)
		{
			Insert (w, 0);
		}

		/// <summary>Adds a widget after all existing widgets.</summary>
		/// <param name="w">The widget to add.</param>
		public void Append (Widget w)
		{
			Insert (w, -1);
		}

		/// <summary>Inserts a widget at the specified location.</summary>
		/// <param name="w">The widget to add.</param>
		/// <param name="WidgetIndex">The index (starting at 0) at which the widget must be inserted, or -1 to insert the widget after all existing widgets.</param>
		public void Insert (Widget w, int WidgetIndex)
		{
			w.Parent = this;
			w.Visible = true;

			if(WidgetIndex == -1)
				children.Add (w);
			else
				children.Insert (WidgetIndex, w);

			ShowAll ();
		}

		/// <summary>Removes the widget at the specified index.</summary>
		/// <param name="WidgetIndex">Index of the widget to remove.</param>
		public void Remove (int WidgetIndex)
		{
			if(WidgetIndex == -1) WidgetIndex = children.Count - 1;

			children[WidgetIndex].Parent = null;
			children.RemoveAt (WidgetIndex);

			ShowAll ();
		}

		protected override void ForAll (bool include_internals, Callback callback)
		{
			foreach(Widget w in children)
			{
				if(w.Visible) callback (w);
			}
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);

			int n = children.Count, nVisible = 0;
			childReqs = new Requisition[n];
			for(int i = 0 ; i < n ; ++i)
			{
				if(children[i].Visible)
				{
					childReqs[i] = children[i].SizeRequest ();
					++nVisible;
				}
			}

            requisition.Width = iconSize;
            requisition.Height = iconSize;
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);

			int n = children.Count;
			Gdk.Rectangle childAlloc = allocation;
			int lineHeight = 0;
			for(int i = 0 ; i < n ; ++i)
			{
				if(children[i].Visible)
				{
					childAlloc.Width = childReqs[i].Width;
					childAlloc.Height = childReqs[i].Height;

					if(childAlloc.X != allocation.X && childAlloc.Right > allocation.Right)
					{
						childAlloc.X = allocation.X;
						childAlloc.Y += lineHeight;
						lineHeight = 0;
					}

					children[i].SizeAllocate (childAlloc);
					childAlloc.X += childAlloc.Width;
					lineHeight = Math.Max (childAlloc.Height, lineHeight);
				}
			}
		}
    }
}
