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

	// Manual binding for GetPreeditString
	// TODO-GTK4 (bindings) - missing from gir.core: "opaque record parameter 'attrs' with direction != in not yet supported"
	[DllImport (GTK_LIBRARY_NAME, EntryPoint = "gtk_im_context_get_preedit_string")]
	private static extern void IMContextGetPreeditString (
		IntPtr handle,
		out GLib.Internal.NonNullableUtf8StringOwnedHandle str,
		out Pango.Internal.AttrListOwnedHandle attrs,
		out int cursor_pos);
}
