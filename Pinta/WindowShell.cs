// 
// WindowShell.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2011 Jonathan Pobst
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
using Gtk;

namespace Pinta
{
	public class WindowShell
	{
		private ApplicationWindow app_window;
		private Box shell_layout;
		private Box menu_layout;
		private Box? workspace_layout;
#if false // TODO-GTK4
		private Box? main_toolbar;
#endif

		public WindowShell (Application app, string name, string title, int width, int height, bool maximize)
		{
			app_window = Gtk.ApplicationWindow.New (app);

			app_window.Name = name;
			app_window.Title = title;
			app_window.DefaultWidth = width;
			app_window.DefaultHeight = height;
			app_window.Resizable = true;

			if (maximize)
				app_window.Maximize ();

			shell_layout = Box.New (Orientation.Vertical, 0);
			menu_layout = Box.New (Orientation.Vertical, 0);

			shell_layout.Prepend (menu_layout);

			app_window.SetChild (shell_layout);
			app_window.Present ();
		}

		public ApplicationWindow Window => app_window;

#if false // TODO-GTK3
		public Toolbar CreateToolBar (string name)
		{
			main_toolbar = new Toolbar ();
			main_toolbar.Name = name;

			menu_layout.PackStart (main_toolbar, false, false, 0);
			main_toolbar.Show ();

			return main_toolbar;
		}

		public Statusbar CreateStatusBar (string name)
		{
			var statusbar = new Statusbar {
				Name = name,
				Margin = 0
			};

			// Remove the default text area
			var child = statusbar.Children.FirstOrDefault ();

			if (child != null)
				statusbar.Remove (child);

			shell_layout.PackEnd (statusbar, false, false, 0);
			statusbar.Show ();

			return statusbar;
		}
#endif

		public Box CreateWorkspace ()
		{
			workspace_layout = Box.New (Orientation.Horizontal, 0);
			workspace_layout.Name = "workspace_layout";
			workspace_layout.Hexpand = true;
			workspace_layout.Halign = Align.Fill;

			shell_layout.Append (workspace_layout);

			return workspace_layout;
		}

#if false // TODO-GTK4
		public void AddDragDropSupport (params TargetEntry[] entries)
		{
			Gtk.Drag.DestSet (this, Gtk.DestDefaults.Motion | Gtk.DestDefaults.Highlight | Gtk.DestDefaults.Drop, entries, Gdk.DragAction.Copy);
		}
#endif
	}
}
