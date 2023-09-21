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
using Pinta.Core;
using ScreenshotPortal.DBus;
using Tmds.DBus;

namespace Pinta.Actions;

internal sealed class NewScreenshotAction : IActionHandler
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

	async private void Activated (object sender, EventArgs args)
	{
		// GTK4 removed gdk_pixbuf_get_from_window(), so we need to use OS-specific APis to take a screenshot.
		// TODO-GTK4 - implement screenshots for Windows

		// On Linux, use the XDG Desktop Portal Screenshot API.
		if (SystemManager.GetOperatingSystem () == OS.X11) {
			try {
				// It's important that the portal interactions are synchronised with the main thread
				// Otherwise the use of the portals will cause massive instability and crash Pinta
				var systemConnection = new Connection (new ClientConnectionOptions (Address.Session) {
					AutoConnect = true,
					SynchronizationContext = System.Threading.SynchronizationContext.Current
				});

				var portal = systemConnection.CreateProxy<IScreenshot> (
					"org.freedesktop.portal.Desktop",
					"/org/freedesktop/portal/desktop");

				// The rootWindowID should be set to allow proper parenting of the screenshot dialog.
				// However, the necessary functions are not correctly wrapped.
				// The empty string means that the compositor may unfortunately place the dialog wherever it pleases.
				// https://flatpak.github.io/xdg-desktop-portal/#parent_window
				var rootWindowID = "";
				var portalOptions = new Dictionary<string, object> // Enables options such as delay, specific windows, etc.
				{
					["modal"] = true,
					["interactive"] = true,
				};

				var handle = await portal.ScreenshotAsync (rootWindowID, portalOptions);
				await handle.WatchResponseAsync (
					reply => {
						// response 0 == success, 1 == undefined error, 2 == user cancelled (not an error)
						// However the response 1 can occur when the user presses "Cancel" on the second stage of the UI
						// As a result, it's not reliable to throw error messages when the retval == 1
						if (reply.response != 0)
							return;

						string? uri = reply.results["uri"].ToString ();

						if (uri is null || !PintaCore.Workspace.OpenFile (Gio.FileHelper.NewForUri (uri)))
							return;

						// Mark as not having a file, so that the user doesn't unintentionally
						// save using the temp file.
						PintaCore.Workspace.ActiveDocument.ClearFileReference ();
					}
				);

			} catch (Tmds.DBus.DBusException e) {
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
