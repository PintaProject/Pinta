using System;
using System.Runtime.InteropServices;

namespace Pinta.Core;

public static partial class GskExtensions
{
	private const string GSK_LIBRARY_NAME = "Gsk";

	static GskExtensions ()
	{
		NativeImportResolver.RegisterLibrary (GSK_LIBRARY_NAME,
			windowsLibraryName: "libgtk-4-1.dll",
			linuxLibraryName: "libgtk-4.so.1",
			osxLibraryName: "libgtk-4.1.dylib"
		);
	}

	// TODO-GTK4 (bindings) - manual binding for AddCairoPath().
	[LibraryImport (GSK_LIBRARY_NAME, EntryPoint = "gsk_path_builder_add_cairo_path")]
	private static partial void PathBuilderAddCairoPath (Gsk.Internal.PathBuilderHandle handle, Cairo.Internal.PathHandle path);

	public static void AddCairoPath (this Gsk.PathBuilder builder, Cairo.Path path)
	{
		PathBuilderAddCairoPath (builder.Handle, path.Handle);
	}
}
