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
using Mono.Addins.Localization;

namespace Pinta.Core;

public interface IChromeService
{
	Window MainWindow { get; }
	void LaunchSimpleEffectDialog (BaseEffect effect, IAddinLocalizer localizer);
}

public sealed class ChromeManager : IChromeService
{
	private PointI last_canvas_cursor_point;
	private bool main_window_busy;

	// NRT - These are all initialized via the Initialize* functions
	// but it would be nice to rewrite it to provably non-null.
	public Application Application { get; private set; } = null!;
	public Window MainWindow { get; private set; } = null!;
	public Widget ImageTabsNotebook { get; private set; } = null!;
	private IProgressDialog progress_dialog = null!;
	private ErrorDialogHandler error_dialog_handler = null!;
	private MessageDialogHandler message_dialog_handler = null!;
	private SimpleEffectDialogHandler simple_effect_dialog_handler = null!;

	public Box? MainToolBar { get; private set; }
	public Box ToolToolBar { get; private set; } = null!;
	public Widget ToolBox { get; private set; } = null!;
	public Box StatusBar { get; private set; } = null!;

	public IProgressDialog ProgressDialog => progress_dialog;
	public Gio.Menu AdjustmentsMenu { get; private set; } = null!;
	public Gio.Menu EffectsMenu { get; private set; } = null!;

	public ChromeManager ()
	{
	}

	#region Public Properties
	public PointI LastCanvasCursorPoint {
		get => last_canvas_cursor_point;
		set {
			if (last_canvas_cursor_point != value) {
				last_canvas_cursor_point = value;
				OnLastCanvasCursorPointChanged ();
			}
		}
	}

	public bool MainWindowBusy {
		get => main_window_busy;
		set {
			main_window_busy = value;

			if (main_window_busy)
				MainWindow.Cursor = Gdk.Cursor.NewFromName (Pinta.Resources.StandardCursors.Progress, null);
			else
				MainWindow.Cursor = Gdk.Cursor.NewFromName (Pinta.Resources.StandardCursors.Default, null);
		}
	}
	#endregion

	#region Public Methods
	public void InitializeApplication (Gtk.Application application)
	{
		Application = application;
	}

	public void InitializeWindowShell (Window shell)
	{
		MainWindow = shell;
	}

	public void InitializeToolToolBar (Box toolToolBar)
	{
		ToolToolBar = toolToolBar;
	}

	public void InitializeMainToolBar (Box mainToolBar)
	{
		MainToolBar = mainToolBar;
	}

	public void InitializeStatusBar (Box statusbar)
	{
		StatusBar = statusbar;
	}

	public void InitializeToolBox (Widget toolbox)
	{
		ToolBox = toolbox;
	}

	public void InitializeImageTabsNotebook (Widget notebook)
	{
		ImageTabsNotebook = notebook;
	}

	public void InitializeMainMenu (Gio.Menu adj_menu, Gio.Menu effects_menu)
	{
		AdjustmentsMenu = adj_menu;
		EffectsMenu = effects_menu;
	}

	public void InitializeProgessDialog (IProgressDialog progressDialog)
	{
		progress_dialog = progressDialog;
	}

	public void InitializeErrorDialogHandler (ErrorDialogHandler handler)
	{
		error_dialog_handler = handler;
	}

	public void InitializeMessageDialog (MessageDialogHandler handler)
	{
		message_dialog_handler = handler;
	}

	public void InitializeSimpleEffectDialog (SimpleEffectDialogHandler handler)
	{
		simple_effect_dialog_handler = handler;
	}

	public void ShowErrorDialog (Window parent, string message, string body, string details)
	{
		error_dialog_handler (parent, message, body, details);
	}

	public void ShowMessageDialog (Window parent, string message, string body)
	{
		message_dialog_handler (parent, message, body);
	}

	public void SetStatusBarText (string text)
	{
		OnStatusBarTextChanged (text);
	}

	public void LaunchSimpleEffectDialog (BaseEffect effect, IAddinLocalizer localizer)
	{
		simple_effect_dialog_handler (effect, localizer);
	}
	#endregion

	private void OnLastCanvasCursorPointChanged ()
	{
		LastCanvasCursorPointChanged?.Invoke (this, EventArgs.Empty);
	}

	private void OnStatusBarTextChanged (string text)
	{
		StatusBarTextChanged?.Invoke (this, new TextChangedEventArgs (text));
	}

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

public delegate void ErrorDialogHandler (Window parent, string message, string body, string details);
public delegate void MessageDialogHandler (Window parent, string message, string body);
public delegate void SimpleEffectDialogHandler (BaseEffect effect, IAddinLocalizer localizer);
