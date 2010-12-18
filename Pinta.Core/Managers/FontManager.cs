// 
// FontManager.cs
//  
// Authors:
//	Olivier Dufour <olivier.duff@gmail.com>
//	Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
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
using System.Linq;
using Gdk;
using Pango;

namespace Pinta.Core
{
	public class FontManager
	{
		private List<FontFamily> families;

		private List<int> default_font_sizes = new List<int> (new int[] { 6, 7, 8, 9, 10, 11, 12, 14, 16,
				18, 20, 22, 24, 26, 28, 32, 36, 40, 44,
				48, 54, 60, 66, 72, 80, 88, 96 });

		public FontManager ()
		{
			families = new List<FontFamily> ();

			using (Pango.Context c = PangoHelper.ContextGet ())
				families.AddRange (c.Families);
		}

		public List<string> GetInstalledFonts ()
		{
			return families.Select (f => f.Name).ToList ();
		}

		public FontFamily GetFamily (string fontname)
		{
			return families.Find (f => f.Name == fontname);
		}

		public List<int> GetSizes (FontFamily family)
		{
			return GetSizes (family.Faces[0]);
		}

		unsafe public List<int> GetSizes (FontFace fontFace)
		{
			int sizes;
			int nsizes;

			// Query for supported sizes for this font
			fontFace.ListSizes (out sizes, out nsizes);

			if (nsizes == 0)
				return default_font_sizes;

			List<int> result = new List<int> ();

			for (int i = 0; i < nsizes; i++)
				result.Add (*(&sizes + 4 * i));

			return result;
		}
		
	}
}
