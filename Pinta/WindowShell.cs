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

using Gtk;
using Pinta.Core;

namespace Pinta;

public sealed class WindowShell
{
	private readonly ApplicationWindow app_window;
	private readonly Adw.HeaderBar? header_bar;
	private readonly Box shell_layout;
	private readonly Box menu_layout;
	private Box? workspace_layout;
	private Box? main_toolbar;

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

		// On macOS the global menubar is used, but otherwise use a header bar.
		if (PintaCore.System.OperatingSystem != OS.Mac) {
			header_bar = Adw.HeaderBar.New ();
			app_window.SetTitlebar (header_bar);
		}

		shell_layout = Box.New (Orientation.Vertical, 0);
		menu_layout = Box.New (Orientation.Vertical, 0);

		shell_layout.Prepend (menu_layout);

		app_window.SetChild (shell_layout);
		app_window.Present ();
	}

	public ApplicationWindow Window => app_window;
	public Adw.HeaderBar? HeaderBar => header_bar;

	public Box CreateToolBar (string name)
	{
		main_toolbar = GtkExtensions.CreateToolBar ();
		main_toolbar.Name = name;

		menu_layout.Append (main_toolbar);
		main_toolbar.Show ();

		return main_toolbar;
	}

	public Box CreateStatusBar (string name)
	{
		var statusbar = GtkExtensions.CreateToolBar ();
		statusbar.Name = name;

		shell_layout.Append (statusbar);

		return statusbar;
	}

	public Box CreateWorkspace ()
	{
		workspace_layout = Box.New (Orientation.Horizontal, 0);
		workspace_layout.Name = "workspace_layout";
		workspace_layout.Hexpand = true;
		workspace_layout.Halign = Align.Fill;

		shell_layout.Append (workspace_layout);

		return workspace_layout;
	}
}
