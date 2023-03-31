// 
// NewScreenshotAction.cs
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
using System.Diagnostics;
using Gdk;
using Pinta.Core;

namespace Pinta.Actions
{
	class NewScreenshotAction : IActionHandler
	{
		#region IActionHandler Members
		public void Initialize ()
		{
			PintaCore.Actions.File.NewScreenshot.Activated += Activated;
		}

		public void Uninitialize ()
		{
			PintaCore.Actions.File.NewScreenshot.Activated -= Activated;
		}
		#endregion

		private void Activated (object _, EventArgs args)
		{
			// GTK4 removed gdk_pixbuf_get_from_window(), so we need to use OS-specific APis to take a screenshot.
			// TODO-GTK4 - implement screenshots for Windows

			// On Linux, use the XDG Desktop Portal Screenshot API.
			if (SystemManager.GetOperatingSystem () == OS.X11) {
				try {
					var connection = Gio.DBusConnection.Get (Gio.BusType.Session);

					Console.WriteLine ($"Connection unique name {connection.UniqueName}");
					string sender = connection.UniqueName!.TrimStart (':').Replace ('.', '_');
					string token = Guid.NewGuid ().ToString ().Replace ('-', '_');
					string handle = $"/org/freedesktop/portal/desktop/request/{sender}/{token}";
					Console.WriteLine ($"handle: {handle}");
					uint signal_id = connection.SignalSubscribe ("org.freedesktop.portal.Desktop", "org.freedesktop.portal.Request", "Response", handle, null, Gio.DBusSignalFlags.NoMatchRule, (_, _, _, _, _, _) => {
						Console.WriteLine ("received signal");
					});

					// The root window id should be set to allow proper parenting of the screenshot dialog.
					// However, the necessary functions are not correctly wrapped.
					// The empty string means that the compositor may unfortunately place the dialog wherever it pleases.
					// https://flatpak.github.io/xdg-desktop-portal/#parent_window
					var root_window_id = "";

					var parameters = new GLib.Variant (
						GLib.Variant.Create (root_window_id),
						// TODO - add the 'handle_token', 'modal', and 'interactive' parameters.
						GLib.Variant.CreateEmptyDictionary (GLib.VariantType.String, GLib.VariantType.Variant)
					);
					connection.Call ("org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop", "org.freedesktop.portal.Screenshot", "Screenshot", parameters);

				} catch (Exception e) {
					PintaCore.Chrome.ShowErrorDialog (PintaCore.Chrome.MainWindow,
						Translations.GetString ("Failed to take screenshot"),
						Translations.GetString ("Failed to access XDG Desktop Portals"),
						e.ToString ());
				}

				return;

			} else if (SystemManager.GetOperatingSystem () == OS.Mac) {
				try {
					// Launch the screencapture utility in interactive mode and save to the clipboard.
					// Note for testing: this requires screen recording permissions, so running from the generated .app bundle is required.
					const string screencapture_path = "/usr/sbin/screencapture";
					const string screencapture_args = "-iUc";

					var process = Process.Start (screencapture_path, screencapture_args);
					process.WaitForExit ();

					PintaCore.Actions.Edit.PasteIntoNewImage.Activate ();
				} catch (Exception e) {
					PintaCore.Chrome.ShowErrorDialog (PintaCore.Chrome.MainWindow,
						Translations.GetString ("Failed to take screenshot"), string.Empty, e.ToString ());
				}

				return;
			}

			PintaCore.Chrome.ShowMessageDialog (PintaCore.Chrome.MainWindow,
				Translations.GetString ("Failed to take screenshot"), string.Empty);
		}
	}
}
