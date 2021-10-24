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
		// NRT - These are all initialized via the Initialize* functions
		// but it would be nice to rewrite it to provably non-null.
		private Toolbar tool_toolbar = null!;
		private Window main_window = null!;
		private IProgressDialog progress_dialog = null!;
		private bool main_window_busy;
		private Gdk.Point last_canvas_cursor_point;
		private Toolbar main_toolbar = null!;
		private ErrorDialogHandler error_dialog_handler = null!;
		private UnsupportedFormatDialogHandler unsupported_format_dialog_handler = null!;

		public Application Application { get; private set; } = null!;
		public Toolbar ToolToolBar { get { return tool_toolbar; } }
		public Toolbar MainToolBar { get { return main_toolbar; } }
		public Window MainWindow { get { return main_window; } }
		public Statusbar StatusBar { get; private set; } = null!;
		public Toolbar ToolBox { get; private set; } = null!;

		public IProgressDialog ProgressDialog { get { return progress_dialog; } }
		public GLib.Menu AdjustmentsMenu { get; private set; } = null!;
		public GLib.Menu EffectsMenu { get; private set; } = null!;

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
					main_window.Window.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
				else
					main_window.Window.Cursor = new Gdk.Cursor(Gdk.CursorType.Arrow);
			}
		}
		#endregion

		#region Public Methods
		public void InitializeApplication (Gtk.Application application)
        {
			Application = application;
        }

		public void InitializeToolToolBar (Toolbar toolToolBar)
		{
			tool_toolbar = toolToolBar;
		}

		public void InitializeMainToolBar (Toolbar mainToolBar)
		{
			main_toolbar = mainToolBar;
		}

		public void InitializeStatusBar (Statusbar statusbar)
		{
			StatusBar = statusbar;
		}

		public void InitializeToolBox (Toolbar toolbox)
		{
			ToolBox = toolbox;
		}

		public void InitializeWindowShell (Window shell)
		{
			main_window = shell;
		}

		public void InitializeMainMenu (GLib.Menu adj_menu, GLib.Menu effects_menu)
		{
			AdjustmentsMenu = adj_menu;
			EffectsMenu = effects_menu;
		}

		public void InitializeProgessDialog (IProgressDialog progressDialog)
		{
			if (progressDialog == null)
				throw new ArgumentNullException ("progressDialog");

			progress_dialog = progressDialog;
		}

		public void InitializeErrorDialogHandler (ErrorDialogHandler handler)
		{
			error_dialog_handler = handler;
		}

		public void InitializeUnsupportedFormatDialog (UnsupportedFormatDialogHandler handler)
		{
			unsupported_format_dialog_handler = handler;
		}

		public void ShowErrorDialog (Window parent, string message, string details)
		{
			error_dialog_handler (parent, message, details);
		}

		public void ShowUnsupportedFormatDialog (Window parent, string message, string details)
		{
			unsupported_format_dialog_handler (parent, message, details);
		}

		public void SetStatusBarText (string text)
		{
			OnStatusBarTextChanged (text);
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
		public event EventHandler? LastCanvasCursorPointChanged;
		public event EventHandler<TextChangedEventArgs>? StatusBarTextChanged;
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

	public delegate void ErrorDialogHandler(Window parent, string message, string details);
	public delegate void UnsupportedFormatDialogHandler(Window parent, string message, string details);
}
