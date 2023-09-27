/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace Pinta.Core;

/// <summary>
/// Encapsulates functionality for zooming/scaling coordinates.
/// Includes methods for Size[F]'s, Point[F]'s, Rectangle[F]'s,
/// and various scalars
/// </summary>
public struct ScaleFactor
{
	private readonly int denominator;
	private readonly int numerator;

	public double Ratio { get; }

	public static readonly ScaleFactor OneToOne = new (1, 1);
	public static readonly ScaleFactor MinValue = new (1, 100);
	public static readonly ScaleFactor MaxValue = new (32, 1);

	private void Clamp () // TODO: Eventually remove, as this prevents the struct from being truly `readonly`
	{
		if (this < MinValue)
			this = MinValue;
		else if (this > MaxValue)
			this = MaxValue;
	}

	public static bool operator < (ScaleFactor lhs, ScaleFactor rhs) =>
		(lhs.numerator * rhs.denominator) < (rhs.numerator * lhs.denominator);

	public static bool operator > (ScaleFactor lhs, ScaleFactor rhs) =>
		(lhs.numerator * rhs.denominator) > (rhs.numerator * lhs.denominator);

	public readonly int ScaleScalar (int x) =>
		(int) (((long) x * numerator) / denominator);

	public readonly int UnscaleScalar (int x) =>
		(int) (((long) x * denominator) / numerator);

	public readonly double ScaleScalar (double x) =>
		x * numerator / denominator;

	public readonly double UnscaleScalar (double x) =>
		x * denominator / numerator;

	public readonly PointD ScalePoint (PointD p) =>
		new (ScaleScalar (p.X), ScaleScalar (p.Y));

	public readonly PointD UnscalePoint (PointD p) =>
		new (UnscaleScalar (p.X), UnscaleScalar (p.Y));

	public ScaleFactor (int numerator, int denominator)
	{
		if (denominator <= 0)
			throw new ArgumentOutOfRangeException (nameof (denominator), "must be greater than 0(denominator = " + denominator + ")");

		if (numerator < 0)
			throw new ArgumentOutOfRangeException (nameof (numerator), "must be greater than 0(numerator = " + numerator + ")");

		this.numerator = numerator;
		this.denominator = denominator;
		Ratio = numerator / (double) denominator;
		this.Clamp ();
	}
}
