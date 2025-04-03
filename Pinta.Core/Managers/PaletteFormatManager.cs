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
using System.IO;
using System.Linq;

namespace Pinta.Core;

public sealed class PaletteFormatManager
{
	public IReadOnlyList<PaletteDescriptor> Formats { get; }

	public PaletteFormatManager ()
	{
		PaintDotNetPalette pdnHandler = new ();
		GimpPalette gimpHandler = new ();
		PaintShopProPalette pspHandler = new ();

		Formats = [
			new PaletteDescriptor (
				"Paint.NET",
				["txt", "TXT"],
				pdnHandler,
				pdnHandler),
			new PaletteDescriptor (
				"GIMP",
				["gpl", "GPL"],
				gimpHandler,
				gimpHandler),
			new PaletteDescriptor (
				"PaintShop Pro",
				["pal", "PAL"],
				pspHandler,
				pspHandler),
		];
	}

	public PaletteDescriptor? GetFormatByFilename (string fileName)
	{
		string extension = Path.GetExtension (fileName);

		string normalized =
			extension
			.ToLowerInvariant ()
			.TrimStart ('.')
			.Trim ();

		return
			Formats
			.Where (p => p.Extensions.Contains (normalized))
			.FirstOrDefault ();
	}
}
