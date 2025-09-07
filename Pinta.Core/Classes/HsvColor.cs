/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;

namespace Pinta.Core;

/// <summary>
/// Adapted from:
/// "A Primer on Building a Color Picker User Control with GDI+ in Visual Basic .NET or C#"
/// http://www.msdnaa.net/Resources/display.aspx?ResID=2460
/// </summary>
public readonly struct HsvColor : IColor<HsvColor>
{
	public double Hue { get; init; } // 0-360
	public double Sat { get; init; } // 0-1
	public double Val { get; init; } // 0-1

	public static HsvColor Black => new (0, 0, 0);
	public static HsvColor Red => new (0, 1, 1);
	public static HsvColor Green => new (120, 1, 1);
	public static HsvColor Blue => new (240, 1, 1);
	public static HsvColor Yellow => new (60, 1, 1);
	public static HsvColor Magenta => new (300, 1, 1);
	public static HsvColor Cyan => new (180, 1, 1);
	public static HsvColor White => new (0, 0, 1);

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
	public Color ToColor (double alpha = 1) => Color.FromHsv (this, alpha);
	public static HsvColor FromBgra (ColorBgra c) => c.ToCairoColor ().ToHsv ();
	public ColorBgra ToBgra () => ToColor ().ToColorBgra ();


	public override readonly string ToString ()
		=> $"({Hue:F2}, {Sat:F2}, {Val:F2})";

	public override int GetHashCode ()
		=> HashCode.Combine (Hue, Sat, Val);
}
