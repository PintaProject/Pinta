//
// PaintShopProPalette.cs
//
// Author:
//       Matthias Mailänder
//
// Copyright (c) 2017 Matthias Mailänder
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
using Cairo;

namespace Pinta.Core;

public sealed class PaintShopProPalette : IPaletteLoader, IPaletteSaver
{
	public List<Color> Load (Gio.File file)
	{
		List<Color> colors = new List<Color> ();
		using var stream = new GioStream (file.Read (null));
		StreamReader reader = new StreamReader (stream);

		string? line = reader.ReadLine ();
		if (line is null || !line.StartsWith ("JASC-PAL"))
			throw new InvalidDataException ("Not a valid PaintShopPro palette file.");

		line = reader.ReadLine (); // version

		int numberOfColors = int.Parse (reader.ReadLine ()!); // NRT - Assumes valid formatted file
		PintaCore.Palette.CurrentPalette.Resize (numberOfColors);

		while (!reader.EndOfStream) {
			line = reader.ReadLine ();

			if (line is null)
				break;

			string[] split = line.Split (' ');
			double r = int.Parse (split[0]) / 255f;
			double g = int.Parse (split[1]) / 255f;
			double b = int.Parse (split[2]) / 255f;
			colors.Add (new Color (r, g, b));
		}

		return colors;
	}

	public void Save (List<Color> colors, Gio.File file)
	{
		using var stream = new GioStream (file.Replace ());
		StreamWriter writer = new StreamWriter (stream);
		writer.WriteLine ("JASC-PAL");
		writer.WriteLine ("0100");
		writer.WriteLine (colors.Count.ToString ());

		foreach (Color color in colors) {
			writer.WriteLine ("{0} {1} {2}", (int) (color.R * 255), (int) (color.G * 255), (int) (color.B * 255));
		}

		writer.Close ();
	}
}

