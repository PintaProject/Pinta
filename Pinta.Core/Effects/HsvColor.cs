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
	public double Sat { get; init;  } // 0-1
	public double Val { get; init;  } // 0-1

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
	public static HsvColor FromRgb (double r, double g, double b) => new Color (r, g, b).ToHsv ();
	public Color ToColor (double alpha = 1) => ColorExtensions.FromHsv (this);

	public ColorBgra ToBgra ()
	{
		var c = this.ToColor ();
		return ColorBgra.FromBgr ((byte) c.B, (byte) c.G, (byte) c.R);
	}

	public override readonly string ToString ()
		=> $"({Hue:2}, {Sat:2}, {Val:2})";

	public override int GetHashCode ()
		=> ((int)Hue + ((int)Sat << 8) + ((int)Val << 16)).GetHashCode ();
}
