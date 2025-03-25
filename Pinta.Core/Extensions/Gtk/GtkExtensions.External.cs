using System;
using System.Runtime.InteropServices;

namespace Pinta.Core;

partial class GtkExtensions
{
	// TODO-GTK4 (bindings, unsubmitted) - need support for 'out' enum parameters.
	[LibraryImport (GTK_LIBRARY_NAME, EntryPoint = "gtk_accelerator_parse")]
	[return: MarshalAs (UnmanagedType.Bool)]
	private static partial bool AcceleratorParse (
		[MarshalAs (UnmanagedType.LPUTF8Str)] string accelerator,
		out uint accelerator_key,
		out Gdk.ModifierType accelerator_mods);

	// TODO-GTK4 (bindings) - structs are not generated (https://github.com/gircore/gir.core/issues/622)
	[StructLayout (LayoutKind.Sequential)]
	private struct GdkRGBA
	{
		public float Red;
		public float Green;
		public float Blue;
		public float Alpha;
	}

	[LibraryImport (GTK_LIBRARY_NAME, EntryPoint = "gtk_style_context_get_color")]
	private static partial void StyleContextGetColor (IntPtr handle, out GdkRGBA color);

	[LibraryImport (GTK_LIBRARY_NAME, EntryPoint = "gtk_color_chooser_get_rgba")]
	private static partial void ColorChooserGetRgba (
		IntPtr handle,
		out GdkRGBA color);

	// Manual binding for GetPreeditString
	// TODO-GTK4 (bindings) - missing from gir.core: "opaque record parameter 'attrs' with direction != in not yet supported"
	[DllImport (GTK_LIBRARY_NAME, EntryPoint = "gtk_im_context_get_preedit_string")]
	private static extern void IMContextGetPreeditString (
		IntPtr handle,
		out GLib.Internal.NonNullableUtf8StringOwnedHandle str,
		out Pango.Internal.AttrListOwnedHandle attrs,
		out int cursor_pos);
}
