// 
// FileActions.cs
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

namespace Pinta.Core;

public sealed class FileActions
{
	public Command New { get; }
	public Command NewScreenshot { get; }
	public Command Open { get; }
	public Command Close { get; }
	public Command Save { get; }
	public Command SaveAs { get; }
	public Command Print { get; }

	public event EventHandler<ModifyCompressionEventArgs>? ModifyCompression;
	public event EventHandler<DocumentCancelEventArgs>? SaveDocument;

	private readonly SystemManager system;
	private readonly AppActions app;
	public FileActions (
		SystemManager system,
		AppActions app)
	{
		New = new Command ("new", Translations.GetString ("New..."), null, Resources.StandardIcons.DocumentNew);
		NewScreenshot = new Command ("NewScreenshot", Translations.GetString ("New Screenshot..."), null, Resources.StandardIcons.ViewFullscreen);
		Open = new Command ("open", Translations.GetString ("Open..."), null, Resources.StandardIcons.DocumentOpen);

		Close = new Command ("close", Translations.GetString ("Close"), null, Resources.StandardIcons.WindowClose);
		Save = new Command ("save", Translations.GetString ("Save"), null, Resources.StandardIcons.DocumentSave);
		SaveAs = new Command ("saveAs", Translations.GetString ("Save As..."), null, Resources.StandardIcons.DocumentSaveAs);
		Print = new Command ("print", Translations.GetString ("Print"), null, Resources.StandardIcons.DocumentPrint);

		New.ShortLabel = Translations.GetString ("New");
		Open.ShortLabel = Translations.GetString ("Open");

		this.system = system;
		this.app = app;
	}

	#region Initialization
	public void RegisterActions (Gtk.Application application, Gio.Menu menu)
	{
		application.AddAccelAction (New, "<Primary>N");
		menu.AppendItem (New.CreateMenuItem ());

		application.AddAction (NewScreenshot);
		menu.AppendItem (NewScreenshot.CreateMenuItem ());

		application.AddAccelAction (Open, "<Primary>O");
		menu.AppendItem (Open.CreateMenuItem ());

		var save_section = Gio.Menu.New ();
		menu.AppendSection (null, save_section);

		application.AddAccelAction (Save, "<Primary>S");
		save_section.AppendItem (Save.CreateMenuItem ());

		application.AddAccelAction (SaveAs, "<Primary><Shift>S");
		save_section.AppendItem (SaveAs.CreateMenuItem ());

		var close_section = Gio.Menu.New ();
		menu.AppendSection (null, close_section);

		application.AddAccelAction (Close, "<Primary>W");
		close_section.AppendItem (Close.CreateMenuItem ());

		// This is part of the application menu on macOS.
		if (system.OperatingSystem != OS.Mac) {
			var exit = app.Exit;
			application.AddAccelAction (exit, "<Primary>Q");
			close_section.AppendItem (exit.CreateMenuItem ());
		}

		// Printing is disabled for now until it is fully functional.
#if false
		menu.Append (Print.CreateAcceleratedMenuItem (Gdk.Key.P, Gdk.ModifierType.ControlMask));
		menu.AppendSeparator ();
#endif
	}

	public void RegisterHandlers () { }

	#endregion

	#region Event Invokers
	internal bool RaiseSaveDocument (Document document, bool saveAs)
	{
		if (SaveDocument == null)
			throw new InvalidOperationException ("GUI is not handling Workspace.SaveDocument");

		DocumentCancelEventArgs e = new (document, saveAs);
		SaveDocument (this, e);
		return !e.Cancel;
	}

	internal int RaiseModifyCompression (int defaultCompression, Gtk.Window parent)
	{
		ModifyCompressionEventArgs e = new (defaultCompression, parent);
		ModifyCompression?.Invoke (this, e);
		return e.Cancel ? -1 : e.Quality;
	}
	#endregion
}
