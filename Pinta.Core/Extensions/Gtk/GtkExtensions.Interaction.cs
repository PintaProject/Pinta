namespace Pinta.Core;

partial class GtkExtensions
{
	/// <summary>
	/// Convert from GetCurrentButton to the MouseButton enum.
	/// </summary>
	public static MouseButton GetCurrentMouseButton (this Gtk.GestureClick gesture)
	{
		uint button = gesture.GetCurrentButton ();
		return button switch {
			MOUSE_LEFT_BUTTON => MouseButton.Left,
			MOUSE_MIDDLE_BUTTON => MouseButton.Middle,
			MOUSE_RIGHT_BUTTON => MouseButton.Right,
			_ => MouseButton.None
		};
	}

	/// <summary>
	/// Convert the "<Primary>" accelerator to the Ctrl or Command key, depending on the platform.
	/// This was done automatically in GTK3, but does not happen in GTK4.
	/// </summary>
	private static string ConvertPrimaryKey (this SystemManager system, string accel) =>
		accel.Replace ("<Primary>", system.OperatingSystem == OS.Mac ? "<Meta>" : "<Control>");

	private static string ConvertPrimaryKey (string accel) =>
		accel.Replace ("<Primary>", SystemManager.GetOperatingSystem () == OS.Mac ? "<Meta>" : "<Control>");

	/// <summary>
	/// Returns the platform-specific label for the "Primary" (Ctrl) key.
	/// For example, this is the Cmd key on macOS.
	/// </summary>
	public static string CtrlLabel (this SystemManager system)
	{
		AcceleratorParse (
			system.ConvertPrimaryKey ("<Primary>"),
			out var key,
			out var mods);

		return Gtk.Functions.AcceleratorGetLabel (key, mods);
	}

	private static string ReadableAcceleratorLabel (string gtkLabel)
	{
		AcceleratorParse (
			ConvertPrimaryKey (gtkLabel),
			out uint key,
			out Gdk.ModifierType mods);

		return Gtk.Functions.AcceleratorGetLabel (key, mods);
	}

	/// <summary>
	/// Returns the platform-specific label for the Alt key.
	/// For example, this is the Option key on macOS.
	/// </summary>
	public static string AltLabel ()
	{
		AcceleratorParse ("<Alt>", out var key, out var mods);
		return Gtk.Functions.AcceleratorGetLabel (key, mods);
	}

	/// <summary>
	/// Provides convenient access to the Gdk.Key of the key being pressed.
	/// </summary>
	public static Gdk.Key GetKey (this Gtk.EventControllerKey.KeyPressedSignalArgs args)
		=> new Gdk.Key (args.Keyval);

	/// <summary>
	/// Provides convenient access to the Gdk.Key of the key being released.
	/// </summary>
	public static Gdk.Key GetKey (this Gtk.EventControllerKey.KeyReleasedSignalArgs args)
		=> new Gdk.Key (args.Keyval);

	public static void GetPreeditString (
		this Gtk.IMContext context,
		out string str,
		out Pango.AttrList attrs,
		out int cursor_pos)
	{
		IMContextGetPreeditString (
			context.Handle.DangerousGetHandle (),
			out var str_handle,
			out var attrs_handle,
			out cursor_pos);

		str = str_handle.ConvertToString ();
		str_handle.Dispose ();
		attrs = new Pango.AttrList (attrs_handle);
	}
}
