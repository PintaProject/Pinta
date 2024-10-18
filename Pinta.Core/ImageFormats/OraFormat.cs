//
// OraFormat.cs
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
using System.IO;
using System.IO.Compression;
using System.Xml;
using Cairo;

namespace Pinta.Core;

public sealed class OraFormat : IImageImporter, IImageExporter
{
	private const int ThumbMaxSize = 256;

	#region IImageImporter implementation

	public Document Import (Gio.File file, Gtk.Window parent)
	{
		using var stream = new GioStream (file.Read (cancellable: null));
		using var zipfile = new ZipArchive (stream);

		XmlDocument stackXml = new ();

		ZipArchiveEntry stackXmlEntry = zipfile.GetEntry ("stack.xml") ?? throw new XmlException ("No 'stack.xml' found in OpenRaster file");
		stackXml.Load (stackXmlEntry.Open ());

		// NRT - This makes a lot of assumptions that the file will be perfectly
		// valid that we need to guard against.
		XmlElement imageElement = stackXml.DocumentElement!;

		Size imageSize = new (
			Width: int.Parse (imageElement.GetAttribute ("w")),
			Height: int.Parse (imageElement.GetAttribute ("h"))
		);

		Document newDocument = new (imageSize, file, "ora");
		newDocument.Workspace.ViewSize = imageSize;

		XmlElement stackElement = (XmlElement) stackXml.GetElementsByTagName ("stack")[0]!;
		XmlNodeList layerElements = stackElement.GetElementsByTagName ("layer");

		if (layerElements.Count == 0)
			throw new XmlException ("No layers found in OpenRaster file");

		for (int i = 0; i < layerElements.Count; i++) {

			XmlElement layerElement = (XmlElement) layerElements[i]!;

			PointI position = new (
				X: int.Parse (GetAttribute (layerElement, "x", "0")),
				Y: int.Parse (GetAttribute (layerElement, "y", "0"))
			);

			string name = GetAttribute (layerElement, "name", $"Layer {i}");

			try {
				// Write the file to a temporary file first
				// Fixes a bug when running on .Net
				ZipArchiveEntry? zf = zipfile.GetEntry (layerElement.GetAttribute ("src")) ?? throw new XmlException ("Missing layer in OpenRaster file");
				using Stream s = zf.Open ();
				string tmp_file = System.IO.Path.GetTempFileName ();

				using (Stream stream_out = File.Open (tmp_file, FileMode.OpenOrCreate)) {
					byte[] buffer = new byte[2048];

					while (true) {
						int len = s.Read (buffer, 0, buffer.Length);

						if (len > 0)
							stream_out.Write (buffer, 0, len);
						else
							break;
					}
				}

				UserLayer layer = newDocument.Layers.CreateLayer (name);
				newDocument.Layers.Insert (layer, 0);

				string visibility = GetAttribute (layerElement, "visibility", "visible");
				if (visibility == "hidden") {
					layer.Hidden = true;
				}

				layer.Opacity = double.Parse (GetAttribute (layerElement, "opacity", "1"), GetFormat ());
				layer.BlendMode = StandardToBlendMode (GetAttribute (layerElement, "composite-op", "svg:src-over"));

				var pb = GdkPixbuf.Pixbuf.NewFromFile (tmp_file)!; // NRT: only nullable when an error is thrown
				Context g = new (layer.Surface);
				g.DrawPixbuf (pb, (PointD) position);

				try {
					File.Delete (tmp_file);
				} catch { }
			} catch {
				// Translators: {0} is the name of a layer, and {1} is the path to a .ora file.
				string details = Translations.GetString ("Could not import layer \"{0}\" from {1}", name, zipfile);
				PintaCore.Chrome.ShowMessageDialog (PintaCore.Chrome.MainWindow, Translations.GetString ("Error"), details);
			}
		}

		return newDocument;
	}
	#endregion

	private static IFormatProvider GetFormat ()
		=> System.Globalization.CultureInfo.CreateSpecificCulture ("en");

	private static string GetAttribute (XmlElement element, string attribute, string defValue)
	{
		string ret = element.GetAttribute (attribute);
		return string.IsNullOrEmpty (ret) ? defValue : ret;
	}

	private static Size GetThumbDimensions (int width, int height)
	{
		if (width <= ThumbMaxSize && height <= ThumbMaxSize)
			return new (width, height);
		else if (width > height)
			return new (ThumbMaxSize, (int) ((double) height / width * ThumbMaxSize));
		else
			return new ((int) ((double) width / height * ThumbMaxSize), ThumbMaxSize);
	}

