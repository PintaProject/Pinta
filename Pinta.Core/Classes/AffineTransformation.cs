using System;

namespace Pinta.Core;

public readonly record struct AffineTransformation (
	double A11, double A12,
	double A21, double A22,
	double dx, double dy)
{
	public static AffineTransformation CreateRotation (RadiansAngle theta)
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
