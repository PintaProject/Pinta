using System;
using System.Globalization;
using System.Text;
using Pinta.Core;

namespace Cairo;

// TODO-GTK4 (bindings, unsubmitted) - should this be added to gir.core?
public readonly record struct Color (
	double R,
	double G,
	double B,
	double A)
:
	IInterpolableColor<Color>,
	IAlphaColor<Color>
{
	public static Color Black => new (0, 0, 0);
	public static Color Red => new (1, 0, 0);
	public static Color Green => new (0, 1, 0);
	public static Color Blue => new (0, 0, 1);
	public static Color Yellow => new (1, 1, 0);
	public static Color Magenta => new (1, 0, 1);
	public static Color Cyan => new (0, 1, 1);
	public static Color White => new (1, 1, 1);
	public static Color Transparent => new (0, 0, 0, 0);

	public Color (double r, double g, double b)
		: this (r, g, b, 1.0)
	{ }

	/// <summary>
	/// Returns the color value as a string in hex color format.
	/// </summary>
	/// <param name="addAlpha">If false, returns in format "RRGGBB" (Alpha will not affect result).<br/>
	/// Otherwise, returns in format "RRGGBBAA".</param>
	public string ToHex (bool addAlpha = true)
	{
		int r = Convert.ToInt32 (R * 255.0);
		int g = Convert.ToInt32 (G * 255.0);
		int b = Convert.ToInt32 (B * 255.0);
		int a = Convert.ToInt32 (A * 255.0);

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
	public static Color? FromHex (string hex)
	{
		string hashStripped =
			hex.StartsWith ('#')
			? hex[1..]
			: hex;

		// handle shorthand hex
		string lengthAdjusted = ExpandColorHex (hashStripped);

		if (lengthAdjusted.Length != 6 && lengthAdjusted.Length != 8)
			return null;

		try {
			int r = int.Parse (lengthAdjusted.Substring (0, 2), NumberStyles.HexNumber);
			int g = int.Parse (lengthAdjusted.Substring (2, 2), NumberStyles.HexNumber);
			int b = int.Parse (lengthAdjusted.Substring (4, 2), NumberStyles.HexNumber);
			int a =
				(lengthAdjusted.Length > 6)
				? int.Parse (lengthAdjusted.Substring (6, 2), NumberStyles.HexNumber)
				: 255;
			return new (r / 255.0, g / 255.0, b / 255.0, a / 255.0);
		} catch {
			return null;
		}
	}

	/// <param name="hex">
	/// Hexadecimal color representation without the hash symbol
	/// </param>
	static string ExpandColorHex (string hex)
	{
		switch (hex.Length) {
			case 3:
			case 4:
				StringBuilder expanded = new (hex.Length * 2);
				for (int i = 0; i < hex.Length; i++)
					expanded.Append (hex[i]);
				return expanded.ToString ();

			default:
				return hex;
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
	public HsvColor ToHsv ()
	{
		// In this function, R, G, and B values must be scaled
		// to be between 0 and 1.
		// HsvColor.Hue will be a value between 0 and 360, and
		// HsvColor.Saturation and value are between 0 and 1.

		double h, s, v;

		double min = Math.Min (Math.Min (R, G), B);
		double max = Math.Max (Math.Max (R, G), B);

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
			if (R == max) {
				// Between Yellow and Magenta
				h = (G - B) / delta;
			} else if (G == max) {
				// Between Cyan and Yellow
				h = 2 + (B - R) / delta;
			} else {
				// Between Magenta and Cyan
				h = 4 + (R - G) / delta;
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
		return new HsvColor (h, s, v);
	}

	/// <summary>
	/// Returns a copy of the original color, replacing provided HSV components.
	/// HSV components not changed will retain their values from the original color.
	/// </summary>
	/// <param name="hue">Hue component, 0 - 360</param>
	/// <param name="sat">Saturation component, 0 - 1</param>
	/// <param name="value">Value component, 0 - 1</param>
	/// <param name="alpha">Alpha component, 0 - 1</param>
	public Color CopyHsv (double? hue = null, double? sat = null, double? value = null, double? alpha = null)
	{
		var hsv = ToHsv ();

		double h = hue ?? hsv.Hue;
		double s = sat ?? hsv.Sat;
		double v = value ?? hsv.Val;
		double a = alpha ?? A;

		return FromHsv (h, s, v, a);
	}

	/// <summary>
	/// Returns a RGBA Cairo color using the given HsvColor.
	/// </summary>
	/// <param name="alpha">Alpha of the new Cairo color, 0 - 1</param>
	public static Color FromHsv (HsvColor hsv, double alpha = 1) => FromHsv (hsv.Hue, hsv.Sat, hsv.Val, alpha);

	/// <summary>
	/// Returns a RGBA Cairo color using the given HSV values.
	/// </summary>
	/// <param name="hue">Hue component, 0 - 360</param>
	/// <param name="sat">Saturation component, 0 - 1</param>
	/// <param name="value">Value component, 0 - 1</param>
	/// <param name="alpha">Alpha component, 0 - 1</param>
	public static Color FromHsv (double hue, double sat, double value, double alpha = 1)
	{
		// HsvColor contains values scaled as in the color wheel.
		// Scale Hue to be between 0 and 360. Saturation
		// and value scale to be between 0 and 1.
		double h = hue % 360.0;

		// Stupid hack!
		// If v or s is set to 0, it results in data loss for hue / sat. So we force it to be slightly above zero.
		double s =
			(sat == 0)
			? 0.0001
			: sat;
		double v =
			(value == 0)
			? 0.0001
			: value;

		// If s is 0, all colors are the same.
		// This is some flavor of gray.
		if (s == 0)
			return new Color (v, v, v, alpha);

		// The color wheel consists of 6 sectors.
		// Figure out which sector you're in.
		double sectorPos = h / 60;
		int sectorNumber = (int) Math.Floor (sectorPos);

		// get the fractional part of the sector.
		// That is, how many degrees into the sector
		// are you?
		double fractionalSector = sectorPos - sectorNumber;

		// Calculate values for the three axes
		// of the color.
		double p = v * (1 - s);
		double q = v * (1 - (s * fractionalSector));
		double t = v * (1 - (s * (1 - fractionalSector)));

		double r;
		double g;
		double b;

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

			default:
				r = 0;
				g = 0;
				b = 0;
				break;
		}

		// return an RgbColor structure, with values scaled
		// to be between 0 and 255.
		return new Color (r, g, b, alpha);
	}

	public static Color Lerp (in Color from, in Color to, double frac)
	{
		return new (
			R: Mathematics.Lerp (from.R, to.R, frac),
			G: Mathematics.Lerp (from.G, to.G, frac),
			B: Mathematics.Lerp (from.B, to.B, frac),
			A: Mathematics.Lerp (from.A, to.A, frac));
	}
}
