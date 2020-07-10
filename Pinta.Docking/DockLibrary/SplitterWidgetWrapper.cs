//
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

using Gdk;
using Gtk;

namespace Pinta.Docking
{
	interface ISplitterWidget
	{
		Gtk.Widget Parent { get; }
		void Init (DockGroup grp, int index);
		void SizeAllocate (Gdk.Rectangle rect);
		bool Visible { get; set; }
		void Hide ();
		void Show ();
		Gtk.Widget Widget { get; }
	}

	class SplitterWidgetWrapper : ISplitterWidget
	{
		public Gtk.Widget Parent => nativeWidget.Parent;
		public Gtk.Widget Widget => nativeWidget.Widget;

		public bool Visible {
			get => nativeWidget.Visible;
			set => nativeWidget.Visible = value;
		}

		ISplitterWidget nativeWidget;

		public SplitterWidgetWrapper (ISplitterWidget splitterWidget)
		{
			nativeWidget = splitterWidget;
		}

		public void Init (DockGroup grp, int index) => nativeWidget.Init (grp, index);

		public void SizeAllocate (Rectangle rect) => nativeWidget.SizeAllocate (rect);

		public void Show () => nativeWidget.Show ();
		public void Hide () => nativeWidget.Hide ();
	}
}
