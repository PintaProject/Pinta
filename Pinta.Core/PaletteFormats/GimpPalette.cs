//
// PaintDotNetPalette.cs
//
// Author:
//       Matthias Mailänder, Maia Kozheva
//
// Copyright (c) 2010 Maia Kozheva <sikon@ubuntu.com>
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

using System;
using System.IO;
using Cairo;
using System.Collections.Generic;
using System.Globalization;

namespace Pinta.Core
{
	public class GimpPalette : IPaletteLoader, IPaletteSaver
	{
		public List<Color> Load (string fileName)
		{
			List<Color> colors = new List<Color> ();
			StreamReader reader = new StreamReader (fileName);
			string line = reader.ReadLine ();

			if (!line.StartsWith ("GIMP"))
				throw new InvalidDataException("Not a valid GIMP palette file.");

			// skip everything until the first color
			while (!char.IsDigit(line[0]))
				line = reader.ReadLine ();

			// then read the palette
			do {
				if (line.IndexOf ('#') == 0)
					continue;

				string[] split = line.Split ((char[]) null, StringSplitOptions.RemoveEmptyEntries);
				double r = int.Parse (split[0]) / 255f;
				double g = int.Parse (split[1]) / 255f;
				double b = int.Parse (split[2]) / 255f;
				colors.Add (new Color (r, g, b));
			} while ((line = reader.ReadLine ()) != null);

			return colors;
		}

		public void Save (List<Color> colors, string fileName)
		{
			StreamWriter writer = new StreamWriter (fileName);
			writer.WriteLine ("GIMP Palette");
			writer.WriteLine ("Name: Pinta Created {0}", DateTime.Now.ToString (DateTimeFormatInfo.InvariantInfo.RFC1123Pattern));
			writer.WriteLine ("#");

			foreach (Color color in colors) {
				writer.WriteLine ("{0,3} {1,3} {2,3} Untitled", (int) (color.R * 255), (int) (color.G * 255), (int) (color.B * 255));
			}

			writer.Close ();
		}
	}
}

