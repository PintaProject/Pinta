// 
// Palette.cs
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Cairo;

namespace Pinta.Core
{
	public sealed class Palette
	{
		public enum FileFormat { PDN, GIMP }
		public event EventHandler PaletteChanged;
	
		private List<Color> colors;

		private Palette ()
		{
			colors = new List<Color> ();
		}
		
		private void OnPaletteChanged ()
		{
			if (PaletteChanged != null)
				PaletteChanged (this, EventArgs.Empty);
		}
		
		public int Count
		{
			get {
				return colors.Count;
			}
		}
		
		public Color this[int index]
		{
			get {
				return colors[index];
			}
			
			set {
				colors[index] = value;
				OnPaletteChanged ();
			}
		}
		
		public void Resize (int newSize)
		{
			int difference = newSize - Count;
			
			if (difference > 0) {
				for (int i = 0; i < difference; i++)
					colors.Add (new Color (1, 1, 1));
			} else {
				colors.RemoveRange (newSize, -difference);
			}
			
			colors.TrimExcess ();
			OnPaletteChanged ();
		}

		public static Palette GetDefault ()
		{
			Palette p = new Palette ();
			p.LoadDefault ();
			return p;
		}
		
		public static Palette FromFile (string fileName)
		{
			Palette p = new Palette ();
			p.Load (fileName);
			return p;
		}
		
		public void LoadDefault ()
		{
			colors.Clear ();

			colors.Add (new Color (255 / 255f, 255 / 255f, 255 / 255f));
			colors.Add (new Color (128 / 255f, 128 / 255f, 128 / 255f));
			colors.Add (new Color (127 / 255f, 0 / 255f, 0 / 255f));
			colors.Add (new Color (127 / 255f, 51 / 255f, 0 / 255f));
			colors.Add (new Color (127 / 255f, 106 / 255f, 0 / 255f));
			colors.Add (new Color (91 / 255f, 127 / 255f, 0 / 255f));
			colors.Add (new Color (38 / 255f, 127 / 255f, 0 / 255f));
			colors.Add (new Color (0 / 255f, 127 / 255f, 14 / 255f));
			colors.Add (new Color (0 / 255f, 127 / 255f, 70 / 255f));
			colors.Add (new Color (0 / 255f, 127 / 255f, 127 / 255f));
			colors.Add (new Color (0 / 255f, 74 / 255f, 127 / 255f));
			colors.Add (new Color (0 / 255f, 19 / 255f, 127 / 255f));
			colors.Add (new Color (33 / 255f, 0 / 255f, 127 / 255f));
			colors.Add (new Color (87 / 255f, 0 / 255f, 127 / 255f));
			colors.Add (new Color (127 / 255f, 0 / 255f, 110 / 255f));
			colors.Add (new Color (127 / 255f, 0 / 255f, 55 / 255f));

			colors.Add (new Color (0 / 255f, 0 / 255f, 0 / 255f));
			colors.Add (new Color (64 / 255f, 64 / 255f, 64 / 255f));
			colors.Add (new Color (255 / 255f, 0 / 255f, 0 / 255f));
			colors.Add (new Color (255 / 255f, 106 / 255f, 0 / 255f));
			colors.Add (new Color (255 / 255f, 216 / 255f, 0 / 255f));
			colors.Add (new Color (182 / 255f, 255 / 255f, 0 / 255f));
			colors.Add (new Color (76 / 255f, 255 / 255f, 0 / 255f));
			colors.Add (new Color (0 / 255f, 255 / 255f, 33 / 255f));
			colors.Add (new Color (0 / 255f, 255 / 255f, 144 / 255f));
			colors.Add (new Color (0 / 255f, 255 / 255f, 255 / 255f));
			colors.Add (new Color (0 / 255f, 148 / 255f, 255 / 255f));
			colors.Add (new Color (0 / 255f, 38 / 255f, 255 / 255f));
			colors.Add (new Color (72 / 255f, 0 / 255f, 255 / 255f));
			colors.Add (new Color (178 / 255f, 0 / 255f, 255 / 255f));
			colors.Add (new Color (255 / 255f, 0 / 255f, 220 / 255f));
			colors.Add (new Color (255 / 255f, 0 / 255f, 110 / 255f));

			colors.Add (new Color (160 / 255f, 160 / 255f, 160 / 255f));
			colors.Add (new Color (48 / 255f, 48 / 255f, 48 / 255f));
			colors.Add (new Color (255 / 255f, 127 / 255f, 127 / 255f));
			colors.Add (new Color (255 / 255f, 178 / 255f, 127 / 255f));
			colors.Add (new Color (255 / 255f, 233 / 255f, 127 / 255f));
			colors.Add (new Color (218 / 255f, 255 / 255f, 127 / 255f));
			colors.Add (new Color (165 / 255f, 255 / 255f, 127 / 255f));
			colors.Add (new Color (127 / 255f, 255 / 255f, 142 / 255f));
			colors.Add (new Color (127 / 255f, 255 / 255f, 197 / 255f));
			colors.Add (new Color (127 / 255f, 255 / 255f, 255 / 255f));
			colors.Add (new Color (127 / 255f, 201 / 255f, 255 / 255f));
			colors.Add (new Color (127 / 255f, 146 / 255f, 255 / 255f));
			colors.Add (new Color (161 / 255f, 127 / 255f, 255 / 255f));
			colors.Add (new Color (214 / 255f, 127 / 255f, 255 / 255f));
			colors.Add (new Color (255 / 255f, 127 / 255f, 237 / 255f));
			colors.Add (new Color (255 / 255f, 127 / 255f, 182 / 255f));

			colors.TrimExcess ();
			OnPaletteChanged ();
		}
		
