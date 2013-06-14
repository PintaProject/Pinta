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
		/// <param name='filename'>The name of the file to be imported.</param>
		/// <param name='parent'>
		/// Window to be used as a parent for any dialogs that are shown.
		/// </param>
		void Import (string filename, Gtk.Window parent);

		/// <summary>
		/// Returns a thumbnail of an image.
		/// If the format provides an efficient way to load a smaller version of
		/// the image, it is suggested to use that method to load a thumbnail
		/// no larger than the given width and height parameters. Otherwise, the
		/// returned pixbuf will need to be rescaled by the calling code if it
		/// exceeds the maximum size.
		/// </summary>
		/// <param name='filename'>The name of the file to be imported.</param>
		/// <param name='maxWidth'>The maximum width of the thumbnail.</param>
		/// <param name='maxHeight'>The maximum height of the thumbnail.</param>
		/// <param name='parent'>
		/// Window to be used as a parent for any dialogs that are shown.
		/// </param>
		/// <returns>The thumbnail, or null if the image could not be loaded.</returns>
		Gdk.Pixbuf LoadThumbnail (string filename, int maxWidth, int maxHeight,
		                          Gtk.Window parent);
	}
}
