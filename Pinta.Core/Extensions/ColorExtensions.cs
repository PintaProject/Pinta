using System;
using System.Globalization;
using Cairo;

namespace Pinta.Core.Extensions;

public static class ColorExtensions
{
	/// <summary>
	/// Returns the color value as a string in hex color format.
	/// </summary>
	/// <param name="addAlpha">If false, returns in format "RRGGBB" (Alpha will not affect result).<br/>
	/// Otherwise, returns in format "RRGGBBAA".</param>
	public static String ToHex (this Color c, bool addAlpha = true)
	{
		int r = Convert.ToInt32 (c.R * 255.0);
		int g = Convert.ToInt32 (c.G * 255.0);
		int b = Convert.ToInt32 (c.B * 255.0);
		int a = Convert.ToInt32 (c.A * 255.0);

		if (addAlpha)
			return $"{r:X2}{g:X2}{b:X2}{a:X2}";
		else
			return $"{r:X2}{g:X2}{b:X2}";
	}

	/// <summary>
	/// Returns a color from an RGBA hex color. Accepts the following formats:<br/>
	/// RRGGBBAA<br/>
	/// RRGGBB<br/>
	/// RGB (Expands to RRGGBB)<br/>
	/// RGBA (Expands to RRGGBBAA)<br/>
	/// Will accept leading #.
	/// </summary>
	/// <param name="hex">Hex color as a string.</param>
	/// <returns>Resulting color. If null, the method could not parse it.</returns>
	public static Color? FromHex (String hex)
	{
		if (hex[0] == '#') {
			Console.WriteLine (hex);
			hex = hex.Remove (0, 1);
			Console.WriteLine (hex);
		}

		// handle shorthand hex
		if (hex.Length == 3)
			hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
		if (hex.Length == 4)
			hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}{hex[3]}{hex[3]}";

