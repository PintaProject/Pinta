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

using Pinta.Core;

namespace Pinta;

public sealed class WindowShell
{
	private readonly Gtk.ApplicationWindow app_window;
	private readonly Adw.HeaderBar? header_bar;
	private readonly Gtk.Box shell_layout;
	private Gtk.Box? workspace_layout;
	private Gtk.Box? main_toolbar;

	public WindowShell (
		Gtk.Application app,
		string name,
		string title,
		int width,
		int height,
		bool useHeaderBar,
		bool maximize)
	{
		var app_layout = Adw.ToolbarView.New ();

		if (useHeaderBar) {
			var adwWindow = Adw.ApplicationWindow.New (app);
			adwWindow.SetContent (app_layout);
			app_window = adwWindow;

			header_bar = Adw.HeaderBar.New ();
			app_layout.AddTopBar (header_bar);
		} else {
			// If the header bar isn't being used, we use a regular Gtk.ApplicationWindow
			// to have a traditional titlebar with the standard close / minimize buttons,
			// and a menubar in the window unless a global menu is used (e.g. macOS)
			app_window = Gtk.ApplicationWindow.New (app);
			app_window.SetChild (app_layout);
		}

		app_window.Name = name;
		app_window.Title = title;
		app_window.DefaultWidth = width;
		app_window.DefaultHeight = height;
		app_window.Resizable = true;

		if (maximize)
			app_window.Maximize ();

		shell_layout = Gtk.Box.New (Gtk.Orientation.Vertical, 0);
		app_layout.SetContent (shell_layout);

		app_window.Present ();
	}

	public Gtk.ApplicationWindow Window => app_window;
	public Adw.HeaderBar? HeaderBar => header_bar;

	public Gtk.Box CreateToolBar (string name)
	{
		main_toolbar = GtkExtensions.CreateToolBar ();
		main_toolbar.Name = name;

		shell_layout.Append (main_toolbar);
		main_toolbar.Show ();

		return main_toolbar;
	}

	public Gtk.Box CreateStatusBar (string name)
	{
		var statusbar = GtkExtensions.CreateToolBar ();
		statusbar.Name = name;

		shell_layout.Append (statusbar);

		return statusbar;
	}

	public Gtk.Box CreateWorkspace ()
	{
		workspace_layout = Gtk.Box.New (Gtk.Orientation.Horizontal, 0);
		workspace_layout.Name = "workspace_layout";
		workspace_layout.Hexpand = true;
		workspace_layout.Halign = Gtk.Align.Fill;

		shell_layout.Append (workspace_layout);

		return workspace_layout;
	}
}
