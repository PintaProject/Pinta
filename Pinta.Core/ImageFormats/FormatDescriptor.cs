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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Pinta.Core;

/// <summary>
/// Describes information about a file format, such as the
/// supported file extensions.
/// </summary>
public sealed class FormatDescriptor
{
	/// <summary>
	/// A list of the supported extensions (for example, "jpeg" and "JPEG").
	/// </summary>
	public ImmutableArray<string> Extensions { get; }

	/// <summary>
	/// A list of supported MIME types (for example, "image/jpg" and "image/png").
	/// </summary>
	public ImmutableArray<string> Mimes { get; }

	/// <summary>
	/// The importer for this file format. This may be null if only exporting
	/// is supported for this format.
	/// </summary>
	public IImageImporter? Importer { get; }

	/// <summary>
	/// The exporter for this file format. This may be null if only importing
	/// is supported for this format.
	/// </summary>
	public IImageExporter? Exporter { get; }

	/// <summary>
	/// A file filter for use in the file dialog.
	/// </summary>
	public Gtk.FileFilter Filter { get; }

	/// <summary>
	/// Whether the format supports layers.
	/// </summary>
	public bool SupportsLayers { get; }

	/// <param name="displayPrefix">
	/// A descriptive name for the format, such as "OpenRaster". This will be displayed
	/// in the file dialog's filter.
	/// </param>
	/// <param name="extensions">A list of supported file extensions (for example, "jpeg" and "JPEG").</param>
	/// <param name="mimes">A list of supported file MIME types (for example, "image/jpeg" and "image/png").</param>
	/// <param name="importer">The importer for this file format, or null if importing is not supported.</param>
	/// <param name="exporter">The exporter for this file format, or null if exporting is not supported.</param>
	/// <param name="supportsLayers">Whether the format supports layers.</param>
	public FormatDescriptor (
		string displayPrefix,
		IEnumerable<string> extensions,
		IEnumerable<string> mimes,
		IImageImporter? importer,
		IImageExporter? exporter,
		bool supportsLayers = false)
	{
		if (importer == null && exporter == null)
			throw new ArgumentException ("Format descriptor is initialized incorrectly", $"{nameof (importer)}, {nameof (exporter)}");

		Extensions = [.. extensions]; // Create a read-only copy
		Mimes = [.. mimes]; // Create a read-only copy
		Importer = importer;
		Exporter = exporter;
		SupportsLayers = supportsLayers;

		Gtk.FileFilter ff = Gtk.FileFilter.New ();
		StringBuilder formatNames = new ();

		foreach (string ext in Extensions) {

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
			foreach (string mime in Mimes) {
				ff.AddMimeType (mime);
			}
		}

		ff.Name = string.Format (Translations.GetString ("{0} image ({1})"), displayPrefix, formatNames);
		Filter = ff;
	}

	[MemberNotNullWhen (returnValue: true, member: nameof (Exporter))]
	public bool IsExportAvailable () => Exporter is not null;

	[MemberNotNullWhen (returnValue: true, member: nameof (Importer))]
	public bool IsImportAvailable () => Importer is not null;
}
