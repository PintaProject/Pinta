//
// GtkExtensions.cs
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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pinta.Core;

partial class GtkExtensions
{
	public static async Task<Gio.File?> OpenFileAsync (
		this Gtk.FileDialog fileDialog,
		Gtk.Window parent)
	{
		Gio.File? choice;
		try {
			choice = await fileDialog.OpenAsync (parent);
		} catch (GLib.GException) {
			// Docs: https://docs.gtk.org/gtk4/method.FileDialog.open_finish.html
			// According to the documentation, an error is set if the user cancels
			// TODO: filter by error code once gir.core allows for that
			return null;
		}

		return choice;
	}

	public static async Task<IReadOnlyList<Gio.File>?> OpenFilesAsync (
		this Gtk.FileDialog fileDialog,
		Gtk.Window parent)
	{
		Gio.ListModel? selection;
		try {
			selection = await fileDialog.OpenMultipleAsync (parent);
		} catch (GLib.GException) {
			// Docs: https://docs.gtk.org/gtk4/method.FileDialog.open_multiple_finish.html
			// According to the documentation, an error is set if the user cancels
			// TODO: filter by error code once gir.core allows for that
			return null;
		}

		if (selection is null) return null;

		uint itemCount = selection.GetNItems ();
		var result = new Gio.File[itemCount];
		for (uint i = 0; i < itemCount; i++) {
			nint g_ref = selection.GetItem (i);
			Gio.FileHelper file = new (handle: g_ref, ownedRef: true);
			result[i] = file;
		}
		return result;
	}

	public static Task<Gtk.ResponseType> ShowAsync (this Gtk.NativeDialog dialog, bool dispose = false)
	{
		TaskCompletionSource<Gtk.ResponseType> completionSource = new ();

		void ResponseCallback (
			Gtk.NativeDialog sender,
			Gtk.NativeDialog.ResponseSignalArgs args)
		{
			completionSource.SetResult ((Gtk.ResponseType) args.ResponseId);
			dialog.OnResponse -= ResponseCallback;
			if (dispose) dialog.Dispose ();
		}

		dialog.OnResponse += ResponseCallback;
		dialog.Show ();

		return completionSource.Task;
	}

	/// <summary>
	/// Similar to gtk_dialog_run() in GTK3, this runs the dialog in a blocking manner with a nested event loop.
	/// This can be useful for compatibility with old code that relies on this behaviour, but new code should be
	/// structured to use event handlers.
	/// </summary>
	public static string RunBlocking (this Adw.MessageDialog dialog)
	{
		string response = "";
		var loop = GLib.MainLoop.New (null, false);

		if (!dialog.Modal)
			dialog.Modal = true;

		dialog.OnResponse += (_, args) => {
			response = args.Response;
			if (loop.IsRunning ())
				loop.Quit ();
		};

		dialog.Show ();
		loop.Run ();

		return response;
	}

	/// <summary>
	/// Similar to gtk_dialog_run() in GTK3, this runs the dialog in a blocking manner with a nested event loop.
	/// This can be useful for compatibility with old code that relies on this behaviour, but new code should be
	/// structured to use event handlers.
	/// </summary>
	public static Gtk.ResponseType RunBlocking (this Gtk.NativeDialog dialog)
	{
		var response = Gtk.ResponseType.None;
		var loop = GLib.MainLoop.New (null, false);

		if (!dialog.Modal)
			dialog.Modal = true;

		dialog.OnResponse += (_, args) => {
			response = (Gtk.ResponseType) args.ResponseId;
			if (loop.IsRunning ())
				loop.Quit ();
		};

		dialog.Show ();
		loop.Run ();

		return response;
	}

	/// <summary>
	/// Similar to gtk_dialog_run() in GTK3, this runs the dialog in a blocking manner with a nested event loop.
	/// This can be useful for compatibility with old code that relies on this behaviour, but new code should be
	/// structured to use event handlers.
	/// </summary>
	public static Gtk.ResponseType RunBlocking (this Gtk.Dialog dialog)
	{
		var response = Gtk.ResponseType.None;
		var loop = GLib.MainLoop.New (null, false);

		if (!dialog.Modal)
			dialog.Modal = true;

		dialog.OnResponse += (_, args) => {

			response = (Gtk.ResponseType) args.ResponseId;

			if (loop.IsRunning ())
				loop.Quit ();
		};

		dialog.Show ();
		loop.Run ();

		return response;
	}

	// TODO-GTK4 (bindings) - replace with adw_message_dialog_choose() once adwaita 1.3 is available, like in v0.4 of gir.core
	public static Task<string> RunAsync (this Adw.MessageDialog dialog, bool dispose = false)
	{
		TaskCompletionSource<string> completionSource = new ();

		void ResponseCallback (
			Adw.MessageDialog sender,
			Adw.MessageDialog.ResponseSignalArgs args)
		{
			completionSource.SetResult (args.Response);
			dialog.OnResponse -= ResponseCallback;
			if (dispose) dialog.Dispose ();
		}

		dialog.OnResponse += ResponseCallback;
		dialog.Present ();

		return completionSource.Task;
	}

	public static Task<Gtk.ResponseType> RunAsync (this Gtk.Dialog dialog, bool dispose = false)
	{
		TaskCompletionSource<Gtk.ResponseType> completionSource = new ();

		void ResponseCallback (
			Gtk.Dialog sender,
			Gtk.Dialog.ResponseSignalArgs args)
		{
			completionSource.SetResult ((Gtk.ResponseType) args.ResponseId);
			dialog.OnResponse -= ResponseCallback;
			if (dispose) dialog.Dispose ();
		}

		dialog.OnResponse += ResponseCallback;
		dialog.Present ();

		return completionSource.Task;
	}

	public static void SetDefaultResponse (
		this Gtk.Dialog dialog,
		Gtk.ResponseType response
	) => dialog.SetDefaultResponse ((int) response);

	public static void SetColor (this Gtk.ColorChooserDialog dialog, Cairo.Color color)
	{
		dialog.SetRgba (new Gdk.RGBA {
			Red = (float) color.R,
			Blue = (float) color.B,
			Green = (float) color.G,
			Alpha = (float) color.A
		});
	}

	// TODO-GTK4 (bindings) - structs are not generated (https://github.com/gircore/gir.core/issues/622)
	public static void GetColor (this Gtk.ColorChooserDialog dialog, out Cairo.Color color)
	{
		ColorChooserGetRgba (dialog.Handle, out var gdk_color);
		color = new Cairo.Color (gdk_color.Red, gdk_color.Green, gdk_color.Blue, gdk_color.Alpha);
	}
}
