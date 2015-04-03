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
using System.Xml;

using Gtk;
using Gdk;
using Cairo;

using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Generic;

namespace Pinta.Core
{
	public class OraFormat: IImageImporter, IImageExporter
	{
		private const int ThumbMaxSize = 256;

		#region IImageImporter implementation
		
		public void Import (string fileName, Gtk.Window parent) {
			ZipFile file = new ZipFile (fileName);
			XmlDocument stackXml = new XmlDocument ();
			stackXml.Load (file.GetInputStream (file.GetEntry ("stack.xml")));
			
			XmlElement imageElement = stackXml.DocumentElement;
			int width = int.Parse (imageElement.GetAttribute ("w"));
			int height = int.Parse (imageElement.GetAttribute ("h"));

			Size imagesize = new Size (width, height);

			Document doc = PintaCore.Workspace.CreateAndActivateDocument (fileName, imagesize);
			doc.HasFile = true;
			
			XmlElement stackElement = (XmlElement) stackXml.GetElementsByTagName ("stack")[0];
			XmlNodeList layerElements = stackElement.GetElementsByTagName ("layer");
			
			if (layerElements.Count == 0)
				throw new XmlException ("No layers found in OpenRaster file");

			doc.ImageSize = imagesize;
			doc.Workspace.CanvasSize = imagesize;

			for (int i = 0; i < layerElements.Count; i++) {
				XmlElement layerElement = (XmlElement) layerElements[i];
				int x = int.Parse (GetAttribute (layerElement, "x", "0"));
				int y = int.Parse (GetAttribute (layerElement, "y", "0"));
				string name = GetAttribute (layerElement, "name", string.Format ("Layer {0}", i));
				
				try {
					// Write the file to a temporary file first
					// Fixes a bug when running on .Net
					ZipEntry zf = file.GetEntry (layerElement.GetAttribute ("src"));
					Stream s = file.GetInputStream (zf);
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

					UserLayer layer = doc.CreateLayer(name);
					doc.Insert (layer, 0);

					layer.Opacity = double.Parse (GetAttribute (layerElement, "opacity", "1"), GetFormat ());
					layer.BlendMode = StandardToBlendMode (GetAttribute (layerElement, "composite-op", "svg:src-over"));

					using (var fs = new FileStream (tmp_file, FileMode.Open))
						using (Pixbuf pb = new Pixbuf (fs)) {
							using (Context g = new Context (layer.Surface)) {
								CairoHelper.SetSourcePixbuf (g, pb, x, y);
								g.Paint ();
							}
						}

					try {
						File.Delete (tmp_file);
					} catch { }
				} catch {
					MessageDialog md = new MessageDialog (parent, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Could not import layer \"{0}\" from {0}", name, file);
					md.Title = "Error";
				
					md.Run ();
					md.Destroy ();
				}
			}
			
			file.Close ();
		}

		public Pixbuf LoadThumbnail (string filename, int maxWidth, int maxHeight, Gtk.Window parent)
		{
			ZipFile file = new ZipFile (filename);
			ZipEntry ze = file.GetEntry ("Thumbnails/thumbnail.png");

			// The ORA specification requires that all files will have a
			// thumbnail that is less than 256x256 pixels, so don't bother
			// with scaling the preview.
			Pixbuf p = new Pixbuf (file.GetInputStream (ze));
			file.Close ();
			return p;
		}

		#endregion
		
		private static IFormatProvider GetFormat () {
			return System.Globalization.CultureInfo.CreateSpecificCulture ("en");
		}

		private static string GetAttribute (XmlElement element, string attribute, string defValue) {
			string ret = element.GetAttribute (attribute);
			return string.IsNullOrEmpty (ret) ? defValue : ret;
		}

		private Size GetThumbDimensions (int width, int height) {
			if (width <= ThumbMaxSize && height <= ThumbMaxSize)
				return new Size (width, height);

			if (width > height)
				return new Size (ThumbMaxSize, (int) ((double)height / width * ThumbMaxSize));
			else
				return new Size ((int) ((double)width / height * ThumbMaxSize), ThumbMaxSize);
		}

		private byte[] GetLayerXmlData(List<UserLayer> layers)
		{
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
				writer.WriteAttributeString ("opacity", layers[i].Hidden ? "0" : string.Format (GetFormat (), "{0:0.00}", layers[i].Opacity));
				writer.WriteAttributeString ("name", layers[i].Name);
				writer.WriteAttributeString ("composite-op", BlendModeToStandard (layers[i].BlendMode));
				writer.WriteAttributeString ("src", "data/layer" + i.ToString () + ".png");
				writer.WriteEndElement ();
			}

			writer.WriteEndElement (); // stack
			writer.WriteEndElement (); // image

			writer.Close ();
			return ms.ToArray ();
		}

		public void Export (Document document, string fileName, Gtk.Window parent)
		{
			ZipOutputStream stream = new ZipOutputStream (new FileStream (fileName, FileMode.Create));
			ZipEntry mimetype = new ZipEntry ("mimetype");
			mimetype.CompressionMethod = CompressionMethod.Stored;
			stream.PutNextEntry (mimetype);

			byte[] databytes = System.Text.Encoding.ASCII.GetBytes ("image/openraster");
			stream.Write (databytes, 0, databytes.Length);

			for (int i = 0; i < document.UserLayers.Count; i++) {
				Pixbuf pb = document.UserLayers[i].Surface.ToPixbuf ();
				byte[] buf = pb.SaveToBuffer ("png");
				(pb as IDisposable).Dispose ();

				stream.PutNextEntry (new ZipEntry ("data/layer" + i.ToString () + ".png"));
				stream.Write (buf, 0, buf.Length);
			}

			stream.PutNextEntry (new ZipEntry ("stack.xml"));
			databytes = GetLayerXmlData (document.UserLayers);
			stream.Write (databytes, 0, databytes.Length);

			ImageSurface flattened = document.GetFlattenedImage ();
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

		private string BlendModeToStandard (BlendMode mode)
		{
			switch (mode) {
				case BlendMode.Normal:
				default:
					return "svg:src-over";
				case BlendMode.Multiply:
					return "svg:multiply";
				case BlendMode.ColorBurn:
					return "svg:color-burn";
				case BlendMode.ColorDodge:
					return "svg:color-dodge";
				case BlendMode.Overlay:
					return "svg:overlay";
				case BlendMode.Difference:
					return "svg:difference";
				case BlendMode.Lighten:
					return "svg:lighten";
				case BlendMode.Darken:
					return "svg:darken";
				case BlendMode.Screen:
					return "svg:screen";
				case BlendMode.Xor:
					return "svg:xor";
                case BlendMode.HardLight:
                    return "svg:hard-light";
                case BlendMode.SoftLight:
                    return "svg:soft-light";
                case BlendMode.Color:
                    return "svg:color";
                case BlendMode.Luminosity:
                    return "svg:luminosity";
                case BlendMode.Hue:
                    return "svg:hue";
                case BlendMode.Saturation:
                    return "svg:saturation";
			}
		}

		private BlendMode StandardToBlendMode (string mode)
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
}
