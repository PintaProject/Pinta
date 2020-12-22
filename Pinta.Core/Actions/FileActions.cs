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
using System.Collections.Generic;
using System.IO;
using Gdk;
using Gtk;

namespace Pinta.Core
{
	public class FileActions
	{
		public Command New { get; private set; }
		public Command NewScreenshot { get; private set; }
		public Command Open { get; private set; }
		public Command Close { get; private set; }
		public Command Save { get; private set; }
		public Command SaveAs { get; private set; }
		public Command Print { get; private set; }
		
		public event EventHandler<ModifyCompressionEventArgs>? ModifyCompression;
		public event EventHandler<DocumentCancelEventArgs>? SaveDocument;
		
		public FileActions ()
		{
			New = new Command("new", Translations.GetString("New..."), null, Resources.StandardIcons.DocumentNew);
			NewScreenshot = new Command ("NewScreenshot", Translations.GetString ("New Screenshot..."), null, Resources.StandardIcons.ViewFullscreen);
			Open = new Command ("open", Translations.GetString ("Open..."), null, Resources.StandardIcons.DocumentOpen);

			Close = new Command ("close", Translations.GetString ("Close"), null, Resources.StandardIcons.WindowClose);
			Save = new Command ("save", Translations.GetString ("Save"), null, Resources.StandardIcons.DocumentSave);
			SaveAs = new Command ("saveAs", Translations.GetString ("Save As..."), null, Resources.StandardIcons.DocumentSaveAs);
			Print = new Command ("print", Translations.GetString ("Print"), null, Resources.StandardIcons.DocumentPrint);

			New.ShortLabel = Translations.GetString ("New");
			Open.ShortLabel = Translations.GetString ("Open");
			Open.IsImportant = true;
			Save.IsImportant = true;
		}

#region Initialization
		public void RegisterActions(Gtk.Application app, GLib.Menu menu)
        {
			app.AddAccelAction(New, "<Primary>N");
			menu.AppendItem(New.CreateMenuItem());

			app.AddAction(NewScreenshot);
			menu.AppendItem(NewScreenshot.CreateMenuItem());

			app.AddAccelAction(Open, "<Primary>O");
			menu.AppendItem(Open.CreateMenuItem());

			var save_section = new GLib.Menu();
			menu.AppendSection(null, save_section);

			app.AddAccelAction(Save, "<Primary>S");
			save_section.AppendItem(Save.CreateMenuItem());

			app.AddAccelAction(SaveAs, "<Primary><Shift>S");
			save_section.AppendItem(SaveAs.CreateMenuItem());

			var close_section = new GLib.Menu();
			menu.AppendSection(null, close_section);

			app.AddAccelAction(Close, "<Primary>W");
			close_section.AppendItem(Close.CreateMenuItem());

			// This is part of the application menu on macOS.
			if (PintaCore.System.OperatingSystem != OS.Mac)
			{
				var exit = PintaCore.Actions.App.Exit;
				app.AddAccelAction(exit, "<Primary>Q");
				close_section.AppendItem(exit.CreateMenuItem());
			}

			// Printing is disabled for now until it is fully functional.
#if false
			menu.Append (Print.CreateAcceleratedMenuItem (Gdk.Key.P, Gdk.ModifierType.ControlMask));
			menu.AppendSeparator ();
#endif
		}

		public void RegisterHandlers ()
		{
		}
#endregion

#region Event Invokers
		internal bool RaiseSaveDocument (Document document, bool saveAs)
		{
			DocumentCancelEventArgs e = new DocumentCancelEventArgs (document, saveAs);

			if (SaveDocument == null)
				throw new InvalidOperationException ("GUI is not handling PintaCore.Workspace.SaveDocument");
			else
				SaveDocument (this, e);

			return !e.Cancel;
		}

		internal int RaiseModifyCompression (int defaultCompression, Gtk.Window parent)
		{
			ModifyCompressionEventArgs e = new ModifyCompressionEventArgs (defaultCompression, parent);
			
			if (ModifyCompression != null)
				ModifyCompression (this, e);
				
			return e.Cancel ? -1 : e.Quality;
		}
#endregion
	}
}
