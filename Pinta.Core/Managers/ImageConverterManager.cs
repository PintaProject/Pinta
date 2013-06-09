// 
// ImageConverterManager.cs
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
using System.Linq;
using Gdk;
using System.IO;

namespace Pinta.Core
{
	public class ImageConverterManager
	{
		private List<FormatDescriptor> formats;

		public ImageConverterManager ()
		{
			formats = new List<FormatDescriptor> ();

			// Create all the formats supported by Gdk
			foreach (var format in Pixbuf.Formats) {
				string formatName = format.Name.ToLowerInvariant ();
				string formatNameUpperCase = formatName.ToUpperInvariant ();
				string[] extensions;

				switch (formatName) {
					case "jpeg":
						extensions = new string[] { "jpg", "jpeg", "JPG", "JPEG" };
						break;
					case "tiff":
						extensions = new string[] { "tif", "tiff", "TIF", "TIFF" };
						break;
					default:
						extensions = new string[] { formatName, formatNameUpperCase };
						break;
				}
				
				GdkPixbufFormat importer = new GdkPixbufFormat (format.Name.ToLowerInvariant ());
				IImageExporter exporter;

				if (formatName == "jpeg")
					exporter = importer = new JpegFormat ();
				else if (formatName == "tga")
					exporter = new TgaExporter ();
				else if (format.IsWritable)
					exporter = importer;
				else
					exporter = null;

				RegisterFormat (new FormatDescriptor (formatNameUpperCase, extensions, importer, exporter));
			}

			// Create all the formats we have our own importers/exporters for
			OraFormat oraHandler = new OraFormat ();
			RegisterFormat (new FormatDescriptor ("OpenRaster", new string[] { "ora", "ORA" }, oraHandler, oraHandler));
		}

		public IEnumerable<FormatDescriptor> Formats { get { return formats; } }

		/// <summary>
		/// Registers a new file format.
		/// </summary>
		public void RegisterFormat (FormatDescriptor fd)
		{
			formats.Add (fd);
		}

		/// <summary>
		/// Unregisters the file format for the given extension.
		/// </summary>
		public void UnregisterFormatByExtension (string extension)
		{
			extension = NormalizeExtension (extension);

			formats.RemoveAll (f => f.Extensions.Contains (extension));			
		}

		/// <summary>
		/// Returns the default format that should be used when saving a file.
		/// This is normally the last format that was chosen by the user.
		/// </summary>
		public FormatDescriptor GetDefaultSaveFormat ()
		{
			string extension = PintaCore.Settings.GetSetting<string> ("default-image-type", "jpeg");

			var fd = GetFormatByExtension (extension);

			// We found the last one we used
			if (fd != null)
				return fd;

			// Return any format we have
			foreach (var f in Formats)
				if (!f.IsReadOnly ())
					return f;

			// We don't have any formats
			throw new InvalidOperationException ("There are no image formats supported.");
		}

		/// <summary>
		/// Finds the correct exporter to use for opening the given file, or null
		/// if no exporter exists for the file.
		/// </summary>
		public IImageExporter GetExporterByFile (string file)
		{
			string extension = Path.GetExtension (file);
			return GetExporterByExtension (extension);
		}

		/// <summary>
		/// Finds the file format for the given file name, or null
		/// if no file format exists for that file.
		/// </summary>
		public FormatDescriptor GetFormatByFile (string file)
		{
			string extension = Path.GetExtension (file);

			extension = NormalizeExtension (extension);

			return Formats.Where (p => p.Extensions.Contains (extension)).FirstOrDefault ();
		}

		/// <summary>
		/// Finds the correct importer to use for opening the given file, or null
		/// if no importer exists for the file.
		/// </summary>
		public IImageImporter GetImporterByFile (string file)
		{
			string extension = Path.GetExtension (file);

			if (extension == null) {
				return null;
			} else {
				return GetImporterByExtension (extension);
			}
		}

		/// <summary>
		/// Sets the default format used when saving files to the given extension.
		/// </summary>
		public void SetDefaultFormat (string extension)
		{
			extension = NormalizeExtension (extension);

			PintaCore.Settings.PutSetting ("default-image-type", extension);
			PintaCore.Settings.SaveSettings ();
		}

		/// <summary>
		/// Finds the correct exporter to use for the given file extension, or null
		/// if no exporter exists for that extension.
		/// </summary>
		private IImageExporter GetExporterByExtension (string extension)
		{
			FormatDescriptor format = GetFormatByExtension (extension);

			if (format == null)
				return null;

			return format.Exporter;
		}

		/// <summary>
		/// Finds the correct importer to use for the given file extension, or null
		/// if no importer exists for that extension.
		/// </summary>
		private IImageImporter GetImporterByExtension (string extension)
		{
			FormatDescriptor format = GetFormatByExtension (extension);

			if (format == null)
				return null;

			return format.Importer;
		}

		/// <summary>
		/// Finds the file format for the given file extension, or null
		/// if no file format exists for that extension.
		/// </summary>
		private FormatDescriptor GetFormatByExtension (string extension)
		{
			extension = NormalizeExtension (extension);

			return Formats.Where (p => p.Extensions.Contains (extension)).FirstOrDefault ();
		}

		/// <summary>
		/// Normalizes the extension.
		/// </summary>
		private static string NormalizeExtension (string extension)
		{
			return extension.ToLowerInvariant ().TrimStart ('.').Trim ();
		}
	}
}
