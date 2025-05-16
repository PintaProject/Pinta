//
// GioExtensions.cs
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

namespace Pinta.Core;

public static class GioExtensions
{
	private const string GIO_LIBRARY_NAME = "Gio";

	static GioExtensions ()
	{
		NativeImportResolver.RegisterLibrary (GIO_LIBRARY_NAME,
			windowsLibraryName: "libgio-2.0-0.dll",
			linuxLibraryName: "libgio-2.0.so.0",
			osxLibraryName: "libgio-2.0.0.dylib"
		);
	}

	/// <summary>
	/// Return the display name for the file. Note that this can be very different from file.Basename,
	/// and should only be used for display purposes rather than identifying the file.
	/// </summary>
	public static string GetDisplayName (this Gio.File file)
	{
		Gio.FileInfo info = file.QueryInfo (
			Gio.Constants.FILE_ATTRIBUTE_STANDARD_DISPLAY_NAME,
			Gio.FileQueryInfoFlags.None,
			cancellable: null);

		return info.GetDisplayName ();
	}

	/// <summary>
	/// Returns an output stream for creating or overwriting the file.
	/// NOTE: if you don't wrap this in a GLib.GioStream, you must call Close() !
	/// </summary>
	public static Gio.OutputStream Replace (this Gio.File file)
	{
		return file.Replace (
			etag: null,
			makeBackup: false,
			flags: Gio.FileCreateFlags.None,
			cancellable: null);
	}

	public static void Remove (this Gio.Menu menu, Command action)
	{
		for (int i = 0; i < menu.GetNItems (); ++i) {
			string name_attr = menu.GetItemAttributeValue (i, "action", GLib.VariantType.String)!.GetString (out nuint _);
			if (name_attr != action.FullName) continue;
			menu.Remove (i);
			return;
		}
	}

	public static void AppendMenuItemSorted (this Gio.Menu menu, Gio.MenuItem item)
	{
		string newLabel = item.GetAttributeValue ("label", GLib.VariantType.String)!.GetString (out nuint _);

		for (int i = 0; i < menu.GetNItems (); i++) {
			string label = menu.GetItemAttributeValue (i, "label", GLib.VariantType.String)!.GetString (out nuint _);
			if (string.Compare (label, newLabel) <= 0) continue;
			menu.InsertItem (i, item);
			return;
		}

		menu.AppendItem (item);
	}

	public static void RemoveMultiple (this Gio.ListStore store, uint position, uint n_removals)
	{
		store.Splice (position, n_removals, [], 0);
	}
}