	private static byte[] GetLayerXmlData (IReadOnlyList<UserLayer> layers)
	{
		using MemoryStream ms = new MemoryStream ();
		using XmlTextWriter writer = new XmlTextWriter (ms, System.Text.Encoding.UTF8) {
			Formatting = Formatting.Indented,
		};

		writer.WriteStartElement ("image");
		writer.WriteAttributeString ("w", layers[0].Surface.Width.ToString ());
		writer.WriteAttributeString ("h", layers[0].Surface.Height.ToString ());
		writer.WriteAttributeString ("version", "0.0.5"); // Current version of the spec.

		writer.WriteStartElement ("stack");

		// ORA stores layers top to bottom
		for (int i = layers.Count - 1; i >= 0; i--) {
			var layer = layers[i];
			writer.WriteStartElement ("layer");
			writer.WriteAttributeString ("opacity", string.Format (GetFormat (), "{0:0.00}", layer.Opacity));
			writer.WriteAttributeString ("name", layer.Name);
			writer.WriteAttributeString ("composite-op", BlendModeToStandard (layer.BlendMode));
			writer.WriteAttributeString ("src", "data/layer" + i.ToString () + ".png");
			if (layer.Hidden) {
				writer.WriteAttributeString ("visibility", "hidden");
			}
			writer.WriteEndElement ();
		}

		writer.WriteEndElement (); // stack
		writer.WriteEndElement (); // image

		writer.Close ();
		return ms.ToArray ();
	}

	public void Export (Document document, Gio.File file, Gtk.Window parent)
	{
		using var file_stream = new GioStream (file.Replace ());

		using var archive = new ZipArchive (file_stream, ZipArchiveMode.Create);

		{
			var mimeBytes = System.Text.Encoding.ASCII.GetBytes ("image/openraster");
			ZipArchiveEntry entry = archive.CreateEntry ("mimetype", CompressionLevel.NoCompression);
			using var s = entry.Open ();
			s.Write (mimeBytes, 0, mimeBytes.Length);
		}

		for (int i = 0; i < document.Layers.UserLayers.Count; i++) {
			var pb = document.Layers.UserLayers[i].Surface.ToPixbuf ();
			byte[] buf = pb.SaveToBuffer ("png");

			ZipArchiveEntry entry = archive.CreateEntry ($"data/layer{i}.png");
			using var s = entry.Open ();
			s.Write (buf, 0, buf.Length);
		}

		{
			var userLayerBytes = GetLayerXmlData (document.Layers.UserLayers);
			ZipArchiveEntry entry = archive.CreateEntry ("stack.xml");
			using var s = entry.Open ();
			s.Write (userLayerBytes, 0, userLayerBytes.Length);
		}

		// Add merged image.
		var flattenedPb = document.GetFlattenedImage ().ToPixbuf ();
		{
			var mergedImageBytes = flattenedPb.SaveToBuffer ("png");
			ZipArchiveEntry entry = archive.CreateEntry ("mergedimage.png");
			using var s = entry.Open ();
			s.Write (mergedImageBytes, 0, mergedImageBytes.Length);
		}

		// Add thumbnail.
		{
			Size newSize = GetThumbDimensions (flattenedPb.Width, flattenedPb.Height);
			var thumb = flattenedPb.ScaleSimple (newSize.Width, newSize.Height, GdkPixbuf.InterpType.Bilinear)!;
			var thumbnailBytes = thumb.SaveToBuffer ("png");
			ZipArchiveEntry entry = archive.CreateEntry ("Thumbnails/thumbnail.png");
			using var s = entry.Open ();
			s.Write (thumbnailBytes, 0, thumbnailBytes.Length);
		}
	}

	private static string BlendModeToStandard (BlendMode mode)
		=> mode switch {
			BlendMode.Multiply => "svg:multiply",
			BlendMode.ColorBurn => "svg:color-burn",
			BlendMode.ColorDodge => "svg:color-dodge",
			BlendMode.Overlay => "svg:overlay",
			BlendMode.Difference => "svg:difference",
			BlendMode.Lighten => "svg:lighten",
			BlendMode.Darken => "svg:darken",
			BlendMode.Screen => "svg:screen",
			BlendMode.Xor => "svg:xor",
			BlendMode.HardLight => "svg:hard-light",
			BlendMode.SoftLight => "svg:soft-light",
			BlendMode.Color => "svg:color",
			BlendMode.Luminosity => "svg:luminosity",
			BlendMode.Hue => "svg:hue",
			BlendMode.Saturation => "svg:saturation",
			_ => "svg:src-over",
		};

	private static BlendMode StandardToBlendMode (string mode)
	{
		switch (mode) {
			case "svg:src-over":
				return BlendMode.Normal;
			case "svg:multiply":
				return BlendMode.Multiply;
			case "svg:color-burn":
				return BlendMode.ColorBurn;
			case "svg:color-dodge":
				return BlendMode.ColorDodge;
			case "svg:overlay":
				return BlendMode.Overlay;
			case "svg:difference":
				return BlendMode.Difference;
			case "svg:lighten":
				return BlendMode.Lighten;
			case "svg:darken":
				return BlendMode.Darken;
			case "svg:screen":
				return BlendMode.Screen;
			case "svg:xor":
				return BlendMode.Xor;
			case "svg:hard-light":
				return BlendMode.HardLight;
			case "svg:soft-light":
				return BlendMode.SoftLight;
			case "svg:color":
				return BlendMode.Color;
			case "svg:luminosity":
				return BlendMode.Luminosity;
			case "svg:hue":
				return BlendMode.Hue;
			case "svg:saturation":
				return BlendMode.Saturation;
			default:
				Console.WriteLine ("Unrecognized composite-op: {0}, using Normal.", mode);
				return BlendMode.Normal;
		}
	}
}
