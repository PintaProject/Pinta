// 
// TgaExporter.cs
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
//
// Portions of the code originate from:
//
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Maia Kozheva <sikon@ubuntu.com>                         //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;

using Cairo;

namespace Pinta.Core
{
	public class TgaExporter: IImageExporter
	{
		private struct TgaHeader
		{
			public byte idLength;            // Image ID Field Length
			public byte cmapType;            // Color Map Type
			public byte imageType;           // Image Type

			public ushort cmapIndex;         // First Entry Index
			public ushort cmapLength;        // Color Map Length
			public byte cmapEntrySize;       // Color Map Entry Size

			public ushort xOrigin;           // X-origin of Image
			public ushort yOrigin;           // Y-origin of Image
			public ushort imageWidth;        // Image Width
			public ushort imageHeight;       // Image Height
			public byte pixelDepth;          // Pixel Depth
			public byte imageDesc;           // Image Descriptor

			public void WriteTo (BinaryWriter output)
			{
				output.Write (this.idLength);
				output.Write (this.cmapType);
				output.Write (this.imageType);

				output.Write (this.cmapIndex);
				output.Write (this.cmapLength);
				output.Write (this.cmapEntrySize);

				output.Write (this.xOrigin);
				output.Write (this.yOrigin);
				output.Write (this.imageWidth);
				output.Write (this.imageHeight);
				output.Write (this.pixelDepth);
				output.Write (this.imageDesc);
			}
		}

		/// <summary>
		/// The image ID field contents. It is important for this field to be non-empty, since
		/// GDK incorrectly identifies the mime type as image/x-win-bitmap if the idLength
		/// value is 0 (see bug #987641).
		/// </summary>
		private const string ImageIdField = "Created by Pinta";
		
		// For now, we only export in uncompressed ARGB32 format. If someone requests this functionality,
		// we can always add more through an export dialog.
		public void Export (Document document, string fileName, Gtk.Window parent) {
			ImageSurface surf = document.GetFlattenedImage (); // Assumes the surface is in ARGB32 format
			BinaryWriter writer = new BinaryWriter (new FileStream (fileName, FileMode.Create, FileAccess.Write));
	
			try {
				TgaHeader header = new TgaHeader();

				header.idLength = (byte) (ImageIdField.Length + 1);
				header.cmapType = 0;
				header.imageType = 2; // uncompressed RGB
				header.cmapIndex = 0;
				header.cmapLength = 0;
				header.cmapEntrySize = 0;
				header.xOrigin = 0;
				header.yOrigin = 0;
				header.imageWidth = (ushort) surf.Width;
				header.imageHeight = (ushort) surf.Height;
				header.pixelDepth = 32;
				header.imageDesc = 8; // 32-bit, lower-left origin, which is weird but hey...
				header.WriteTo (writer);

				writer.Write(ImageIdField);
				
				byte[] data = surf.Data;
				
				// It just so happens that the Cairo ARGB32 internal representation matches
				// the TGA format, except vertically-flipped. In little-endian, of course.
				for (int y = surf.Height - 1; y >= 0; y--)
					writer.Write (data, surf.Stride * y, surf.Stride);
			} finally {
				(surf as IDisposable).Dispose ();
				writer.Close ();
			}
		}
	}
}
