using System;

namespace Pinta.Core.Classes;

public readonly struct RadiansAngle
{
	private const double MAX_RADIANS = Math.PI * 2;
	public double Radians { get; }

	public RadiansAngle (double radians)
	{
		Radians = radians switch {
			0 => 0,
			>= 0 => radians % MAX_RADIANS,
			_ => (MAX_RADIANS + (radians % MAX_RADIANS)) % MAX_RADIANS
		};
	}

	const double RADIANS_TO_DEGREES = 180d / Math.PI;
	const double RADIANS_TO_REVOLUTIONS = 1d / (Math.PI * 2);

	public DegreesAngle ToDegrees () => new (Radians * RADIANS_TO_DEGREES);
	public RevolutionsAngle ToRevolutions () => new (Radians * RADIANS_TO_REVOLUTIONS);

	public static RadiansAngle operator + (RadiansAngle a, RadiansAngle b) => new (a.Radians + b.Radians);
	public static RadiansAngle operator - (RadiansAngle a, RadiansAngle b) => new (a.Radians - b.Radians);
}

public readonly struct DegreesAngle
{
	private const double MAX_DEGREES = 360;
	public double Degrees { get; }

	public DegreesAngle (double degrees)
	{
		Degrees = degrees switch {
			0 => 0,
			>= 0 => degrees % MAX_DEGREES,
			_ => (MAX_DEGREES + (degrees % MAX_DEGREES)) % MAX_DEGREES
		};
	}

	const double DEGREES_TO_RADIANS = Math.PI / 180d;
	const double DEGREES_TO_REVOLUTIONS = 1d / 360d;

	public RadiansAngle ToRadians () => new (Degrees * DEGREES_TO_RADIANS);
	public RevolutionsAngle ToRevolutions () => new (Degrees * DEGREES_TO_REVOLUTIONS);

	public static DegreesAngle operator + (DegreesAngle a, DegreesAngle b) => new (a.Degrees + b.Degrees);
	public static DegreesAngle operator - (DegreesAngle a, DegreesAngle b) => new (a.Degrees - b.Degrees);
}

public readonly struct RevolutionsAngle
{
	private const double MAX_REVOLUTIONS = 1;
	public double Revolutions { get; }

	public RevolutionsAngle (double revolutions)
	{
		Revolutions = revolutions switch {
			0 => 0,
			>= 0 => revolutions % MAX_REVOLUTIONS,
			_ => (MAX_REVOLUTIONS + (revolutions % MAX_REVOLUTIONS)) % MAX_REVOLUTIONS
		};
	}

	const double REVOLUTIONS_TO_RADIANS = Math.PI * 2d;
	const double REVOLUTIONS_TO_DEGREES = 360d;

	public RadiansAngle ToRadians () => new (Revolutions * REVOLUTIONS_TO_RADIANS);
	public DegreesAngle ToDegrees () => new (Revolutions * REVOLUTIONS_TO_DEGREES);

	public static RevolutionsAngle operator + (RevolutionsAngle a, RevolutionsAngle b) => new (a.Revolutions + b.Revolutions);
	public static RevolutionsAngle operator - (RevolutionsAngle a, RevolutionsAngle b) => new (a.Revolutions - b.Revolutions);
}
