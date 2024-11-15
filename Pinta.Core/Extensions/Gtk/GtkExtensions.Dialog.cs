using System.Threading.Tasks;

namespace Pinta.Core;

partial class GtkExtensions
{
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
