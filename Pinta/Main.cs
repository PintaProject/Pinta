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
using System.CommandLine;
using System.IO;
using System.Linq;
using Pinta.Core;

namespace Pinta;

internal sealed class MainClass
{
	[STAThread]
	public static int Main (string[] args)
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

		var threads_option = new Option<int> (
			name: "--render-threads",
			aliases: "-rt") {
			Description = Translations.GetString ("Number of threads to use for rendering"),
			DefaultValueFactory = _ => -1
		};

		var files_arg = new Argument<string[]> (
			name: "files") {
			Description = Translations.GetString ("Files to open")
		};

		var debug_option = new Option<bool> (
			name: "--debug") {
			Description = Translations.GetString ("Enable additional logging or behavior changes for debugging")
		};

		// Note the implicit '--version' argument uses the InformationalVersion from the assembly.
		var root_command = new RootCommand (Translations.GetString ("Pinta"));
		root_command.Options.Add (threads_option);
		root_command.Arguments.Add (files_arg);
		root_command.Options.Add (debug_option);

		root_command.SetAction (parseResult => {
			OpenMainWindow (
				parseResult.GetValue (threads_option),
				parseResult.GetValue (files_arg) ?? [],
				parseResult.GetValue (debug_option));
		});

		return root_command.Parse (args).Invoke ();
	}

	private static void OpenMainWindow (int threads, IEnumerable<string> files, bool debug)
	{
		GLib.UnhandledException.SetHandler (OnUnhandledException);

		Gsk.Module.Initialize ();
		Pango.Module.Initialize ();
		PangoCairo.Module.Initialize ();
		var app = Adw.Application.New ("com.github.PintaProject.Pinta", Gio.ApplicationFlags.NonUnique);

		// Add our icons to the search path.
		GtkExtensions.GetDefaultIconTheme ().AddSearchPath (Pinta.Core.SystemManager.GetDataRootDirectory () + "/icons");

		var main_window = new MainWindow (app);

		if (SystemManager.GetOperatingSystem () == OS.Mac) {
			RegisterForAppleEvents ();
		}

		if (threads > 0)
			PintaCore.System.RenderThreads = threads;

		app.OnStartup += (_, _) => main_window.Startup ();

		app.OnActivate += (_, _) => {
			main_window.Activate ();
			OpenFilesFromCommandLine (files);

			// For debugging, run the garbage collector much more frequently.
			// This can be useful to detect certain memory management issues in the GTK bindings.
			if (debug) {
				GLib.Functions.TimeoutAdd (0, 100, () => {
					GC.Collect ();
					GC.WaitForPendingFinalizers ();
					return true;
				});
			}
		};

		// Run with a SynchronizationContext to integrate async methods with GLib.MainLoop.
		app.RunWithSynchronizationContext (null);
	}

	private static void OpenFilesFromCommandLine (IEnumerable<string> files)
	{
		// Ignore the process serial number parameter on Mac OS X
		if (PintaCore.System.OperatingSystem == OS.Mac && files.Any ()) {
			if (files.First ().StartsWith ("-psn_")) {
				files = files.Skip (1);
			}
		}

		if (files.Any ()) {
			foreach (var file in files) {
				PintaCore.Workspace.OpenFile (Gio.FileHelper.NewForCommandlineArg (file));
			}
		} else {
			// Create a blank document
			PintaCore.Workspace.NewDocument (
				PintaCore.Actions,
				new Core.Size (800, 600),
				new Cairo.Color (1, 1, 1));
		}
	}

	private static void OnUnhandledException (Exception e)
	{
		_ = PintaCore.Chrome.ShowErrorDialog (PintaCore.Chrome.MainWindow,
				"Unhandled exception", e.Message, e.ToString ());
	}

	/// <summary>
	/// Registers for OSX-specific events, like quitting from the dock.
	/// </summary>
	static void RegisterForAppleEvents ()
	{
		MacInterop.ApplicationEvents.Quit += (sender, e) => {
			GLib.Functions.TimeoutAdd (0, 10, () => {
				PintaCore.Actions.App.Exit.Activate ();
				return false;
			});
			e.Handled = true;
		};

		MacInterop.ApplicationEvents.Reopen += (sender, e) => {
			var window = PintaCore.Chrome.MainWindow;
			window.Unminimize ();
			window.Hide ();
			window.Show ();
			window.Present ();
			e.Handled = true;
		};

		MacInterop.ApplicationEvents.OpenDocuments += (sender, e) => {
			if (e.Documents != null) {
				GLib.Functions.TimeoutAdd (0, 10, () => {
					foreach (string filename in e.Documents.Keys) {
						System.Console.Error.WriteLine ("Opening: {0}", filename);
						PintaCore.Workspace.OpenFile (Gio.FileHelper.NewForCommandlineArg (filename));
					}
					return false;
				});
			}
			e.Handled = true;
		};
	}
}
