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
	public class TgaExporter : IImageExporter
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

			public readonly void WriteTo (BinaryWriter output)
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
		public void Export (Document document, Gio.File file, Gtk.Window parent)
		{
			ImageSurface surf = document.GetFlattenedImage (); // Assumes the surface is in ARGB32 format
			using var file_stream = new GioStream (file.Replace ());
			using var writer = new BinaryWriter (file_stream);

			TgaHeader header = new TgaHeader {
				idLength = (byte) (ImageIdField.Length + 1),
				cmapType = 0,
				imageType = 2, // uncompressed RGB
				cmapIndex = 0,
				cmapLength = 0,
				cmapEntrySize = 0,
				xOrigin = 0,
				yOrigin = 0,
				imageWidth = (ushort) surf.Width,
				imageHeight = (ushort) surf.Height,
				pixelDepth = 32,
				imageDesc = 8 // 32-bit, lower-left origin, which is weird but hey...
			};
			header.WriteTo (writer);

			writer.Write (ImageIdField);

			Span<byte> data = surf.GetData ();

			// It just so happens that the Cairo ARGB32 internal representation matches
			// the TGA format, except vertically-flipped. In little-endian, of course.
			for (int y = surf.Height - 1; y >= 0; y--)
				writer.Write (data.Slice (surf.Stride * y, surf.Stride));

		}
	}
}
