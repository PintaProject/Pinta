using System;
using Gtk;
using NUnit.Framework;
using Pango;
using Color = Cairo.Color;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class ColorTests
{
	[TestCase (1, 0, 0, 0, 1, 1)]
	[TestCase (0, 1, 0, 120, 1, 1)]
	[TestCase (0, 0, 1, 240, 1, 1)]
	[TestCase (0, 0.5, 1, 210, 1, 1)]
	[TestCase (0.2, 0.5, 0.25, 130, 0.6, 0.5)]
	public void ColorToHsv (double r, double g, double b, double h, double s, double v)
	{
		Color c = new (r, g, b);
		HsvColor hsv = c.ToHsv ();
		Assert.That (hsv, Is.EqualTo (new HsvColor (h, s, v)));
		Color c2 = hsv.ToColor ();
		// assert reversibility; color > hsv > color retains same info
		// floating point rounding
		c2 = new (Math.Round (c2.R, 4), Math.Round (c2.G, 4), Math.Round (c2.B, 4));
		Assert.That (c2, Is.EqualTo (c));
	}

	[TestCase ("FFFFFF", 1, 1, 1, 1)]
	[TestCase ("FFFF", 1, 1, 1, 1)]
	[TestCase ("FFF", 1, 1, 1, 1)]
	[TestCase ("#FFFFFF", 1, 1, 1, 1)]
	[TestCase ("FFFF", 1, 1, 1, 1)]
	[TestCase ("#FFF", 1, 1, 1, 1)]
	[TestCase ("CC33AA99", 0.8, 0.2, 0.6667, 0.6)]
	[TestCase ("#CC33AA99", 0.8, 0.2, 0.6667, 0.6)]
	[TestCase ("C3A9", 0.8, 0.2, 0.6667, 0.6)]
	[TestCase ("C3A", 0.8, 0.2, 0.6667, 1)]
	public void Hex (string hex, double r, double g, double b, double a)
	{
		var hc = Color.FromHex (hex)!.Value;
		hc = new (Math.Round (hc.R, 4), Math.Round (hc.G, 4), Math.Round (hc.B, 4));
		var c = new Color (r, g, b);
		Assert.That (hc, Is.EqualTo (c));
	}
}
