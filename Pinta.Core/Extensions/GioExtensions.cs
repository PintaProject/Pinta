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
using System.Linq;
using System.Runtime.InteropServices;
using Gio;

namespace Pinta.Core
{
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
#if false // TODO-GTK4 - needs gir.core bindings
			var info = file.QueryInfo (Constants.FILE_ATTRIBUTE_STANDARD_DISPLAY_NAME, GLib.FileQueryInfoFlags.None, cancellable: null);
			return info.DisplayName;
#else
			return file.GetBasename ()!;
#endif
		}

		/// <summary>
		/// Returns an output stream for creating or overwriting the file.
		/// NOTE: if you don't wrap this in a GLib.GioStream, you must call Close() !
		/// </summary>
		public static Gio.OutputStream Replace (this Gio.File file)
		{
#if false // TODO-GTK4 - needs gir.core bindings
			return file.Replace (null, false, Gio.FileCreateFlags.None, null);
#else
			GLib.Internal.ErrorOwnedHandle error;
			IntPtr result = Gio.Internal.File.Replace (file.Handle, null, false, Gio.FileCreateFlags.None, IntPtr.Zero, out error);
			GLib.Error.ThrowOnError (error);
			return new FileOutputStreamWrapper (result, true);
#endif
		}

		internal class FileOutputStreamWrapper : Gio.FileOutputStream
		{
			internal FileOutputStreamWrapper (IntPtr ptr, bool ownedRef) : base (ptr, ownedRef)
			{
			}
		}

		// TODO-GTK4 - needs a proper binding in gir.core
		public static Gio.FileInputStream Read (this Gio.File file, Gio.Cancellable? cancellable)
		{
			GLib.Internal.ErrorOwnedHandle error;
			IntPtr result = Gio.Internal.File.Read (file.Handle, cancellable == null ? IntPtr.Zero : cancellable.Handle, out error);
			GLib.Error.ThrowOnError (error);
			return new FileInputStreamWrapper (result, true);
		}

		internal class FileInputStreamWrapper : Gio.FileInputStream
		{
			internal FileInputStreamWrapper (IntPtr ptr, bool ownedRef) : base (ptr, ownedRef)
			{
			}
		}

		// TODO-GTK4 - needs a proper binding in gir.core
		public static Gio.FileInfo QueryInfo (this Gio.FileInputStream stream, string attributes, Gio.Cancellable? cancellable)
		{
			GLib.Internal.ErrorOwnedHandle error;
			IntPtr result = Gio.Internal.FileInputStream.QueryInfo (stream.Handle, attributes, cancellable == null ? IntPtr.Zero : cancellable.Handle, out error);
			GLib.Error.ThrowOnError (error);
			return new FileInfoWrapper (result, true);
		}

		// TODO-GTK4 - needs a proper binding in gir.core
		public static Gio.FileInfo QueryInfo (this Gio.FileOutputStream stream, string attributes, Gio.Cancellable? cancellable)
		{
			GLib.Internal.ErrorOwnedHandle error;
			IntPtr result = Gio.Internal.FileOutputStream.QueryInfo (stream.Handle, attributes, cancellable == null ? IntPtr.Zero : cancellable.Handle, out error);
			GLib.Error.ThrowOnError (error);
			return new FileInfoWrapper (result, true);
		}

		// TODO-GTK4 - needs a proper binding in gir.core
		public static Gio.FileInfo QueryInfo (this Gio.FileIOStream stream, string attributes, Gio.Cancellable? cancellable)
		{
			GLib.Internal.ErrorOwnedHandle error;
			IntPtr result = Gio.Internal.FileIOStream.QueryInfo (stream.Handle, attributes, cancellable == null ? IntPtr.Zero : cancellable.Handle, out error);
			GLib.Error.ThrowOnError (error);
			return new FileInfoWrapper (result, true);
		}

		internal class FileInfoWrapper : Gio.FileInfo
		{
			internal FileInfoWrapper (IntPtr ptr, bool ownedRef) : base (ptr, ownedRef)
			{
			}
		}

		// TODO-GTK4 - needs a proper binding in gir.core
		public static long Read (this Gio.InputStream stream, byte[] buffer, uint count, Gio.Cancellable? cancellable)
		{
			GLib.Internal.ErrorOwnedHandle error;
			long result = Read (stream.Handle, buffer, count, cancellable == null ? IntPtr.Zero : cancellable.Handle, out error);
			GLib.Error.ThrowOnError (error);
			return result;
		}

		// Manual dllimport since the generated gir.core internal function is incorrect (https://github.com/gircore/gir.core/issues/763)
		[DllImport (GioLibraryName, EntryPoint = "g_input_stream_read")]
		private static extern long Read (IntPtr stream, byte[] buffer, uint count, IntPtr cancellable, out GLib.Internal.ErrorOwnedHandle error);

		// TODO-GTK4 - needs a proper binding in gir.core
		public static long Write (this Gio.OutputStream stream, byte[] buffer, uint count, Gio.Cancellable? cancellable)
		{
			GLib.Internal.ErrorOwnedHandle error;
			long result = Gio.Internal.OutputStream.Write (stream.Handle, buffer, count, cancellable == null ? IntPtr.Zero : cancellable.Handle, out error);
			GLib.Error.ThrowOnError (error);
			return result;
		}

		// TODO-GTK4 - needs a proper binding in gir.core
		public static bool Seek (this Gio.Seekable seekable, long offset, GLib.SeekType seek_type, Gio.Cancellable? cancellable)
		{
			GLib.Internal.ErrorOwnedHandle error;
			bool result = Gio.Internal.Seekable.Seek (seekable.Handle, offset, seek_type, cancellable == null ? IntPtr.Zero : cancellable.Handle, out error);
			GLib.Error.ThrowOnError (error);
			return result;
		}

		// TODO-GTK4 - needs a proper binding in gir.core
		public static bool Close (this Gio.InputStream stream, Gio.Cancellable? cancellable)
		{
			GLib.Internal.ErrorOwnedHandle error;
			bool result = Gio.Internal.InputStream.Close (stream.Handle, cancellable == null ? IntPtr.Zero : cancellable.Handle, out error);
			GLib.Error.ThrowOnError (error);
			return result;
		}

		// TODO-GTK4 - needs a proper binding in gir.core
		public static bool Close (this Gio.OutputStream stream, Gio.Cancellable? cancellable)
		{
			GLib.Internal.ErrorOwnedHandle error;
			bool result = Gio.Internal.OutputStream.Close (stream.Handle, cancellable == null ? IntPtr.Zero : cancellable.Handle, out error);
			GLib.Error.ThrowOnError (error);
			return result;
		}

		// TODO-GTK4 - needs a proper binding in gir.core
		public static bool Close (this Gio.IOStream stream, Gio.Cancellable? cancellable)
		{
			GLib.Internal.ErrorOwnedHandle error;
			bool result = Gio.Internal.IOStream.Close (stream.Handle, cancellable == null ? IntPtr.Zero : cancellable.Handle, out error);
			GLib.Error.ThrowOnError (error);
			return result;
		}

		public static void Remove (this Gio.Menu menu, Command action)
		{
			for (int i = 0; i < menu.GetNItems (); ++i) {
				var name_attr = menu.GetItemAttributeValue (i, "action", GLib.VariantType.String).GetString ();
				if (name_attr == action.FullName) {
					menu.Remove (i);
					return;
				}
			}
		}

		public static void AppendMenuItemSorted (this Gio.Menu menu, Gio.MenuItem item)
		{
			var new_label = item.GetAttributeValue ("label", GLib.VariantType.String).GetString ();

			for (int i = 0; i < menu.GetNItems (); i++) {
				var label = menu.GetItemAttributeValue (i, "label", GLib.VariantType.String).GetString ();
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
}
