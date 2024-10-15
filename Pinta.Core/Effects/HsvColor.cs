/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Core.Extensions;

namespace Pinta.Core;

/// <summary>
/// Adapted from:
/// "A Primer on Building a Color Picker User Control with GDI+ in Visual Basic .NET or C#"
/// http://www.msdnaa.net/Resources/display.aspx?ResID=2460
/// </summary>
public readonly struct HsvColor
{
	public double Hue { get; init; } // 0-360
	public double Sat { get; init; } // 0-1
	public double Val { get; init; } // 0-1

	public static bool operator == (HsvColor c1, HsvColor c2)
	{
		if (Math.Abs (c1.Hue - c2.Hue) < 0.1 &&
		    Math.Abs (c1.Sat - c2.Sat) < 0.001 &&
		    Math.Abs (c1.Val - c2.Val) < 0.001)
			return true;
		return false;
	}

	public static bool operator != (HsvColor c1, HsvColor c2) => !(c1 == c2);

	public HsvColor (double hue, double sat, double val)
	{
		if (hue < 0 || hue > 360)
			throw new ArgumentOutOfRangeException (nameof (hue), "must be in the range [0, 360]");

		if (sat < 0 || sat > 1)
			throw new ArgumentOutOfRangeException (nameof (sat), "must be in the range [0, 1]");

		if (val < 0 || val > 1)
			throw new ArgumentOutOfRangeException (nameof (val), "must be in the range [0, 1]");

		Hue = hue;
		Sat = sat;
		Val = val;
	}

	public static HsvColor FromColor (Color c) => c.ToHsv ();
	public Color ToColor (double alpha = 1) => Color.FromHsv (this);

	public static HsvColor FromRgbD (double r, double g, double b) => new Color (r, g, b).ToHsv ();

	public static HsvColor FromRgbI (int r, int g, int b) => new Color (r / 255.0, g / 255.0, b / 255.0).ToHsv ();

	public static HsvColor FromBgra (ColorBgra c) => new Color (c.R / 255.0, c.G / 255.0, c.B / 255.0).ToHsv ();
	public ColorBgra ToBgra ()
	{
		var c = this.ToColor ();
		return ColorBgra.FromBgr ((byte) (c.B * 255.0), (byte) (c.G * 255.0), (byte) (c.R * 255.0));
	}

	public override readonly string ToString ()
		=> $"({Hue:F2}, {Sat:F2}, {Val:F2})";

	public override int GetHashCode ()
		=> ((int) Hue + ((int) Sat << 8) + ((int) Val << 16)).GetHashCode ();
}
