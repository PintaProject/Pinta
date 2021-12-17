// 
// Main.cs
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
using System.Reflection;
using System.Runtime.InteropServices;
using Gtk;
using Mono.Options;
using Pinta.Core;

namespace Pinta
{
	class MainClass
	{
		[STAThread]
		public static void Main (string[] args)
		{
			if (SystemManager.GetOperatingSystem () == OS.Mac) {
				MacInterop.Environment.Init ();
			}

			string locale_dir = Path.Combine (SystemManager.GetDataRootDirectory (), "locale");

			try {
				Translations.Init ("pinta", locale_dir);
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}

			int threads = -1;
			bool show_help = false;
			bool show_version = false;

			var p = new OptionSet () {
				{ "h|help", Translations.GetString("Show this message and exit."), v => show_help = v != null },
				{ "v|version", Translations.GetString("Display the application version."), v => show_version = v != null },
				{ "rt|render-threads=", Translations.GetString ("number of threads to use for rendering"), (int v) => threads = v }
			};

			List<string> extra;

			try {
				extra = p.Parse (args);
			} catch (OptionException e) {
				Console.WriteLine (e.Message);
				ShowHelp (p);
				return;
			}

			if (show_version) {
				Console.WriteLine (PintaCore.ApplicationVersion);
				return;
			}

			if (show_help) {
				ShowHelp (p);
				return;
			}

			GLib.ExceptionManager.UnhandledException += new GLib.UnhandledExceptionHandler (ExceptionManager_UnhandledException);

			Application.Init ();

			// For testing a dark variant of the theme.
			//Gtk.Settings.Default.SetProperty("gtk-application-prefer-dark-theme", new GLib.Value(true));

			// Add our icons to the search path.
			Gtk.IconTheme.Default.AppendSearchPath (Pinta.Core.SystemManager.GetDataRootDirectory () + "/icons");

			var app = new MainWindow ();

			if (threads != -1)
				Pinta.Core.PintaCore.System.RenderThreads = threads;

			if (SystemManager.GetOperatingSystem () == OS.Mac) {
				RegisterForAppleEvents ();
			}

			// TODO-GTK3 - try using the GTK command line parsing once GtkSharp supports it.
			app.Activated += (_, _) => OpenFilesFromCommandLine (extra);
			app.Run ("pinta", Array.Empty<string> ());
		}

		private static void ShowHelp (OptionSet p)
		{
			Console.WriteLine (Translations.GetString ("Usage: pinta [files]"));
			Console.WriteLine ();
			Console.WriteLine (Translations.GetString ("Options: "));
			p.WriteOptionDescriptions (Console.Out);
		}

		private static void OpenFilesFromCommandLine (List<string> extra)
		{
			// Ignore the process serial number parameter on Mac OS X
			if (PintaCore.System.OperatingSystem == OS.Mac && extra.Count > 0) {
				if (extra[0].StartsWith ("-psn_")) {
					extra.RemoveAt (0);
				}
			}

			if (extra.Count > 0) {
				foreach (var file in extra) {
					string path = file;
					if (path.StartsWith ("file://"))
						path = new Uri (path).LocalPath;

					PintaCore.Workspace.OpenFile (path);
				}
			} else {
				// Create a blank document
				PintaCore.Workspace.NewDocument (new Gdk.Size (800, 600), new Cairo.Color (1, 1, 1));
			}
		}

		private static void ExceptionManager_UnhandledException (GLib.UnhandledExceptionArgs args)
		{
			Exception ex = (Exception) args.ExceptionObject;
			PintaCore.Chrome.ShowErrorDialog (PintaCore.Chrome.MainWindow,
							  string.Format ("{0}:\n{1}", "Unhandled exception", ex.Message),
							  ex.ToString ());
		}

		/// <summary>
		/// Registers for OSX-specific events, like quitting from the dock.
		/// </summary>
		static void RegisterForAppleEvents ()
		{
			MacInterop.ApplicationEvents.Quit += (sender, e) => {
				GLib.Timeout.Add (10, delegate {
					PintaCore.Actions.App.Exit.Activate ();
					return false;
				});
				e.Handled = true;
			};

			MacInterop.ApplicationEvents.Reopen += (sender, e) => {
				var window = PintaCore.Chrome.MainWindow;
				window.Deiconify ();
				window.Hide ();
				window.Show ();
				window.Present ();
				e.Handled = true;
			};

			MacInterop.ApplicationEvents.OpenDocuments += (sender, e) => {
				if (e.Documents != null) {
					GLib.Timeout.Add (10, delegate {
						foreach (string filename in e.Documents.Keys) {
							System.Console.Error.WriteLine ("Opening: {0}", filename);
							PintaCore.Workspace.OpenFile (filename);
						}
						return false;
					});
				}
				e.Handled = true;
			};
		}
	}
}
