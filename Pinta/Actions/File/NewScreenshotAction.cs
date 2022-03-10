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
using Gdk;
using Pinta.Core;
using ScreenshotPortal.DBus;
using Tmds.DBus;

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

		async private void Activated (object sender, EventArgs e)
		{
			// Use XDG Desktop Portal Screenshot API's to allow capturing screenshots on Wayland environments
			// Check for presence of the $WAYLAND_DISPLAY variable to indicate Wayland is being used.
			// Ideally GTK would give us this information instead, but the necessary functions aren't wrapped.
			if (SystemManager.GetOperatingSystem () == OS.X11) {
				try {
					// It's important that the portal interactions are synchronised with the main thread
					// Otherwise the use of the portals will cause massive instability and crash Pinta
					var systemConnection = new Connection (new ClientConnectionOptions (Address.Session) { AutoConnect = true, SynchronizationContext = GLib.GLibSynchronizationContext.Current });

					var portal = systemConnection.CreateProxy<IScreenshot> (
						"org.freedesktop.portal.Desktop",
						"/org/freedesktop/portal/desktop");

					// The rootWindowID should be set to allow proper parenting of the screenshot dialog.
					// However, the necessary functions are not correctly wrapped.
					// The empty string means that the compositor may unfortunately place the dialog wherever it pleases.
					// https://flatpak.github.io/xdg-desktop-portal/#parent_window
					var rootWindowID = "";
					var portalOptions = new Dictionary<string, object> {
						{ "modal", true },
						{ "interactive", true }}; // Enables options such as delay, specific windows, etc.

					var handle = await portal.ScreenshotAsync (rootWindowID, portalOptions);
					await handle.WatchResponseAsync (
						reply => {
							// response 0 == success, 1 == undefined error, 2 == user cancelled (not an error)
							// However the response 1 can occur when the user presses "Cancel" on the second stage of the UI
							// As a result, it's not reliable to throw error messages when the retval == 1
							if (reply.response == 0) {
								if (PintaCore.Workspace.OpenFile (GLib.FileFactory.NewForUri (reply.results["uri"].ToString ()))) {
									// Mark as not having a file, so that the user doesn't unintentionally
									// save using the temp file.
									PintaCore.Workspace.ActiveDocument.ClearFileReference ();
								}
							}
						}
					);
					return; // Prevent falling back to default screenshot behaviour

				} catch (Tmds.DBus.DBusException) {
					PintaCore.Chrome.ShowErrorDialog (PintaCore.Chrome.MainWindow,
						Translations.GetString ("Failed to take screenshot"),
						Translations.GetString ("Failed to access XDG Desktop Portals"));
					return; // Prevent falling back to default screenshot behaviour
				}
			}

			// Pinta's standard screenshotting logic for non-Wayland systems

			int delay = PintaCore.Settings.GetSetting<int> ("screenshot-delay", 0);

			using var dialog = new SpinButtonEntryDialog (Translations.GetString ("Take Screenshot"),
					PintaCore.Chrome.MainWindow, Translations.GetString ("Delay before taking a screenshot (seconds):"), 0, 300, delay);

			if (dialog.Run () == (int) Gtk.ResponseType.Ok) {
				delay = dialog.GetValue ();

				PintaCore.Settings.PutSetting ("screenshot-delay", delay);

				GLib.Timeout.Add ((uint) delay * 1000, () => {
					Screen screen = Screen.Default;
					var root_window = screen.RootWindow;
					int width = root_window.Width;
					int height = root_window.Height;

					if (width == 0 || height == 0) {
						// Something went wrong...
						// This might happen when running under wayland, see bug 1923241
						PintaCore.Chrome.ShowErrorDialog (PintaCore.Chrome.MainWindow,
							Translations.GetString ("Failed to take screenshot"),
							Translations.GetString ("Could not obtain the size of display '{0}'", screen.Display.Name));
						return false;
					}

					Document doc = PintaCore.Workspace.NewDocument (new Size (width, height), new Cairo.Color (1, 1, 1));

					using (var pb = new Pixbuf (root_window, 0, 0, width, height)) {
						using (Cairo.Context g = new Cairo.Context (doc.Layers.UserLayers[0].Surface)) {
							CairoHelper.SetSourcePixbuf (g, pb, 0, 0);
							g.Paint ();
						}
					}

					doc.IsDirty = true;

					if (!PintaCore.Chrome.MainWindow.IsActive) {
						PintaCore.Chrome.MainWindow.UrgencyHint = true;

						// Don't flash forever
						GLib.Timeout.Add (3 * 1000, () => PintaCore.Chrome.MainWindow.UrgencyHint = false);
					}

					return false;
				});
			}
		}
	}
}
