using System;

namespace Pinta.Core;

/// <summary>
/// A 3x2 matrix with double precision, similar to the single precision System.Numerics.Matrix3x2.
/// This can represent an affine transform for 2D coordinates.
/// Note that this uses the row vector convention, e.g. transformation of a vector is v * M.
/// </summary>
public readonly record struct Matrix3x2D (
	double M11, double M12,
	double M21, double M22,
	double M31, double M32)
{
	public static Matrix3x2D CreateRotation (RadiansAngle theta)
	{
		double radians = theta.Radians;
		double sin = Math.Sin (radians);
		double cos = Math.Cos (radians);
		return new (
			cos, sin,
			-sin, cos,
			0, 0);
	}
}
