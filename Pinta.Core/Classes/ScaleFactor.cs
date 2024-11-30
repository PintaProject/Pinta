/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

namespace Pinta.Core;

/// <summary>
/// Encapsulates functionality for zooming/scaling coordinates.
/// Includes methods for Size[F]'s, Point[F]'s, Rectangle[F]'s,
/// and various scalars
/// </summary>
public static class ScaleFactor
{
	public static Fraction<int> OneToOne { get; } = new (1, 1);
	public static Fraction<int> MinValue { get; } = new (1, 100);
	public static Fraction<int> MaxValue { get; } = new (32, 1);

	public static int ScaleScalar (this in Fraction<int> fraction, int x) =>
		(int) (((long) x * fraction.Numerator) / fraction.Denominator);

	public static int UnscaleScalar (this in Fraction<int> fraction, int x) =>
		(int) (((long) x * fraction.Denominator) / fraction.Numerator);

	public static double ScaleScalar (this in Fraction<int> fraction, double x) =>
		x * fraction.Numerator / fraction.Denominator;

	public static double UnscaleScalar (this in Fraction<int> fraction, double x) =>
		x * fraction.Denominator / fraction.Numerator;

	public static PointD ScalePoint (this in Fraction<int> fraction, PointD p) =>
		new (fraction.ScaleScalar (p.X), fraction.ScaleScalar (p.Y));

	public static PointD UnscalePoint (this in Fraction<int> fraction, PointD p) =>
		new (fraction.UnscaleScalar (p.X), fraction.UnscaleScalar (p.Y));

	public static double ComputeRatio (this in Fraction<int> fraction)
		=> fraction.Numerator / (double) fraction.Denominator;

	/// <returns>
	/// Fraction representing the scale factor,
	/// clamped to <see cref="MinValue"/> and <see cref="MaxValue"/>
	/// </returns>
	public static Fraction<int> CreateClamped (int numerator, int denominator)
	{
		Fraction<int> baseFraction = new (numerator, denominator);
		return Mathematics.Clamp (baseFraction, MinValue, MaxValue);
	}
}
