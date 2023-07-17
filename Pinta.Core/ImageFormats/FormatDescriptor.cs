// 
// FormatDescriptor.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Gtk;

namespace Pinta.Core
{
	/// <summary>
	/// Describes information about a file format, such as the
	/// supported file extensions.
	/// </summary>
	public sealed class FormatDescriptor
	{
		/// <summary>
		/// A list of the supported extensions (for example, "jpeg" and "JPEG").
		/// </summary>
		public string[] Extensions { get; private set; }

		/// <summary>
		/// A list of supported MIME types (for example, "image/jpg" and "image/png").
		/// </summary>
		public string[] Mimes { get; private set; }

		/// <summary>
		/// The importer for this file format. This may be null if only exporting
		/// is supported for this format.
		/// </summary>
		public IImageImporter? Importer { get; private set; }

		/// <summary>
		/// The exporter for this file format. This may be null if only importing
		/// is supported for this format.
		/// </summary>
		public IImageExporter? Exporter { get; private set; }

		/// <summary>
		/// A file filter for use in the file dialog.
		/// </summary>
		public FileFilter Filter { get; private set; }

		/// <param name="displayPrefix">
		/// A descriptive name for the format, such as "OpenRaster". This will be displayed
		/// in the file dialog's filter.
		/// </param>
		/// <param name="extensions">A list of supported file extensions (for example, "jpeg" and "JPEG").</param>
		/// <param name="mimes">A list of supported file MIME types (for example, "image/jpeg" and "image/png").</param>
		/// <param name="importer">The importer for this file format, or null if importing is not supported.</param>
		/// <param name="exporter">The exporter for this file format, or null if exporting is not supported.</param>
		public FormatDescriptor (string displayPrefix, string[] extensions, string[] mimes,
					 IImageImporter? importer, IImageExporter? exporter)
		{
			if (extensions == null || (importer == null && exporter == null)) {
				throw new ArgumentNullException ("Format descriptor is initialized incorrectly");
			}

			this.Extensions = extensions;
			this.Mimes = mimes;
			this.Importer = importer;
			this.Exporter = exporter;

			FileFilter ff = FileFilter.New ();
			StringBuilder formatNames = new StringBuilder ();

			foreach (string ext in extensions) {
				if (formatNames.Length > 0)
					formatNames.Append (", ");

				string wildcard = $"*.{ext}";
				ff.AddPattern (wildcard);
				formatNames.Append (wildcard);
			}

			// On Unix-like systems, file extensions are often considered optional.
			// Files can often also be identified by their MIME types.
			// Windows does not understand MIME types natively.
			// Adding a MIME filter on Windows would break the native file picker and force a GTK file picker instead.
			if (SystemManager.GetOperatingSystem () != OS.Windows) {
				foreach (string mime in mimes) {
					ff.AddMimeType (mime);
				}
			}

			ff.Name = string.Format (Translations.GetString ("{0} image ({1})"), displayPrefix, formatNames);
			this.Filter = ff;
		}

		[MemberNotNullWhen (returnValue: false, member: nameof (Exporter))]
		public bool IsReadOnly ()
		{
			return Exporter == null;
		}

		[MemberNotNullWhen (returnValue: false, member: nameof (Importer))]
		public bool IsWriteOnly ()
		{
			return Importer == null;
		}
	}
}
