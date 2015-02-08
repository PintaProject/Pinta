// 
// OraFormat.cs
//
// Author:
//       Maia Kozheva <sikon@ubuntu.com>
// 
// Copyright (c) 2010 Maia Kozheva <sikon@ubuntu.com>
// Copyright (C) 2014 Alan Horkan <horkana@maths.tcd.ie>
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

// OpenRaster (.ora)
// OpenRaster is an open exchange format for layered raster based graphics documents.
// http://freedesktop.org/wiki/Specifications/OpenRaster/
// OpenDocument Format including Draw (.odg)
// http://www.oasis-open.org/committees/office/

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
        
        private const string oraMimeType = "image/openraster";
        private const string odgMimeType = "application/vnd.oasis.opendocument.graphics";
        public string MimeType = oraMimeType;

        // xml namespaces   
        private string nsmeta = "urn:oasis:names:tc:opendocument:xmlns:meta:1.0";
        private string nsoffice = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
        private string nsdc = "http://purl.org/dc/elements/1.1/";
        
        public void Import (string fileName, Gtk.Window parent)
        {
            ZipFile zfile = new ZipFile (fileName);       
            
            // warn if mimetype incorrect
            try {
                StreamReader reader = new StreamReader ((zfile.GetInputStream (zfile.GetEntry ("mimetype"))));
                string line = reader.ReadLine();
                    if (line != MimeType) 
                    {
                        MessageDialog md = new MessageDialog (PintaCore.Chrome.MainWindow, DialogFlags.Modal, MessageType.Warning, ButtonsType.Ok, "Unexpected mimetype in {0}", fileName);
                        md.Title = "Warning";
                   
                        md.Run ();
                        md.Destroy ();                        
                    }

            } catch { }   

            // must have stack.xml to be a valid OpenRaster
            XmlDocument stackXml = new XmlDocument ();            
            stackXml.Load (zfile.GetInputStream (zfile.GetEntry ("stack.xml")));
            
            XmlElement imageElement = stackXml.DocumentElement;
            int width = int.Parse (imageElement.GetAttribute ("w"));
            int height = int.Parse (imageElement.GetAttribute ("h"));
            int c = 0; // store layer index
            Size imagesize = new Size (width, height);

            Document doc = PintaCore.Workspace.CreateAndActivateDocument (fileName, imagesize);
            doc.HasFile = true;
            
            XmlElement stackElement = (XmlElement) stackXml.GetElementsByTagName ("stack")[0];
            XmlNodeList layerElements = stackElement.GetElementsByTagName ("layer");
            
            if (layerElements.Count == 0)
                throw new XmlException ("No layers found in OpenRaster file");

            doc.ImageSize = imagesize;
            doc.Workspace.CanvasSize = imagesize;

            for (int i = 0; i < layerElements.Count; i++)
            {
                XmlElement layerElement = (XmlElement) layerElements[i];
                int x = int.Parse (GetAttribute (layerElement, "x", "0"));
                int y = int.Parse (GetAttribute (layerElement, "y", "0"));
                string name = GetAttribute (layerElement, "name", string.Format ("Layer {0}", i));
                
                try
                {
                    // Write the file to a temporary file first
                    // Fixes exception on .Net when image too big. bug #594677
                    ZipEntry zf = zfile.GetEntry (layerElement.GetAttribute ("src"));
                    Stream s = zfile.GetInputStream (zf);
                    string tmp_file = System.IO.Path.GetTempFileName ();
                    
                    using (Stream stream_out = File.Open (tmp_file, FileMode.OpenOrCreate))
                    {
                        byte[] buffer = new byte[2048];
                        
                        while (true)
                        {
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
                    if ( GetAttribute (layerElement, "visibility", "1") == "hidden")
                    {
                        layer.Hidden = true;
                    }
                    layer.BlendMode = StandardToBlendMode (GetAttribute (layerElement, "composite-op", "svg:src-over"));
                    // Note: it is possible that more than one layer may be selected
                    // programs may decide what to do in that case
                    if ( GetAttribute (layerElement, "selected", "1") == "true")
                    {
                        c = (layerElements.Count -1) -i;
                        // out of range error if you try to set now
                        // must wait until later
                        // doc.SetCurrentUserLayer (c);
                    }
                    if ( GetAttribute (layerElement, "edit-locked", "1") == "true")
                    {
                        layer.Locked = true;
                    }
                    
                    using (var fs = new FileStream (tmp_file, FileMode.Open))
                        using (Pixbuf pb = new Pixbuf (fs))
                    {
                        using (Context g = new Context (layer.Surface))
                        {
                            CairoHelper.SetSourcePixbuf (g, pb, x, y);
                            g.Paint ();
                        }
                    }

                    try
                    {
                        File.Delete (tmp_file);
                    } catch { }
                } catch
                {
                    MessageDialog md = new MessageDialog (PintaCore.Chrome.MainWindow, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Could not import layer \"{0}\" from {0}", name, zfile);
                    md.Title = "Error";
                    
                    md.Run ();
                    md.Destroy ();
                }
            }

            XmlDocument metaXml = new XmlDocument ();
            // meta.xml is optional and might not exist
            try {
                metaXml.Load (zfile.GetInputStream (zfile.GetEntry ("meta.xml")));
                ReadMeta (metaXml);
            } catch { }        
            
            doc.SetCurrentUserLayer (c); // select a layer
            
            zfile.Close ();
        }

        public Pixbuf LoadThumbnail (string filename, int maxWidth, int maxHeight, Gtk.Window parent)
        {
            ZipFile zf = new ZipFile (filename);
            ZipEntry ze = zf.GetEntry ("Thumbnails/thumbnail.png");

            // The ORA specification requires that all files have a
            // thumbnail that is less than 256x256 pixels, so don't bother
            // with scaling the preview.
            Pixbuf p = new Pixbuf (zf.GetInputStream (ze));
            zf.Close ();
            return p;
        }

        private void ReadMimeType (string filename)
        {
            // read file and if mimetype != MimeType
        }        
        
        private void ReadStack ()
        {
            
        }
        
        private void ReadMeta (XmlDocument metaXml)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(metaXml.NameTable);
            nsmgr.AddNamespace ("dc", nsdc);
            nsmgr.AddNamespace ("meta", nsmeta);
            
            // author/creator 
            XmlNode authorElement = metaXml.SelectSingleNode ("//dc:creator", nsmgr);
            PintaCore.Workspace.ActiveDocument.Author = authorElement.InnerText;
            // title 
            XmlNode titleElement = metaXml.SelectSingleNode ("//dc:title", nsmgr);
            PintaCore.Workspace.ActiveDocument.Title = titleElement.InnerText;
            // subject 
            XmlNode subjectElement = metaXml.SelectSingleNode ("//dc:subject", nsmgr);
            PintaCore.Workspace.ActiveDocument.Subject = subjectElement.InnerText;
            // publisher
            // comment/description
            XmlNode commentElement = metaXml.SelectSingleNode ("//dc:description", nsmgr);
            PintaCore.Workspace.ActiveDocument.Comments = commentElement.InnerText;
            // keywords
            XmlNode keywordsElement = metaXml.SelectSingleNode ("//meta:keyword", nsmgr);
           	PintaCore.Workspace.ActiveDocument.Keywords = keywordsElement.InnerText;
            // user defined ...

        }
        
        #endregion
        
        private static IFormatProvider GetFormat ()
        {
            return System.Globalization.CultureInfo.CreateSpecificCulture ("en");
        }

        private static string GetAttribute (XmlElement element, string attribute, string defValue)
        {
            string ret = element.GetAttribute (attribute);
            return string.IsNullOrEmpty (ret) ? defValue : ret;
        }

        // Preview Thumbnail 256 pixels
        private Size GetThumbDimensions (int width, int height)
        {
            if (width <= ThumbMaxSize && height <= ThumbMaxSize)
                return new Size (width, height);

            if (width > height)
                return new Size (ThumbMaxSize, (int) ((double)height / width * ThumbMaxSize));
            else
                return new Size ((int) ((double)width / height * ThumbMaxSize), ThumbMaxSize);
        }

        // stack.xml required by OpenRaster
        private byte[] GetStackXmlData (List<UserLayer> layers)
        {
            MemoryStream stream = new MemoryStream ();
            XmlWriterSettings settings = new XmlWriterSettings ();
            settings.Indent = true;
            settings.OmitXmlDeclaration = false;
            settings.ConformanceLevel = ConformanceLevel.Document;
            settings.CloseOutput = false;
            XmlWriter writer = XmlWriter.Create (stream, settings);
            
            writer.WriteStartElement ("image");
            writer.WriteAttributeString ("version", "0.0.3"); // mandatory
            writer.WriteAttributeString ("w", layers[0].Surface.Width.ToString ());
            writer.WriteAttributeString ("h", layers[0].Surface.Height.ToString ());
//          writer.WriteAttributeString ("xres", "600"); // optional
//          writer.WriteAttributeString ("yres", "600"); 
            
            writer.WriteStartElement ("stack");
            writer.WriteAttributeString ("name", "root");
            // must be ommitted from root stack
//            writer.WriteAttributeString ("opacity", "1");
//            writer.WriteAttributeString ("visibility", "hidden");
            
            // ORA stores layers top to bottom
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                writer.WriteStartElement ("layer");
                writer.WriteAttributeString (
                    "opacity",
                    string.Format (GetFormat (), "{0:0.00}", layers[i].Opacity)
                );
                writer.WriteAttributeString ("name", layers[i].Name);
                writer.WriteAttributeString (
                    "composite-op",
                    BlendModeToStandard (layers[i].BlendMode)
                );
                writer.WriteAttributeString (
                    "src",
                    "data/layer" + i.ToString () + ".png"
                );
                // visible by default, only write tag if hidden
                if (layers[i].Hidden)
                {
                    writer.WriteAttributeString (
                        "visibility",
                        "hidden"
                    );
                }
                if (layers[i].Locked)
                {
                    writer.WriteAttributeString (
                        "edit-locked",
                        "true"
                    );
                }

                // HACK: ugly but it seems to work
                // mark the currently selected layer
                if (layers[i] == PintaCore.Workspace.ActiveDocument.CurrentUserLayer)
                {
                    writer.WriteAttributeString ("selected", "true");
                }

                writer.WriteEndElement ();
            }

            writer.WriteEndElement (); // stack
            writer.WriteEndElement (); // image

            writer.Close ();
            return stream.ToArray ();
        }

        public void Export (Document document, string fileName, Gtk.Window parent)
        {
            ZipOutputStream stream = new ZipOutputStream (new FileStream (fileName, FileMode.Create));
            ZipEntry mimetype = new ZipEntry ("mimetype");
            mimetype.CompressionMethod = CompressionMethod.Stored;
            stream.PutNextEntry (mimetype);

            byte[] databytes = System.Text.Encoding.ASCII.GetBytes (oraMimeType);
            stream.Write (databytes, 0, databytes.Length);

            for (int i = 0; i < document.UserLayers.Count; i++)
            {
                Pixbuf pb = document.UserLayers[i].Surface.ToPixbuf ();
                byte[] buf = pb.SaveToBuffer ("png");
                (pb as IDisposable).Dispose ();

                stream.PutNextEntry (new ZipEntry ("data/layer" + i.ToString () + ".png"));
                stream.Write (buf, 0, buf.Length);
            }

            // OpenDocument MUST include manifest and content
            stream.PutNextEntry (new ZipEntry ("META-INF/manifest.xml"));
            databytes = GetManifestXmlData (document.UserLayers);
            stream.Write (databytes, 0, databytes.Length);
            stream.PutNextEntry (new ZipEntry ("content.xml"));
            databytes = GetContentXmlData (document.UserLayers);
            stream.Write (databytes, 0, databytes.Length);

            /*            // OpenDocument MAY include meta, settings, styles
            // optional in theory, needed in practice to avoid unwanted errors/warnings
            stream.PutNextEntry (new ZipEntry ("styles.xml"));
            databytes = GetStylesXmlData ();
            stream.Write (databytes, 0, databytes.Length);
            
            stream.PutNextEntry (new ZipEntry ("settings.xml"));
            databytes = GetSettingsXmlData ();
            stream.Write (databytes, 0, databytes.Length);
             */
            stream.PutNextEntry (new ZipEntry ("meta.xml"));
            databytes = GetMetaXmlData (document);
            stream.Write (databytes, 0, databytes.Length);
            
            stream.PutNextEntry (new ZipEntry ("stack.xml"));
            databytes = GetStackXmlData (document.UserLayers);
            stream.Write (databytes, 0, databytes.Length);

            ImageSurface flattened = document.GetFlattenedImage ();
            Pixbuf flattenedPb = flattened.ToPixbuf ();
            // mergedimage.png preview image required from OpenRaster 0.2.0
            stream.PutNextEntry (new ZipEntry ("mergedimage.png"));
            databytes = flattenedPb.SaveToBuffer ("png");
            stream.Write (databytes, 0, databytes.Length);
            
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
                case BlendMode.Additive:
                    return "svg:plus";
                case BlendMode.ColorBurn:
                    return "svg:color-burn";
                case BlendMode.ColorDodge:
                    return "svg:color-dodge";
                case BlendMode.Reflect:
                    return "pinta-reflect";
                case BlendMode.Glow:
                    return "pinta-glow";
                case BlendMode.Overlay:
                    return "svg:overlay";
                case BlendMode.Difference:
                    return "svg:difference";
                case BlendMode.Negation:
                    return "pinta-negation";
                case BlendMode.Lighten:
                    return "svg:lighten";
                case BlendMode.Darken:
                    return "svg:darken";
                case BlendMode.Screen:
                    return "svg:screen";
                case BlendMode.Xor:
                    return "svg:xor";
            }
        }

        private BlendMode StandardToBlendMode (string mode)
        {
            switch (mode) {
                case "svg:src-over":
                    return BlendMode.Normal;
                case "svg:multiply":
                    return BlendMode.Multiply;
                case "svg:plus":
                    return BlendMode.Additive;
                case "svg:color-burn":
                    return BlendMode.ColorBurn;
                case "svg:color-dodge":
                    return BlendMode.ColorDodge;
                case "pinta-reflect":
                    return BlendMode.Reflect;
                case "pinta-glow":
                    return BlendMode.Glow;
                case "svg:overlay":
                    return BlendMode.Overlay;
                case "svg:difference":
                    return BlendMode.Difference;
                case "pinta-negation":
                    return BlendMode.Negation;
                case "svg:lighten":
                    return BlendMode.Lighten;
                case "svg:darken":
                    return BlendMode.Darken;
                case "svg:screen":
                    return BlendMode.Screen;
                case "svg:xor":
                    return BlendMode.Xor;
                default:
                    Console.WriteLine ("Unrecognized composite-op: {0}, using Normal.", mode);
                    return BlendMode.Normal;
            }
        }

        // HACK: naive conversion of pixels to cm,
        // strictly it also depends on display DPI
        // not sure why OpenOffice fails to understand pixels
        // TODO include the correct xres & yres in stack.xml image tag  
        private string ConvertPixels (double pixels)
        {
            // 1 pixel = 0.02645833333333 centimeter
            double factor = 0.02645833334;
            // round to 3 significant figures
            string cm = Math.Round(pixels * factor, 3).ToString();
            return cm;
        }

        // content.xml required by OpenDocument
        private byte[] GetContentXmlData (List<UserLayer> layers)
        {
            const string units = "cm";
            string layerW;
            string layerH;
            // xmlns:office
            const string nsoffice = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
            // xmlns:draw
            const string nsdraw = "urn:oasis:names:tc:opendocument:xmlns:drawing:1.0";
            // xmlns:xlink
            const string nsxlink = "http://www.w3.org/1999/xlink";
            // xmlns:svg
            const string nssvg ="urn:oasis:names:tc:opendocument:xmlns:svg-compatible:1.0" ;
            
            MemoryStream stream = new MemoryStream ();
            // be strict with output, and tidy too
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = false;
            settings.OmitXmlDeclaration = false;
            settings.ConformanceLevel = ConformanceLevel.Document;
            settings.CloseOutput = false;
            XmlWriter writer = XmlWriter.Create (stream, settings);
            
            writer.WriteStartElement ("office", "document-content", nsoffice);
            // declare the namespace that are going to be used 
            writer.WriteAttributeString ("xmlns", "office", null, nsoffice);
            writer.WriteAttributeString ("xmlns", "draw", null, nsdraw);
            writer.WriteAttributeString ("xmlns", "xlink", null, nsxlink);
            writer.WriteAttributeString ("xmlns", "svg", null, nssvg);
            writer.WriteStartElement ("body", nsoffice);
            writer.WriteStartElement ("drawing", nsoffice);

            writer.WriteStartElement ("page", nsdraw);
            writer.WriteAttributeString("draw", "name", nsdraw, "page1"); // optional
            writer.WriteAttributeString("draw", "master-page-name", nsdraw, "Default");
            
            /* 
             * Design note:
             * To keep the markup relatively simple, only one OpenDocument layer named "Layout"
             * is used, and multiple xlinked image objects are used instead.
             */
            // z-order from bottom to top
            for (int i = 0; i < layers.Count; i++)
            {
                // NOTE: deliberately checking every layer
                // layers might have different sizes in future
                layerW = ConvertPixels( layers[i].Surface.Width );
                layerH = ConvertPixels( layers[i].Surface.Width );
                
                writer.WriteStartElement ("draw", "frame", nsdraw);
                writer.WriteAttributeString ("draw", "layer", nsdraw, "Layout");
                writer.WriteAttributeString ("svg", "width", nssvg, layerW + units);
                writer.WriteAttributeString ("svg", "height", nssvg, layerH + units);
                writer.WriteAttributeString ("svg", "x", nssvg, "0" + units);
                writer.WriteAttributeString ("svg", "y", nssvg, "0" + units);
                
                writer.WriteStartElement ("image", nsdraw);
                writer.WriteAttributeString ("xlink", "href", nsxlink, "data/layer" + i.ToString () + ".png");
                writer.WriteAttributeString ("xlink", "type", nsxlink, "simple");
                writer.WriteAttributeString ("xlink", "show", nsxlink, "embed");
                writer.WriteAttributeString ("xlink", "actuate", nsxlink, "onload");

                writer.WriteEndElement (); // draw:image
                writer.WriteEndElement (); // draw:frame
            }

            writer.WriteEndElement (); // page
            writer.WriteEndElement (); // drawing
            writer.WriteEndElement (); // body
            writer.WriteEndElement (); // document-content

            writer.Close ();
            return stream.ToArray ();
        }
        
        // manifest.xml required by OpenDocument
        // a manifest listing all the contents of the Zip archive
        private byte[] GetManifestXmlData (List<UserLayer> layers)
        {
            const string ns = "urn:oasis:names:tc:opendocument:xmlns:manifest:1.0";
            const string prefix = "manifest";
            MemoryStream stream = new MemoryStream ();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = false;
            settings.ConformanceLevel = ConformanceLevel.Document;
            settings.CloseOutput = false;
            XmlWriter writer = XmlWriter.Create (stream, settings);
            
            writer.WriteStartElement ("manifest", "manifest", ns);
            writer.WriteAttributeString("xmlns", prefix, null, ns);
            // mimetype
            writer.WriteStartElement ("file-entry", ns);
            // you might think path should be "/mimetype"
            // but OpenOffice uses "/"
            writer.WriteAttributeString ("full-path", ns, "/");
            writer.WriteAttributeString ("media-type", ns, MimeType);
            writer.WriteEndElement ();

            // merged imaged
            writer.WriteStartElement ("file-entry", ns);
            writer.WriteAttributeString ("full-path", ns, "mergedimage.png");
            writer.WriteAttributeString ("media-type", ns, "image/png");
            writer.WriteEndElement ();            

            // thumbnail
            writer.WriteStartElement ("file-entry", ns);
            writer.WriteAttributeString ("full-path", ns, "Thumbnails/thumbnail.png");
            writer.WriteAttributeString ("media-type", ns, "image/png");
            writer.WriteEndElement ();
            
            // content.xml
            writer.WriteStartElement ("file-entry", ns);
            writer.WriteAttributeString ("full-path", ns, "content.xml");
            writer.WriteAttributeString ("media-type", ns, "text/xml");
            writer.WriteEndElement ();

            // meta.xml
            writer.WriteStartElement ("file-entry", ns);
            writer.WriteAttributeString ("full-path", ns, "meta.xml");
            writer.WriteAttributeString ("media-type", ns, "text/xml");
            writer.WriteEndElement ();            
            
            // stack.xml
            writer.WriteStartElement ("file-entry", ns);
            writer.WriteAttributeString ("full-path", ns, "stack.xml");
            writer.WriteAttributeString ("media-type", ns, "text/xml");
            writer.WriteEndElement ();
            
            // OpenRaster keeps images in data/
            // OpenDocument keeps images in Pictures/ but using data/ also works
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                writer.WriteStartElement ("file-entry", ns);
                writer.WriteAttributeString ("full-path", ns, "data/layer" + i.ToString () + ".png");
                writer.WriteAttributeString ("media-type", ns, "image/png");
                writer.WriteEndElement ();
            }

            writer.WriteEndElement (); // manifest

            writer.Close ();
            return stream.ToArray ();
        }

        // meta.xml OpenDocument optional in theory
        // in practice required to avoid unwanted error messages
        private byte[] GetMetaXmlData (Document document)
        {
            string useragent = PintaCore.ApplicationName + "/" + PintaCore.ApplicationVersion + "$" + Environment.OSVersion.ToString ();

            const string prefix = "meta";
            
            MemoryStream stream = new MemoryStream ();
            XmlWriterSettings settings = new XmlWriterSettings ();
            settings.Indent = true;
            settings.OmitXmlDeclaration = false;
            settings.ConformanceLevel = ConformanceLevel.Document;
            settings.CloseOutput = false;
            XmlWriter writer = XmlWriter.Create (stream, settings);
            
            settings.NewLineOnAttributes = true; // turn on extra line breaks
            writer.WriteStartElement ("office", "document-meta", nsoffice);
            writer.WriteAttributeString ("xmlns", "office", null, nsoffice);
            writer.WriteAttributeString ("xmlns", prefix, null, nsmeta);
            writer.WriteAttributeString ("xmlns", "dc", null, nsdc);
            writer.WriteStartElement ("office", "meta", nsoffice);
            settings.NewLineOnAttributes = false; // turn off extra line breaks

            // meta generator
            writer.WriteStartElement (prefix, "generator", nsmeta);
            writer.WriteString (useragent);
            writer.WriteEndElement ();
            // TODO date / creation date / other dates // vlow priority
            // Author/Creator dc:creator
            writer.WriteStartElement ("dc", "creator", nsdc);
            writer.WriteString (document.Author);
            writer.WriteEndElement ();
            // Title
            writer.WriteStartElement ("dc", "title", nsdc);
            writer.WriteString (document.Title);
            writer.WriteEndElement ();
            // Subject
            writer.WriteStartElement ("dc", "subject", nsdc);
            writer.WriteString (document.Subject);
            writer.WriteEndElement ();
            // Publisher
            writer.WriteStartElement ("dc", "publisher", nsdc);
            writer.WriteString ( "" );
            writer.WriteEndElement ();
            // Comments/Description
            writer.WriteStartElement ("dc", "description", nsdc);
            writer.WriteString(document.Comments);
            writer.WriteEndElement ();
            // keywords, multiple tags
            writer.WriteStartElement ("meta", "keyword", nsmeta);
            writer.WriteString (document.Keywords);
            writer.WriteEndElement ();
            
            // User defined: Name Value pairs
            writer.WriteStartElement ("meta", "user-defined", nsmeta);
            string userdefname = "";
            string userdefvalue = "";
            writer.WriteAttributeString ("meta", "name", nsmeta, userdefname);
            writer.WriteString ( userdefvalue );
            writer.WriteEndElement ();
            
            writer.WriteEndElement (); // meta
            writer.WriteEndElement (); // document-meta
            writer.Close ();
            return stream.ToArray ();
        }

    }
}