		public void Load (string fileName)
		{
			List<Color> tmpColors = new List<Color> ();
			StreamReader reader = new StreamReader (fileName);
			
			try {
				string line = reader.ReadLine ();
				
				if (line.IndexOf ("GIMP") != 0) {
					// Assume PDN palette
					do {
						if (line.IndexOf (';') == 0)
							continue;
						
						uint color = uint.Parse (line.Substring (0, 8), NumberStyles.HexNumber);
						double b = (color & 0xff) / 255f;
						double g = ((color >> 8) & 0xff) / 255f;
						double r = ((color >> 16) & 0xff) / 255f;
						double a = (color >> 24)  / 255f;
						tmpColors.Add (new Color (r, g, b, a));
					} while ((line = reader.ReadLine ()) != null);
				} else {
					// GIMP palette: skip everything until the first color
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
						tmpColors.Add (new Color (r, g, b));
					} while ((line = reader.ReadLine ()) != null);
				}
			
				colors = tmpColors;
				colors.TrimExcess ();
				OnPaletteChanged ();
			} finally {
				reader.Close ();
			}
		}
		
		public void Save (string fileName, FileFormat format)
		{
			StreamWriter writer = new StreamWriter (fileName);
		
			if (format == FileFormat.PDN) {
				writer.WriteLine ("; Hexadecimal format: aarrggbb");
			
				foreach (Color color in colors) {
					byte a = (byte) (color.A * 255);
					byte r = (byte) (color.R * 255);
					byte g = (byte) (color.G * 255);
					byte b = (byte) (color.B * 255);
					writer.WriteLine ("{0:X}", (a << 24) | (r << 16) | (g << 8) | b);
				}
			} else {
				// GIMP
				writer.WriteLine ("GIMP Palette");
				writer.WriteLine ("Name: Pinta Created {0}", DateTime.Now.ToString (DateTimeFormatInfo.InvariantInfo.RFC1123Pattern));
				writer.WriteLine ("#");
				
				foreach (Color color in colors) {
					writer.WriteLine ("{0,3} {1,3} {2,3} Untitled", (int) (color.R * 255), (int) (color.G * 255), (int) (color.B * 255));
				}
			}
			
			writer.Close ();
		}
	}
}
