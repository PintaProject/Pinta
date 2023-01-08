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
using GLib.Internal;

namespace Pinta.Core
{
	public static class GdkPixbufExtensions
	{
		// TODO-GTK4 - needs a proper binding in gir.core
		public static Pixbuf NewPixbufFromStream (Gio.InputStream stream, Gio.Cancellable? cancellable)
		{
			GLib.Internal.ErrorOwnedHandle error;
			IntPtr result = GdkPixbuf.Internal.Pixbuf.NewFromStream (stream.Handle, cancellable == null ? IntPtr.Zero : cancellable.Handle, out error);
			GLib.Error.ThrowOnError (error);
			return new PixbufWrapper (result, true);
		}

		// TODO-GTK4 - needs a proper binding in gir.core
		public static Pixbuf NewPixbufFromFileAtScale (string filename, int width, int height, bool preserve_aspect_ratio)
		{
			GLib.Internal.ErrorOwnedHandle error;
			IntPtr result = GdkPixbuf.Internal.Pixbuf.NewFromFileAtScale (filename, width, height, preserve_aspect_ratio, out error);
			GLib.Error.ThrowOnError (error);
			return new PixbufWrapper (result, true);
		}

		// TODO-GTK4 - needs a proper binding in gir.core
		public static bool SaveToStream (this Pixbuf pixbuf, Gio.OutputStream stream, string type, Gio.Cancellable? cancellable)
		{
			GLib.Internal.ErrorOwnedHandle error;
			bool result = GdkPixbuf.Internal.Pixbuf.SaveToStreamv (pixbuf.Handle, stream.Handle, type, IntPtr.Zero, IntPtr.Zero, cancellable == null ? IntPtr.Zero : cancellable.Handle, out error);
			GLib.Error.ThrowOnError (error);
			return result;
		}

		internal class PixbufWrapper : Pixbuf
		{
			internal PixbufWrapper (IntPtr ptr, bool ownedRef) : base (ptr, ownedRef)
			{
			}
		}

		// Higher-level wrapper around GdkPixbuf.Pixbuf.GetFormats(), which returns only a GLib.SList.
		// TODO-GTK4 - gir.core doesn't have public bindings for GLib.SList functions
		public static PixbufFormat[] GetFormats ()
		{
			// FIXME - using an unowned handle here because SListOwnedHandle is not correctly implemented in gir.core
			var slist_handle = GetFormatsNative ();
			uint n = GLib.Internal.SList.Length (slist_handle);
			var result = new PixbufFormat[n];
			for (uint i = 0; i < n; ++i) {
				var format = new GdkPixbuf.Internal.PixbufFormatUnownedHandle (GLib.Internal.SList.NthData (slist_handle, i));
				result[i] = new PixbufFormat (format);
			}

			return result;
		}

		// TODO-GTK4 - gir.core doesn't yet generate bindings for record member functions.
		public static string GetName (this PixbufFormat format)
		{
			return GdkPixbuf.Internal.PixbufFormat.GetName (format.Handle);
		}

		// TODO-GTK4 - gir.core doesn't yet generate bindings for record member functions.
		public static bool IsWritable (this PixbufFormat format)
		{
			return GdkPixbuf.Internal.PixbufFormat.IsWritable (format.Handle);
		}

		// TODO-GTK4 - gir.core doesn't yet generate bindings for record member functions.
		public static string[] GetMimeTypes (this PixbufFormat format)
		{
			IntPtr resultNative = GetMimeTypes (format.Handle);
			// FIXME - this does not free the result!
			return GLib.Internal.StringHelper.ToStringArrayUtf8 (resultNative);
		}

		[DllImport ("gdk_pixbuf-2.0", EntryPoint = "gdk_pixbuf_format_get_mime_types")]
		private static extern IntPtr GetMimeTypes (GdkPixbuf.Internal.PixbufFormatHandle format);

		[DllImport ("gdk_pixbuf-2.0", EntryPoint = "gdk_pixbuf_get_formats")]
		private static extern SListUnownedHandle GetFormatsNative ();
	}
}
