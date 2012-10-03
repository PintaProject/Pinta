// 
// ImageImporter.cs
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
using Mono.Addins;

namespace Pinta.Core
{
	[TypeExtensionPoint]
	public interface IImageImporter
	{
		/// <summary>
		/// Imports a document into Pinta.
		/// </summary>
		/// <param name='fileName'>
		/// The name of the file to be imported.
		/// </param>
		void Import (string fileName);

		/// <summary>
		/// Returns a thumbnail of an image.
		/// If the format provides an efficient way to load a thumbnail (such as
		/// with the OpenRaster format), it is suggested to use that method to
		/// load the thumbnail if possible.
		/// </summary>
		/// <returns>
		/// The thumbnail, or null if the image could not be loaded.
		/// </returns>			
		/// <param name='maxWidth'>
		/// The maximum width of the thumbnail.
		/// </param>
		/// <param name='maxHeight'>
		/// The maximum height of the thumbnail.
		/// </param>
		Gdk.Pixbuf LoadThumbnail (string filename, int maxWidth, int maxHeight);
	}
}
