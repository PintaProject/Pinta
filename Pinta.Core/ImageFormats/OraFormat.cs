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
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using Gdk;
using Cairo;
using System.Xml;
using System.Globalization;

namespace Pinta.Core
{
	public class OraFormat: IImageImporter, IImageExporter
	{
		private const int ThumbMaxSize = 256;

		public void Import (LayerManager layers, string fileName) {
			// TODO: Implement ORA reading
			throw new NotImplementedException ();
		}

		private Size GetThumbDimensions (int width, int height) {
			if (width <= ThumbMaxSize && height <= ThumbMaxSize)
				return new Size (width, height);

			if (width > height)
				return new Size (ThumbMaxSize, (int) ((double)height / width * ThumbMaxSize));
			else
				return new Size ((int) ((double)width / height * ThumbMaxSize), ThumbMaxSize);
		}

		private byte[] GetLayerXmlData (LayerManager layers) {
			CultureInfo culture = CultureInfo.CreateSpecificCulture ("en");

			MemoryStream ms = new MemoryStream ();
			XmlTextWriter writer = new XmlTextWriter (ms, System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;

			writer.WriteStartElement ("image");
			writer.WriteAttributeString ("w", layers[0].Surface.Width.ToString ());
			writer.WriteAttributeString ("h", layers[0].Surface.Height.ToString ());

			writer.WriteStartElement ("stack");
			writer.WriteAttributeString ("opacity", "1");
			writer.WriteAttributeString ("name", "root");

			// ORA stores layers top to bottom
			for (int i = layers.Count - 1; i >= 0; i--) {
				writer.WriteStartElement ("layer");
				writer.WriteAttributeString ("opacity", layers[i].Hidden ? "0" : string.Format (culture, "{0:0.00}", layers[i].Opacity));
				writer.WriteAttributeString ("name", layers[i].Name);
				writer.WriteAttributeString ("src", "data/layer" + i.ToString () + ".png");
				writer.WriteEndElement ();
			}

			writer.WriteEndElement (); // stack
			writer.WriteEndElement (); // image

			writer.Close ();
			return ms.ToArray ();
		}

		public void Export (LayerManager layers, string fileName) {
			ZipOutputStream stream = new ZipOutputStream (new FileStream (fileName, FileMode.Create));
			ZipEntry mimetype = new ZipEntry ("mimetype");
			mimetype.CompressionMethod = CompressionMethod.Stored;
			stream.PutNextEntry (mimetype);

			byte[] databytes = System.Text.Encoding.ASCII.GetBytes ("image/openraster");
			stream.Write (databytes, 0, databytes.Length);

			for (int i = 0; i < layers.Count; i++) {
				Pixbuf pb = layers[i].Surface.ToPixbuf ();
				byte[] buf = pb.SaveToBuffer ("png");
				(pb as IDisposable).Dispose ();

				stream.PutNextEntry (new ZipEntry ("data/layer" + i.ToString () + ".png"));
				stream.Write (buf, 0, buf.Length);
			}

			stream.PutNextEntry (new ZipEntry ("stack.xml"));
			databytes = GetLayerXmlData (layers);
			stream.Write (databytes, 0, databytes.Length);

			ImageSurface flattened = layers.GetFlattenedImage();
			Pixbuf flattenedPb = flattened.ToPixbuf ();
			Size newSize = GetThumbDimensions (flattenedPb.Width, flattenedPb.Height);
			Pixbuf thumb = flattenedPb.ScaleSimple (newSize.Width, newSize.Height, InterpType.Bilinear);

			stream.PutNextEntry (new ZipEntry ("Thumbnails/thumbnail.png"));
			databytes = thumb.SaveToBuffer ("png");
			stream.Write (databytes, 0, databytes.Length);

			(flattened as IDisposable).Dispose();
			(flattenedPb as IDisposable).Dispose();
			(thumb as IDisposable).Dispose();

			stream.Close ();
		}
	}
}
