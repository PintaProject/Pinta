// 
// Color.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc
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
using System.ComponentModel;

namespace Xwt.Drawing
{
	struct Color
	{
		double r, g, b, a;

		[NonSerialized]
		HslColor hsl;
		
		public double Red {
			get { return r; }
			set { r = Normalize (value); hsl = null; }
		}
		
		public double Green {
			get { return g; }
			set { g = Normalize (value); hsl = null; }
		}
		
		public double Blue {
			get { return b; }
			set { b = Normalize (value); hsl = null; }
		}
		
		public double Alpha {
			get { return a; }
			set { a = Normalize (value); }
		}
		
		public double Hue {
			get {
				return Hsl.H;
			}
			set {
				Hsl = new HslColor (Normalize (value), Hsl.S, Hsl.L);
			}
		}
		
		public double Saturation {
			get {
				return Hsl.S;
			}
			set {
				Hsl = new HslColor (Hsl.H, Normalize (value), Hsl.L);
			}
		}
		
		public double Light {
			get {
				return Hsl.L;
			}
			set {
				Hsl = new HslColor (Hsl.H, Hsl.S, Normalize (value));
			}
		}
		
		double Normalize (double v)
		{
			if (v < 0) return 0;
			if (v > 1) return 1;
			return v;
		}
		
		public double Brightness {
			get {
				return System.Math.Sqrt (Red * .241 + Green * .691 + Blue * .068);
			}
		}
		
		HslColor Hsl {
			get {
				if (hsl == null)
					hsl = (HslColor)this;
				return hsl;
			}
			set {
				hsl = value;
				Color c = (Color)value;
				r = c.r;
				b = c.b;
				g = c.g;
			}
		}
		
		public Color (double red, double green, double blue): this ()
		{
			Red = red;
			Green = green;
			Blue = blue;
			Alpha = 1f;
		}
		
		public Color (double red, double green, double blue, double alpha): this ()
		{
			Red = red;
			Green = green;
			Blue = blue;
			Alpha = alpha;
		}
		
		public Color WithAlpha (double alpha)
		{
			Color c = this;
			c.Alpha = alpha;
			return c;
		}
		
		public Color WithIncreasedLight (double lightIncrement)
		{
			Color c = this;
			c.Light += lightIncrement;
			return c;
		}

		/// <summary>
		/// Returns a color which looks more contrasted (or less, if amount is negative)
		/// </summary>
		/// <returns>The new color</returns>
		/// <param name="amount">Amount to change (can be positive or negative).</param>
		/// <remarks>
		/// This method adds or removes light to/from the color to make it more contrasted when
		/// compared to a neutral grey.
		/// The resulting effect is that light colors are made lighter, and dark colors
		/// are made darker. If the amount is negative, the effect is inversed (colors are
		/// made less contrasted)
		/// </remarks>
		public Color WithIncreasedContrast (double amount)
		{
			return WithIncreasedContrast (new Color (0.5, 0.5, 0.5), amount);
		}

		/// <summary>
		/// Returns a color which looks more contrasted (or less, if amount is negative) with
		/// respect to a provided reference color.
		/// </summary>
		/// <returns>The new color</returns>
		/// <param name="referenceColor">Reference color.</param>
		/// <param name="amount">Amount to change (can be positive or negative).</param>
		public Color WithIncreasedContrast (Color referenceColor, double amount)
		{
			Color c = this;
			if (referenceColor.Light > Light)
				c.Light -= amount;
			else
				c.Light += amount;
			return c;
		}
			
		public Color BlendWith (Color target, double amount)
		{
			if (amount < 0 || amount > 1)
				throw new ArgumentException ("Blend amount must be between 0 and 1");
			return new Color (BlendValue (r, target.r, amount), BlendValue (g, target.g, amount), BlendValue (b, target.b, amount), target.Alpha);
		}
		
		double BlendValue (double s, double t, double amount)
		{
			return s + (t - s) * amount;
		}
	
		public static Color FromBytes (byte red, byte green, byte blue)
		{
			return FromBytes (red, green, blue, 255);
		}
		
		public static Color FromBytes (byte red, byte green, byte blue, byte alpha)
		{
			return new Color {
				Red = ((double)red) / 255.0,
				Green = ((double)green) / 255.0,
				Blue = ((double)blue) / 255.0,
				Alpha = ((double)alpha) / 255.0
			};
		}
		
		public static Color FromHsl (double h, double s, double l)
		{
			return FromHsl (h, s, l, 1);
		}
		
		public static Color FromHsl (double h, double s, double l, double alpha)
		{
			HslColor hsl = new HslColor (h, s, l);
			Color c = (Color)hsl;
			c.Alpha = alpha;
			c.hsl = hsl;
			return c;
		}
		
		public static Color FromName (string name)
		{
			Color color;
			TryParse (name, out color);
			return color;
		}
		
		public static bool TryParse (string name, out Color color)
		{
			if (name == null)
				throw new ArgumentNullException ("name");

			uint val;
			if (name.Length == 0 || !TryParseColourFromHex (name, out val)) {
				color = default (Color);
				return false;
			}
			color = Color.FromBytes ((byte)(val >> 24), (byte)((val >> 16) & 0xff), (byte)((val >> 8) & 0xff), (byte)(val & 0xff));
			return true;
		}
		

		static bool TryParseColourFromHex (string str, out uint val)
		{
			val = 0;
			
			if (str[0] != '#' || str.Length > 9)
				return false;
			
			if (!uint.TryParse (str.Substring (1), System.Globalization.NumberStyles.HexNumber, null, out val))
				return false;
			
			val = val << ((9 - str.Length) * 4);
			
			if (str.Length <= 7)
				val |= 0xff;
			
			return true;
		}
		
		public static bool operator == (Color c1, Color c2)
		{
			return c1.r == c2.r && c1.g == c2.g && c1.b == c2.b && c1.a == c2.a;
		}
		
		public static bool operator != (Color c1, Color c2)
		{
			return c1.r != c2.r || c1.g != c2.g || c1.b != c2.b || c1.a != c2.a;
		}
		
		public override bool Equals (object o)
		{
			if (!(o is Color))
				return false;
		
			return (this == (Color) o);
		}
		
		public override int GetHashCode ()
		{
			unchecked {
				var hash = r.GetHashCode ();
				hash = (hash * 397) ^ g.GetHashCode ();
				hash = (hash * 397) ^ b.GetHashCode ();
				hash = (hash * 397) ^ a.GetHashCode ();
				return hash;
			}
		}
		
		public override string ToString ()
		{
			return string.Format ("[Color: Red={0}, Green={1}, Blue={2}, Alpha={3}]", Red, Green, Blue, Alpha);
		}

		public string ToHexString ()
		{
			return "#" + ((int)(Red * 255)).ToString ("x2") + ((int)(Green * 255)).ToString ("x2") + ((int)(Blue * 255)).ToString ("x2") + ((int)(Alpha * 255)).ToString ("x2");
		}
	}
}

