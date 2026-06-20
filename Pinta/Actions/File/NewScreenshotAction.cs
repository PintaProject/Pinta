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
using System.Threading.Tasks;
using Pinta.Core;
using Requests.DBus;
using ScreenshotPortal.DBus;
using Tmds.DBus.Protocol;

namespace Pinta.Actions;

internal sealed class NewScreenshotAction : IActionHandler
{
	private readonly SystemManager system;
	private readonly ChromeManager chrome;
	private readonly WorkspaceManager workspace;
	private readonly ActionManager actions;

	internal NewScreenshotAction (
		SystemManager system,
		ChromeManager chrome,
		WorkspaceManager workspace,
		ActionManager actions)
	{
		this.system = system;
		this.chrome = chrome;
		this.workspace = workspace;
		this.actions = actions;
	}

	void IActionHandler.Initialize ()
	{
		actions.File.NewScreenshot.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		actions.File.NewScreenshot.Activated -= Activated;
	}

	private async void Activated (object sender, EventArgs args)
	{
		// GTK4 removed gdk_pixbuf_get_from_window(), so we need to use OS-specific APis to take a screenshot.
		try {

			if (SystemManager.GetOperatingSystem () == OS.X11)
				await HandleX11 ();
			else if (SystemManager.GetOperatingSystem () == OS.Mac)
				HandleMac ();
			else if (SystemManager.GetOperatingSystem () == OS.Windows)
				await HandleWindows ();
			else
				HandleDefault ();

		} catch (DBusExceptionBase e) {

			await chrome.ShowErrorDialog (
				chrome.MainWindow,
				Translations.GetString ("Failed to take screenshot"),
				Translations.GetString ("Failed to access XDG Desktop Portals"),
				e.ToString ());

		} catch (NoHandlersForOSException e) {

			await chrome.ShowMessageDialog (
				chrome.MainWindow,
				e.Message,
				string.Empty);

		} catch (Exception ex) {

			await chrome.ShowErrorDialog (
				chrome.MainWindow,
				ex.Message,
				string.Empty,
				ex.ToString ());

		}
	}

	private sealed class NoHandlersForOSException : Exception
	{
		internal NoHandlersForOSException ()
			: base (Translations.GetString ("Failed to take screenshot"))
		{ }
	}
	private static void HandleDefault ()
		=> throw new NoHandlersForOSException ();

	private void HandleMac ()
	{
		// Launch the screencapture utility in interactive mode and save to the clipboard.
		// Note for testing: this requires screen recording permissions, so running from the generated .app bundle is required.
		const string screencapture_path = "/usr/sbin/screencapture";
		const string screencapture_args = "-iUc";

		var process = Process.Start (screencapture_path, screencapture_args);
		process.WaitForExit ();

		actions.Edit.PasteIntoNewImage.Activate ();
	}

	private async Task HandleX11 ()
	{
		// On Linux, use the XDG Desktop Portal Screenshot API.

		// It's important that the portal interactions are synchronised with the main thread
		// Otherwise the use of the portals will cause massive instability and crash Pinta
		using DBusConnection connection = new (DBusAddress.Session!);

		await connection.ConnectAsync();

		var portal = new Screenshot(connection,
			"org.freedesktop.portal.Desktop",
			"/org/freedesktop/portal/desktop");

		// The rootWindowID should be set to allow proper parenting of the screenshot dialog.
		// However, the necessary functions are not correctly wrapped.
		// The empty string means that the compositor may unfortunately place the dialog wherever it pleases.
		// https://flatpak.github.io/xdg-desktop-portal/#parent_window
		var rootWindowID = "";

		// https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Request.html
		string sender = (connection.UniqueName ?? "").TrimStart(':').Replace(".", "_");
		string token = "Pinta_" + Stopwatch.GetTimestamp().ToString();
		ObjectPath expectedPath = $"/org/freedesktop/portal/desktop/request/{sender}/{token}";

		// Enables options such as delay, specific windows, etc.
		Dictionary<string, VariantValue> portalOptions = new () {
			["modal"] = true,
			["interactive"] = true,
			["handle_token"] = token,
		};

		Request request = new Request (connection, "org.freedesktop.portal.Desktop", expectedPath);
		TaskCompletionSource requestCompletion = new ();
		using var _ = await request.WatchResponseAsync (
			(Notification<(uint Response, Dictionary<string, VariantValue> Results)> notification) =>
			{
				if (notification.IsCompletion)
				{
					requestCompletion.TrySetException (notification.Exception);
				}
				else
				{
					var reply = notification.Value;
					try
					{
						if (reply.Response != 0)
							return;

						string? uri = null;
						if (reply.Results.TryGetValue ("uri", out VariantValue vv) && vv.Type == VariantValueType.String)
							uri = vv.GetString ();

						if (uri is null || !workspace.OpenFile (Gio.FileHelper.NewForUri (uri)))
							return;

						// Mark as not having a file, so that the user doesn't unintentionally
						// save using the temp file.
						workspace.ActiveDocument.ClearFileReference ();
					}
					catch (Exception ex)
					{
						requestCompletion.TrySetException (ex);
					}
					finally
					{
						requestCompletion.TrySetResult ();
					}
				}
			}, ObserverFlags.EmitAll);
		ObjectPath actualPath = await portal.ScreenshotAsync (rootWindowID, portalOptions);

		if (actualPath != expectedPath)
		{
			throw new DBusUnexpectedValueException ("Request did not get the expected path.");
		}

		await requestCompletion.Task;
	}

	private async Task HandleWindows ()
	{
		// Launch the standard screen capture utility which will add to the clipboard.
		// https://learn.microsoft.com/en-us/windows/uwp/launch-resume/launch-screen-snipping#open-a-new-snip-from-your-app
		// LaunchUri() returns once the application is launched, not when it's finished, so we don't
		// currently have a way to be notified when we can add a new document. Listening for clipboard change
		// events could have many false positives.
		await system.LaunchUri ("ms-screenclip:");
	}
}
