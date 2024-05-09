//
// GdkExtensions.cs
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
using System.Runtime.InteropServices;
using GdkPixbuf;

namespace Pinta.Core;

public static partial class GdkPixbufExtensions
{
	private const string PixbufLibraryName = "GdkPixbuf";

	static GdkPixbufExtensions ()
	{
		NativeImportResolver.RegisterLibrary (PixbufLibraryName,
			windowsLibraryName: "libgdk_pixbuf-2.0-0.dll",
			linuxLibraryName: "libgdk_pixbuf-2.0.so.0",
			osxLibraryName: "libgdk_pixbuf-2.0.0.dylib"
		);
	}

	// TODO-GTK4 (bindings, unsubmitted) - needs support for primitive value arrays
	public static byte[] SaveToBuffer (this Pixbuf pixbuf, string type)
	{
		SaveToBufferv (pixbuf.Handle, out IntPtr buffer, out uint buffer_size, type, IntPtr.Zero, IntPtr.Zero, out var error);
		if (!error.IsInvalid)
			throw new GLib.GException (error);

		var result = new byte[buffer_size];
		Marshal.Copy (buffer, result, 0, (int) buffer_size);
		GLib.Functions.Free (buffer);
		return result;
	}

	// Higher-level wrapper around GdkPixbuf.Pixbuf.GetFormats(), which returns only a GLib.SList.
	// TODO-GTK4 (bindings) - record methods are not generated (https://github.com/gircore/gir.core/issues/743)
	public static PixbufFormat[] GetFormats ()
	{
		var slist = new GLib.SList (GetFormatsNative ());
		uint n = GLib.SList.Length (slist);
		var result = new PixbufFormat[n];
		for (uint i = 0; i < n; ++i) {
			var format = new GdkPixbuf.Internal.PixbufFormatUnownedHandle (GLib.SList.NthData (slist, i));
			result[i] = new PixbufFormat (GdkPixbuf.Internal.PixbufFormat.Copy (format));
		}

		return result;
	}

	[DllImport (PixbufLibraryName, EntryPoint = "gdk_pixbuf_get_formats")]
	private static extern GLib.Internal.SListOwnedHandle GetFormatsNative ();

	[DllImport (PixbufLibraryName, EntryPoint = "gdk_pixbuf_save_to_bufferv")]
	private static extern bool SaveToBufferv (IntPtr pixbuf, out IntPtr buffer, out uint buffer_size, [MarshalAs (UnmanagedType.LPUTF8Str)] string type, IntPtr option_keys, IntPtr option_values, out GLib.Internal.ErrorOwnedHandle error);
}
