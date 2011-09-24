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
		public ImageConverterManager ()
		{
			Formats = new List<FormatDescriptor> ();

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

				Formats.Add (new FormatDescriptor (formatName, formatNameUpperCase, extensions, importer, exporter));
			}

			// Create all the formats we have our own importers/exporters for
			OraFormat oraHandler = new OraFormat ();
			Formats.Add (new FormatDescriptor ("ora", "OpenRaster", new string[] { "ora" }, oraHandler, oraHandler));
		}

		public IList<FormatDescriptor> Formats { get; private set; }

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

		public IImageExporter GetExporterByExtension (string extension)
		{
			FormatDescriptor format = GetFormatByExtension (extension);

			if (format == null)
				return null;

			return format.Exporter;
		}

		public IImageExporter GetExporterByFile (string file)
		{
			string extension = Path.GetExtension (file);
			return GetExporterByExtension (extension);
		}

		public FormatDescriptor GetFormatByExtension (string extension)
		{
			// Normalize the extension
			extension = extension.ToLowerInvariant ().TrimStart ('.').Trim ();

			return Formats.Where (p => p.Extensions.Contains (extension)).FirstOrDefault ();
		}

		public FormatDescriptor GetFormatByFile (string file)
		{
			string extension = Path.GetExtension (file);

			// Normalize the extension
			extension = extension.ToLowerInvariant ().TrimStart ('.').Trim ();

			return Formats.Where (p => p.Extensions.Contains (extension)).FirstOrDefault ();
		}

		public IImageImporter GetImporterByExtension (string extension)
		{
			FormatDescriptor format = GetFormatByExtension (extension);

			if (format == null)
				return null;

			return format.Importer;
		}

		public IImageImporter GetImporterByFile (string file)
		{
			string extension = Path.GetExtension (file);
			return GetImporterByExtension (extension);
		}

		public void SetDefaultFormat (string extension)
		{
			// Normalize the extension
			extension = extension.ToLowerInvariant ().TrimStart ('.').Trim ();

			PintaCore.Settings.PutSetting ("default-image-type", extension);
			PintaCore.Settings.SaveSettings ();
		}
	}
}
