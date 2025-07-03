using System;

namespace Pinta.Core;

public readonly record struct Matrix2D (
	double A11, double A12,
	double A21, double A22)
{
	public static Matrix2D Rotation (RadiansAngle theta)
	{
		double radians = theta.Radians;
		double sin = Math.Sin (radians);
		double cos = Math.Cos (radians);
		return new (
			cos, -sin,
			sin, cos);
	}
}
