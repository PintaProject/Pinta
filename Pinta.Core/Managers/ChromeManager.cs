// 
// ChromeManager.cs
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
	public class ChromeManager
	{
		private Toolbar tool_toolbar;
		private DrawingArea drawing_area;
		private Window main_window;
		private IProgressDialog progress_dialog;
		private bool main_window_busy;
		private Gdk.Point last_canvas_cursor_point;
		private MenuBar main_menu;
		private Toolbar main_toolbar;
		private Statusbar main_statusbar;

		public Toolbar ToolToolBar { get { return tool_toolbar; } }
		public Toolbar MainToolBar { get { return main_toolbar; } }
		public DrawingArea Canvas { get { return drawing_area; } }
		public Window MainWindow { get { return main_window; } }
		public IProgressDialog ProgressDialog { get { return progress_dialog; } }
		public MenuBar MainMenu { get { return main_menu; } }
		public Statusbar MainStatusbar { get { return main_statusbar; } }

		public ChromeManager ()
		{
		}
		
		#region Public Properties
		public Gdk.Point LastCanvasCursorPoint {
			get { return last_canvas_cursor_point; }
			set {
				if (last_canvas_cursor_point != value) {
					last_canvas_cursor_point = value;
					OnLastCanvasCursorPointChanged ();				
				}
			}
		}
		
		public bool MainWindowBusy {
			get { return main_window_busy; }
			set {
				main_window_busy = value;
				
				if (main_window_busy)
					main_window.GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
				else
					main_window.GdkWindow.Cursor = PintaCore.Tools.CurrentTool.DefaultCursor;
			}
		}
		#endregion

		#region Public Methods
		public void InitializeToolToolBar (Toolbar toolToolBar)
		{
			tool_toolbar = toolToolBar;
		}

		public void InitializeMainToolBar (Toolbar mainToolBar)
		{
			main_toolbar = mainToolBar;
		}

		public void InitializeCanvas (DrawingArea canvas)
		{
			drawing_area = canvas;
		}

		public void InitializeWindowShell (Window shell)
		{
			main_window = shell;
		}

		public void InitializeMainMenu (MenuBar menu)
		{
			main_menu = menu;
		}

		public void InitializeMainStatusbar (Statusbar statusbar)
		{
			main_statusbar = statusbar;
		}

		public void InitializeProgessDialog (IProgressDialog progressDialog)
		{
			if (progressDialog == null)
				throw new ArgumentNullException ("progressDialog");

			progress_dialog = progressDialog;
		}


		public void SetStatusBarText (string text)
		{
			OnStatusBarTextChanged (text);

			if(main_statusbar != null)
				main_statusbar.Push(0, text);
		}
		#endregion

		#region Protected Methods
		protected void OnLastCanvasCursorPointChanged ()
		{
			if (LastCanvasCursorPointChanged != null)
				LastCanvasCursorPointChanged (this, EventArgs.Empty);
		}

		protected void OnStatusBarTextChanged (string text)
		{
			if (StatusBarTextChanged != null)
				StatusBarTextChanged (this, new TextChangedEventArgs (text));
		}
		#endregion
		
		#region Public Events
		public event EventHandler LastCanvasCursorPointChanged;
		public event EventHandler<TextChangedEventArgs> StatusBarTextChanged;
		#endregion
	}
		
	public interface IProgressDialog
	{
		void Show ();
		void Hide ();
		string Title { get; set; }
		string Text { get; set; }
		double Progress { get; set; }
		event EventHandler<EventArgs> Canceled;
	}
}
