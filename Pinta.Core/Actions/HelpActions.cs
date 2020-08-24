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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Gtk;
using Mono.Unix;

namespace Pinta.Core
{
	public class HelpActions
	{
		public Gtk.Action Contents { get; private set; }
		public Gtk.Action Website { get; private set; }
		public Gtk.Action Bugs { get; private set; }
		public Gtk.Action Translate { get; private set; }
		public Gtk.Action About { get; private set; }

		public HelpActions ()
		{
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add ("Menu.Help.Bug.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Help.Bug.png")));
			fact.Add ("Menu.Help.Website.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Help.Website.png")));
			fact.Add ("Menu.Help.Translate.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.Help.Translate.png")));
			fact.AddDefault ();

			Contents = new Gtk.Action ("Contents", Catalog.GetString ("Contents"), null, Stock.Help);
			Website = new Gtk.Action ("Website", Catalog.GetString ("Pinta Website"), null, "Menu.Help.Website.png");
			Bugs = new Gtk.Action ("Bugs", Catalog.GetString ("File a Bug"), null, "Menu.Help.Bug.png");
			Translate = new Gtk.Action ("Translate", Catalog.GetString ("Translate This Application"), null, "Menu.Help.Translate.png");
			About = new Gtk.Action ("About", Catalog.GetString ("About"), null, Stock.About);
		}

		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
			menu.Append (Contents.CreateAcceleratedMenuItem (Gdk.Key.F1, Gdk.ModifierType.None));
			menu.Append (Website.CreateMenuItem ());
			menu.Append (Bugs.CreateMenuItem ());
			menu.Append (Translate.CreateMenuItem ());
			menu.AppendSeparator ();
			menu.Append (About.CreateMenuItem ());
		}
		
		public void RegisterHandlers ()
		{
			Contents.Activated += DisplayHelp;
			Website.Activated += new EventHandler (Website_Activated);
			Bugs.Activated += new EventHandler (Bugs_Activated);
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

		private void OpenUrl(string url)
        {
			try {
				Process.Start (url);
            } catch (System.ComponentModel.Win32Exception) {
				// See bug #1888883. Newer mono versions (e.g. 6.10) throw an
				// error instead of opening the default browser, so explicitly
				// try opening via xdg-open if the simple approach fails.
				if (PintaCore.System.OperatingSystem == OS.X11) {
					Process.Start ("xdg-open", url);
				} else {
					throw;
                }
            }
        }
		#endregion
	}
}
