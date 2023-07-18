// 
// HelpActions.cs
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

namespace Pinta.Core
{
	public class HelpActions
	{
		public Command Contents { get; }
		public Command Website { get; }
		public Command Bugs { get; }
		public Command Translate { get; }

		public HelpActions ()
		{
			Contents = new Command ("contents", Translations.GetString ("Contents"), null, Resources.StandardIcons.HelpBrowser);
			Website = new Command ("website", Translations.GetString ("Pinta Website"), null, Resources.Icons.HelpWebsite);
			Bugs = new Command ("bugs", Translations.GetString ("File a Bug"), null, Resources.Icons.HelpBug);
			Translate = new Command ("translate", Translations.GetString ("Translate This Application"), null, Resources.Icons.HelpTranslate);
		}

		#region Initialization
		public void RegisterActions (Gtk.Application app, Gio.Menu menu)
		{
			app.AddAccelAction (Contents, "F1");
			menu.AppendItem (Contents.CreateMenuItem ());

			app.AddAction (Website);
			menu.AppendItem (Website.CreateMenuItem ());

			app.AddAction (Bugs);
			menu.AppendItem (Bugs.CreateMenuItem ());

			app.AddAction (Translate);
			menu.AppendItem (Translate.CreateMenuItem ());

			// This is part of the application menu on macOS.
			if (PintaCore.System.OperatingSystem != OS.Mac) {
				var about_section = Gio.Menu.New ();
				menu.AppendSection (null, about_section);

				var about = PintaCore.Actions.App.About;
				app.AddAction (about);
				about_section.AppendItem (about.CreateMenuItem ());
			}
		}

		public void RegisterHandlers ()
		{
			Contents.Activated += DisplayHelp;
			Website.Activated += Website_Activated;
			Bugs.Activated += Bugs_Activated;
			Translate.Activated += Translate_Activated;
		}

		private void Bugs_Activated (object sender, EventArgs e)
		{
			OpenUrl ("https://bugs.launchpad.net/pinta");
		}

		private void DisplayHelp (object sender, EventArgs e)
		{
			OpenUrl ("https://pinta-project.com/user-guide");
		}

		private void Translate_Activated (object sender, EventArgs e)
		{
			OpenUrl ("https://translations.launchpad.net/pinta");
		}

		private void Website_Activated (object sender, EventArgs e)
		{
			OpenUrl ("https://www.pinta-project.com");
		}

		private void OpenUrl (string url)
		{
			// TODO-GTK4 - this produces an "unsupported on current backend" error on macOS
			Gtk.Functions.ShowUri (PintaCore.Chrome.MainWindow, url, /* GDK_CURRENT_TIME */ 0);
		}
		#endregion
	}
}
