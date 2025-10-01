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
using Cairo;
using GdkPixbuf;

namespace Pinta.Core;

public class GdkPixbufFormat : IImageImporter, IImageExporter
{
	private readonly string filetype;
	private readonly bool supports_alpha;

	public GdkPixbufFormat (string filetype, bool supportsAlpha = true)
	{
		this.filetype = filetype;
		this.supports_alpha = supportsAlpha;
	}

	public Document Import (Gio.File file)
	{
		using Pixbuf streamBuffer = ReadPixbuf (file);
		using Pixbuf effectiveBuffer = streamBuffer.ApplyEmbeddedOrientation () ?? streamBuffer;

		Size imageSize = new (effectiveBuffer.Width, effectiveBuffer.Height);

		Document newDocument = new (
			PintaCore.Actions,
			PintaCore.Tools,
			PintaCore.Workspace,
			imageSize,
			file,
			filetype);

		Layer layer = newDocument.Layers.AddNewLayer (file.GetDisplayName ());

		using Context g = new (layer.Surface);

		g.DrawPixbuf (effectiveBuffer, PointD.Zero);

		return newDocument;
	}

	private static Pixbuf ReadPixbuf (Gio.File file)
	{
		// Handle any EXIF orientation flags
		using Gio.FileInputStream fs = file.Read (cancellable: null);
		try {
			return Pixbuf.NewFromStream (fs, cancellable: null)!; // NRT: only nullable when an error is thrown
		} finally {
			fs.Close (null);
		}
	}

	protected virtual void DoSave (
		Pixbuf pb,
		Gio.File file,
		string fileType,
		Gtk.Window parent)
	{
		using Gio.OutputStream stream = file.Replace ();
		try {
			pb.SaveToStreamv (stream, fileType,
					optionKeys: [],
					optionValues: [],
					cancellable: null);
		} finally {
			stream.Close (null);
		}
	}

	public void Export (
		Document document,
		Gio.File file,
		Gtk.Window parent)
	{
		using ImageSurface flattenedImage = document.GetFlattenedImage ();
		// Note that some pixbuf formats will throw an error when saving an RGBA pixbuf
		// if the image format doesn't actually store alpha
		// (e.g. glycin does this for JPEG - bug #1774)
		using Pixbuf pb = flattenedImage.ToPixbuf (includeAlpha: supports_alpha);
		DoSave (pb, file, filetype, parent);
	}
}
