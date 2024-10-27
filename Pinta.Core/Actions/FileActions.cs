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

	public void RegisterActions (Gtk.Application application, Gio.Menu menu)
	{
		bool isMac = system.OperatingSystem == OS.Mac;

		Gio.Menu save_section = Gio.Menu.New ();
		save_section.AppendItem (Save.CreateMenuItem ());
		save_section.AppendItem (SaveAs.CreateMenuItem ());

		Gio.Menu close_section = Gio.Menu.New ();
		close_section.AppendItem (Close.CreateMenuItem ());
		if (!isMac) close_section.AppendItem (app.Exit.CreateMenuItem ()); // This is part of the application menu on macOS

		menu.AppendItem (New.CreateMenuItem ());
		menu.AppendItem (NewScreenshot.CreateMenuItem ());
		menu.AppendItem (Open.CreateMenuItem ());
		menu.AppendSection (null, save_section);
		menu.AppendSection (null, close_section);
#if false
		// Printing is disabled for now until it is fully functional.
		menu.Append (Print.CreateAcceleratedMenuItem (Gdk.Key.P, Gdk.ModifierType.ControlMask));
		menu.AppendSeparator ();
#endif

		application.AddAccelAction (New, "<Primary>N");
		application.AddAction (NewScreenshot);
		application.AddAccelAction (Open, "<Primary>O");
		application.AddAccelAction (Save, "<Primary>S");
		application.AddAccelAction (SaveAs, "<Primary><Shift>S");
		application.AddAccelAction (Close, "<Primary>W");
		if (!isMac) application.AddAccelAction (app.Exit, "<Primary>Q"); // This is part of the application menu on macOS
	}

	public void RegisterHandlers () { }

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
}
