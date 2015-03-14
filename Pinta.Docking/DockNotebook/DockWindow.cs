//
// DockWindow.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using System.Linq;
using System.Collections.Generic;
using Gdk;

namespace Pinta.Docking.DockNotebook
{
	public class DockWindow : Gtk.Window
	{
		static List<DockWindow> allWindows = new List<DockWindow> ();
        private DockNotebook notebook;

		public DockWindow () : base (Gtk.WindowType.Toplevel)
		{
			allWindows.Add (this);

			notebook = new DockNotebook ();
			notebook.NavigationButtonsVisible = false;
			Child = new DockNotebookContainer (notebook);
			notebook.InitSize ();
		}

		public static IEnumerable<DockWindow> GetAllWindows ()
		{
			return allWindows;
		}

		public DockNotebookContainer Container {
			get {
				return (DockNotebookContainer) Child;
			}
		}

		protected override bool OnDeleteEvent (Event evnt)
		{
            foreach (var notebook in Container.GetNotebooks ().ToList ())
            foreach (var tab in notebook.Tabs.ToList ())
                if (!notebook.OnCloseTab (tab))
                    return true;

			return base.OnDeleteEvent (evnt);
		}

		protected override void OnDestroyed ()
		{
			allWindows.Remove (this);

			base.OnDestroyed ();
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			// A small delay to make sure the window is fully rendered before showing it
			GdkWindow.Opacity = 0;
			GLib.Timeout.Add (120, delegate {
				GdkWindow.Opacity = 1;
				return false;
			});
		}
	}
	
}
