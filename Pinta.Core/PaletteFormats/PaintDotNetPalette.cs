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
	public class PaintDotNetPalette : IPaletteLoader, IPaletteSaver
	{
		public List<Color> Load (string fileName)
		{
			List<Color> colors = new List<Color> ();
			StreamReader reader = new StreamReader (fileName);

			try {
				string line = reader.ReadLine ();
				do {
					if (line.IndexOf (';') == 0)
						continue;

					uint color = uint.Parse (line.Substring (0, 8), NumberStyles.HexNumber);
					double b = (color & 0xff) / 255f;
					double g = ((color >> 8) & 0xff) / 255f;
					double r = ((color >> 16) & 0xff) / 255f;
					double a = (color >> 24) / 255f;
					colors.Add (new Color (r, g, b, a));
				} while ((line = reader.ReadLine ()) != null);

				return colors;
			} finally {
				reader.Close ();
			}
		}

		public void Save (List<Color> colors, string fileName)
		{
			StreamWriter writer = new StreamWriter (fileName);
			writer.WriteLine ("; Hexadecimal format: aarrggbb");

			foreach (Color color in colors) {
				byte a = (byte)(color.A * 255);
				byte r = (byte)(color.R * 255);
				byte g = (byte)(color.G * 255);
				byte b = (byte)(color.B * 255);
				writer.WriteLine ("{0:X}", (a << 24) | (r << 16) | (g << 8) | b);
			}
			writer.Close ();
		}
	}
}

