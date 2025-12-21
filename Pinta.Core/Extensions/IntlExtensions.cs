using System;
using System.Runtime.InteropServices;

namespace Pinta.Core;

/// <summary>
/// Extensions for libintl (gettext), which is used by the native GTK-related libraries.
/// </summary>
public static partial class IntlExtensions
{
	private const string IntlLibraryName = "Intl";

	public const string AdwaitaTextDomain = "libadwaita";

	static IntlExtensions ()
	{
		NativeImportResolver.RegisterLibrary (
			IntlLibraryName,
			windowsLibraryName: "libintl-8.dll",
			linuxLibraryName: "libintl.so.8",
			osxLibraryName: "libintl.8.dylib");
	}

	[LibraryImport (IntlLibraryName, EntryPoint = "bindtextdomain")]
	private static partial IntPtr InternalBindTextDomain (
		GLib.Internal.NonNullablePlatformStringHandle domain,
		GLib.Internal.NonNullablePlatformStringHandle dir);

	public static void BindTextDomain (string domain, string dir)
	{
		InternalBindTextDomain (
			GLib.Internal.NonNullablePlatformStringUnownedHandle.Create (domain),
			GLib.Internal.NonNullablePlatformStringUnownedHandle.Create (dir));
	}
}