		if (hex.Length != 6 && hex.Length != 8)
			return null;
		try {
			int r = int.Parse (hex.Substring (0, 2), NumberStyles.HexNumber);
			int g = int.Parse (hex.Substring (2, 2), NumberStyles.HexNumber);
			int b = int.Parse (hex.Substring (4, 2), NumberStyles.HexNumber);
			int a = 255;
			if (hex.Length > 6)
				a = int.Parse (hex.Substring (5, 2), NumberStyles.HexNumber);

			Console.WriteLine ($"{r}, {g}, {b}, {a}");

			return new Color ().SetRgba (r / 255.0, g / 255.0, b / 255.0, a / 255.0);
		} catch {
			return null;
		}
	}

	/// <summary>
	/// Hue, Saturation, Value description of a color.<br/>
	/// Hue varies from 0 - 360.<br/>
	/// Saturation and value varies from 0 - 1.
	/// </summary>
	public struct Hsv
	{
		public readonly double h;
		public readonly double s;
		public readonly double v;

		public Hsv (double h, double s, double v)
		{
			this.h = h;
			this.s = s;
			this.v = v;
		}
	}

	/// <summary>
	/// Copied from RgbColor.ToHsv<br/>
	/// Returns the Cairo color in HSV value.
	/// </summary>
	/// <returns>HSV struct.
	/// Hue varies from 0 - 360.<br/>
	/// Saturation and value varies from 0 - 1.
	/// </returns>
	public static Hsv GetHsv (this Color c)
	{
		// In this function, R, G, and B values must be scaled
		// to be between 0 and 1.
		// HsvColor.Hue will be a value between 0 and 360, and
		// HsvColor.Saturation and value are between 0 and 1.

		double h, s, v;

		double min = Math.Min (Math.Min (c.R, c.G), c.B);
		double max = Math.Max (Math.Max (c.R, c.G), c.B);

		double delta = max - min;

		if (max == 0 || delta == 0) {
			// R, G, and B must be 0, or all the same.
			// In this case, S is 0, and H is undefined.
			// Using H = 0 is as good as any...
			s = 0;
			h = 0;
			v = max;
		} else {
			s = delta / max;
			if (c.R == max) {
				// Between Yellow and Magenta
				h = (c.G - c.B) / delta;
			} else if (c.G == max) {
				// Between Cyan and Yellow
				h = 2 + (c.B - c.R) / delta;
			} else {
				// Between Magenta and Cyan
				h = 4 + (c.R - c.G) / delta;
			}
			v = max;
		}
		// Scale h to be between 0 and 360.
		// This may require adding 360, if the value
		// is negative.
		h *= 60;

		if (h < 0) {
			h += 360;
		}

		// Scale to the requirements of this
		// application. All values are between 0 and 255.
		return new Hsv (h, s, v);
	}

	/// <summary>
	/// Returns a copy of the original color, replacing provided RGBA components.
	/// </summary>
	/// <param name="r">Red component, 0 - 1</param>
	/// <param name="g">Green component, 0 - 1</param>
	/// <param name="b">Blue component, 0 - 1</param>
	/// <param name="a">Alpha component, 0 - 1</param>
	public static Color SetRgba (this Color c, double? r = null, double? g = null, double? b = null, double? a = null)
	{
		return new Color (r ?? c.R, g ?? c.G, b ?? c.B, a ?? c.A);
	}

	/// <summary>
	/// Returns a copy of the original color, replacing provided HSV components.
	/// </summary>
	/// <param name="hue">Hue component, 0 - 360</param>
	/// <param name="sat">Saturation component, 0 - 1</param>
	/// <param name="value">Value component, 0 - 1</param>
	/// <param name="alpha">Alpha component, 0 - 1</param>
	public static Color SetHsv (this Color c, double? hue = null, double? sat = null, double? value = null, double? alpha = null)
	{
		var hsv = c.GetHsv ();

		double h = hue ?? hsv.h;
		double s = sat ?? hsv.s;
		double v = value ?? hsv.v;
		double a = alpha ?? c.A;

		return FromHsv (h, s, v, a);
	}

	/// <summary>
	/// Copied from HsvColor.ToRgb<br/>
	/// Returns a Cairo color using the given HSV values.
	/// </summary>
	/// <param name="hue">Hue component, 0 - 360</param>
	/// <param name="sat">Saturation component, 0 - 1</param>
	/// <param name="value">Value component, 0 - 1</param>
	/// <param name="alpha">Alpha component, 0 - 1</param>
	public static Color FromHsv (double hue, double sat, double value, double alpha = 1)
	{
		double h = hue;
		double s = sat;
		double v = value;

		// Stupid hack!
		// If v or s is set to 0, it results in data loss for hue / sat. So we force it to be slightly above zero.
		if (v == 0)
			v = 0.0001;
		if (s == 0)
			s = 0.0001;

		// HsvColor contains values scaled as in the color wheel.
		// Scale Hue to be between 0 and 360. Saturation
		// and value scale to be between 0 and 1.
		h %= 360.0;

		double r = 0;
		double g = 0;
		double b = 0;

		if (s == 0) {
			// If s is 0, all colors are the same.
			// This is some flavor of gray.
			r = v;
			g = v;
			b = v;
		} else {
			// The color wheel consists of 6 sectors.
			// Figure out which sector you're in.
			double sectorPos = h / 60;
			int sectorNumber = (int) (Math.Floor (sectorPos));

			// get the fractional part of the sector.
			// That is, how many degrees into the sector
			// are you?
			double fractionalSector = sectorPos - sectorNumber;

			// Calculate values for the three axes
			// of the color.
			double p = v * (1 - s);
			double q = v * (1 - (s * fractionalSector));
			double t = v * (1 - (s * (1 - fractionalSector)));

			// Assign the fractional colors to r, g, and b
			// based on the sector the angle is in.
			switch (sectorNumber) {
				case 0:
					r = v;
					g = t;
					b = p;
					break;

				case 1:
					r = q;
					g = v;
					b = p;
					break;

				case 2:
					r = p;
					g = v;
					b = t;
					break;

				case 3:
					r = p;
					g = q;
					b = v;
					break;

				case 4:
					r = t;
					g = p;
					b = v;
					break;

				case 5:
					r = v;
					g = p;
					b = q;
					break;
			}
		}
		// return an RgbColor structure, with values scaled
		// to be between 0 and 255.
		return new Color (r, g, b, alpha);
	}
}
