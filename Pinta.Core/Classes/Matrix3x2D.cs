using System;

namespace Pinta.Core;

public readonly record struct Matrix3x2D (
	double A11, double A12,
	double A21, double A22,
	double A31, double A32)
{
	public static Matrix3x2D CreateRotation (RadiansAngle theta)
	{
		double radians = theta.Radians;
		double sin = Math.Sin (radians);
		double cos = Math.Cos (radians);
		return new (
			cos, -sin,
			sin, cos,
			0, 0);
	}
}
