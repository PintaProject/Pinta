//
// PaletteFormats.cs
//
// Author:
//       Matthias Mailänder
//
// Copyright (c) 2017 
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
using System.Collections.Generic;
using System.Linq;


namespace Pinta.Core;

public sealed class PaletteFormatManager
{
	private readonly List<PaletteDescriptor> formats;

	public PaletteFormatManager ()
	{
		formats = [];

		PaintDotNetPalette pdnHandler = new PaintDotNetPalette ();
		formats.Add (new PaletteDescriptor ("Paint.NET", ["txt", "TXT"], pdnHandler, pdnHandler));

		GimpPalette gimpHandler = new GimpPalette ();
		formats.Add (new PaletteDescriptor ("GIMP", ["gpl", "GPL"], gimpHandler, gimpHandler));

		PaintShopProPalette pspHandler = new PaintShopProPalette ();
		formats.Add (new PaletteDescriptor ("PaintShop Pro", ["pal", "PAL"], pspHandler, pspHandler));
	}

	public IEnumerable<PaletteDescriptor> Formats => formats;

	public PaletteDescriptor? GetFormatByFilename (string fileName)
	{
		string extension = System.IO.Path.GetExtension (fileName);
		extension = NormalizeExtension (extension);
		return formats.Where (p => p.Extensions.Contains (extension)).FirstOrDefault ();
	}

	private static string NormalizeExtension (string extension)
	{
		return extension.ToLowerInvariant ().TrimStart ('.').Trim ();
	}
}
