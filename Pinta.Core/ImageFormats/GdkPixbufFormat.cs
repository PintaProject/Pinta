// 
// GdkPixbufFormat.cs
//  
// Author:
//       Maia Kozheva <sikon@ubuntu.com>
// 
// Copyright (c) 2010 Maia Kozheva <sikon@ubuntu.com>
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
using System.IO;
using System.Runtime.InteropServices;

using Gdk;

namespace Pinta.Core
{
	public class GdkPixbufFormat: IImageImporter, IImageExporter
	{
		private string filetype;

		public GdkPixbufFormat(string filetype)
		{
			this.filetype = filetype;
		}

		#region IImageImporter implementation

		public void Import (string fileName, Gtk.Window parent)
		{
			Pixbuf bg;

			// Handle any EXIF orientation flags
			using (var fs = new FileStream (fileName, FileMode.Open, FileAccess.Read))
				bg = new Pixbuf (fs);

			bg = bg.ApplyEmbeddedOrientation ();

			Size imagesize = new Size (bg.Width, bg.Height);

			Document doc = PintaCore.Workspace.CreateAndActivateDocument (fileName, imagesize);
			doc.HasFile = true;
			doc.ImageSize = imagesize;
			doc.Workspace.CanvasSize = imagesize;

			Layer layer = doc.AddNewLayer (Path.GetFileName (fileName));

			using (Cairo.Context g = new Cairo.Context (layer.Surface)) {
				CairoHelper.SetSourcePixbuf (g, bg, 0, 0);
				g.Paint ();
			}

			bg.Dispose ();
		}

		public Pixbuf LoadThumbnail (string filename, int maxWidth, int maxHeight, Gtk.Window parent)
		{
			int imageWidth;
			int imageHeight;
			Pixbuf pixbuf = null;

			var imageInfo = Gdk.Pixbuf.GetFileInfo (filename, out imageWidth, out imageHeight);

			if (imageInfo == null) {
				return null;
			}

			// Scale down images that are too large, but don't scale up small images.
			if (imageWidth > maxWidth || imageHeight > maxHeight) {
				pixbuf = new Gdk.Pixbuf (filename, maxWidth, maxHeight, true);
			} else {
				pixbuf = new Gdk.Pixbuf (filename);
			}

			return pixbuf;
		}

		#endregion
		
		protected virtual void DoSave (Pixbuf pb, string fileName, string fileType, Gtk.Window parent)
		{
			pb.SaveUtf8(fileName, fileType);
		}

		public void Export (Document document, string fileName, Gtk.Window parent)
		{
			Cairo.ImageSurface surf = document.GetFlattenedImage ();
	
			Pixbuf pb = surf.ToPixbuf ();
			DoSave(pb, fileName, filetype, parent);

			(pb as IDisposable).Dispose ();
			(surf as IDisposable).Dispose ();
		}
	}

	/// <summary>
	/// Contains bindings for gdk_pixbuf_save_utf8 and gdk_pixbuf_savev_utf8, which are not exposed by gtk-sharp.
	/// </summary>
	internal static class PixbufExtensions
	{
		[DllImport ("libgdk_pixbuf-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern bool gdk_pixbuf_save_utf8 (IntPtr raw, IntPtr filename, IntPtr type, out IntPtr error, IntPtr dummy);

		public static bool SaveUtf8 (this Pixbuf pb, string filename, string type)
		{
			if (PintaCore.System.OperatingSystem == OS.Windows) {
				IntPtr error = IntPtr.Zero;
				IntPtr native_filename = GLib.Marshaller.StringToPtrGStrdup (filename);
				IntPtr native_type = GLib.Marshaller.StringToPtrGStrdup (type);
				bool result = gdk_pixbuf_save_utf8 (pb.Handle, native_filename, native_type, out error, IntPtr.Zero);
				GLib.Marshaller.Free (native_filename);
				GLib.Marshaller.Free (native_type);
				if (error != IntPtr.Zero) {
					throw new GLib.GException (error);
				}
				return result;
			} else {
				return pb.Save (filename, type);
			}
		}

        [DllImport("libgdk_pixbuf-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern bool gdk_pixbuf_savev_utf8 (IntPtr raw, IntPtr filename, IntPtr type, IntPtr[] option_keys, IntPtr[] option_values, out IntPtr error);

		public static bool SavevUtf8 (this Pixbuf pb, string filename, string type, string[] option_keys, string[] option_values)
		{
			if (PintaCore.System.OperatingSystem == OS.Windows) {
				IntPtr native_filename = GLib.Marshaller.StringToPtrGStrdup (filename);
				IntPtr native_type = GLib.Marshaller.StringToPtrGStrdup (type);

				int num = (option_keys == null) ? 0 : option_keys.Length;
				IntPtr[] native_keys = new IntPtr[num];
				for (int i = 0; i < num; i++) {
					native_keys[i] = GLib.Marshaller.StringToPtrGStrdup (option_keys[i]);
				}

				int num2 = (option_values == null) ? 0 : option_values.Length;
				IntPtr[] native_values = new IntPtr[num2];
				for (int j = 0; j < num2; j++) {
					native_values[j] = GLib.Marshaller.StringToPtrGStrdup (option_values[j]);
				}

				IntPtr error = IntPtr.Zero;
				bool result = gdk_pixbuf_savev_utf8 (pb.Handle, native_filename, native_type, native_keys, native_values, out error);

				GLib.Marshaller.Free (native_filename);
				GLib.Marshaller.Free (native_type);
				ReleaseArray (native_keys);
				ReleaseArray (native_values);

				if (error != IntPtr.Zero) {
					throw new GLib.GException (error);
				}
				return result;
			} else {
				return pb.Savev (filename, type, option_keys, option_values);
			}
		}

		private static void ReleaseArray (IntPtr[] arr)
		{
			foreach (IntPtr p in arr)
				GLib.Marshaller.Free (p);
		}
	}
}
