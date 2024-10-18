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
using GdkPixbuf;

namespace Pinta.Core;

public class GdkPixbufFormat : IImageImporter, IImageExporter
{
	private readonly string filetype;

	public GdkPixbufFormat (string filetype)
	{
		this.filetype = filetype;
	}

	#region IImageImporter implementation

	public void Import (Gio.File file, Gtk.Window parent)
	{
		Pixbuf bg;

		// Handle any EXIF orientation flags
		using (var fs = file.Read (cancellable: null)) {
			try {
				bg = Pixbuf.NewFromStream (fs, cancellable: null)!; // NRT: only nullable when an error is thrown
			} finally {
				fs.Close (null);
			}
		}

		bg = bg.ApplyEmbeddedOrientation () ?? bg;

		Size imageSize = new (bg.Width, bg.Height);

		// TODO: Move "attach document" part out of file format class.
		//       The creation of the document should be separate from
		//       its activation.
		Document newDocument = new (imageSize, file, filetype);
		newDocument.Workspace.ViewSize = imageSize;
		PintaCore.Workspace.AttachDocument (newDocument, PintaCore.Actions);

		Layer layer = newDocument.Layers.AddNewLayer (file.GetDisplayName ());

		Cairo.Context g = new (layer.Surface);
		g.DrawPixbuf (bg, PointD.Zero);
	}
	#endregion

	protected virtual void DoSave (Pixbuf pb, Gio.File file, string fileType, Gtk.Window parent)
	{
		using var stream = file.Replace ();
		try {
			pb.SaveToStreamv (stream, fileType,
					optionKeys: Array.Empty<string> (),
					optionValues: Array.Empty<string> (),
					cancellable: null);
		} finally {
			stream.Close (null);
		}
	}

	public void Export (Document document, Gio.File file, Gtk.Window parent)
	{
		var surf = document.GetFlattenedImage ();

		using Pixbuf pb = surf.ToPixbuf ();
		DoSave (pb, file, filetype, parent);
	}
}
