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

using System;
using System.Collections.Generic;

namespace Pinta.Core;

public static class GioExtensions
{
	private const string GioLibraryName = "Gio";

	static GioExtensions ()
	{
		NativeImportResolver.RegisterLibrary (GioLibraryName,
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
			attributes: Gio.Constants.FILE_ATTRIBUTE_STANDARD_DISPLAY_NAME,
			flags: Gio.FileQueryInfoFlags.None,
			cancellable: null);
		return info.GetDisplayName ();
	}

	public static IEnumerable<Gio.File> EnumerateAsFiles (this Gio.ListModel fileList)
	{
		for (uint i = 0, n = fileList.GetNItems (); i < n; i++)
			yield return new Gio.FileHelper (fileList.GetItem (i), ownedRef: true);
	}

	/// <summary>
	/// Returns an output stream for creating or overwriting the file.
	/// NOTE: if you don't wrap this in a GLib.GioStream, you must call Close() !
	/// </summary>
	public static Gio.OutputStream Replace (this Gio.File file)
	{
		return file.Replace (null, false, Gio.FileCreateFlags.None, null);
	}

	public static void Remove (this Gio.Menu menu, Command action)
	{
		for (int i = 0; i < menu.GetNItems (); ++i) {
			var name_attr = menu.GetItemAttributeValue (i, "action", GLib.VariantType.String)!.GetString (out var _);
			if (name_attr == action.FullName) {
				menu.Remove (i);
				return;
			}
		}
	}

	public static void AppendMenuItemSorted (this Gio.Menu menu, Gio.MenuItem item)
	{
		var new_label = item.GetAttributeValue ("label", GLib.VariantType.String)!.GetString (out var _);

		for (int i = 0; i < menu.GetNItems (); i++) {
			var label = menu.GetItemAttributeValue (i, "label", GLib.VariantType.String)!.GetString (out var _);
			if (string.Compare (label, new_label) > 0) {
				menu.InsertItem (i, item);
				return;
			}
		}

		menu.AppendItem (item);
	}

	public static void RemoveMultiple (this Gio.ListStore store, uint position, uint n_removals)
	{
		store.Splice (position, n_removals, Array.Empty<GObject.Object> (), 0);
	}
}
