using System;

namespace Pinta.Core;

public readonly struct RadiansAngle
{
	public const double MAX_RADIANS = Math.PI * 2;
	public readonly double Radians { get; }

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

	public readonly DegreesAngle ToDegrees () => new (Radians * RADIANS_TO_DEGREES);
	public readonly RevolutionsAngle ToRevolutions () => new (Radians * RADIANS_TO_REVOLUTIONS);

	public static RadiansAngle operator + (RadiansAngle a, RadiansAngle b) => new (a.Radians + b.Radians);
	public static RadiansAngle operator - (RadiansAngle a, RadiansAngle b) => new (a.Radians - b.Radians);
	public static bool operator == (RadiansAngle a, RadiansAngle b) => a.Equals (b);
	public static bool operator != (RadiansAngle a, RadiansAngle b) => !a.Equals (b);
	public static bool operator > (RadiansAngle a, RadiansAngle b) => a.Radians > b.Radians;
	public static bool operator < (RadiansAngle a, RadiansAngle b) => a.Radians < b.Radians;
	public override readonly int GetHashCode () => Radians.GetHashCode ();
	public override readonly bool Equals (object? obj)
	{
		if (obj is not RadiansAngle other) return false;
		return Radians == other.Radians;
	}
}

public readonly struct DegreesAngle
{
	public const double MAX_DEGREES = 360;
	public readonly double Degrees { get; }

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

	public readonly RadiansAngle ToRadians () => new (Degrees * DEGREES_TO_RADIANS);
	public readonly RevolutionsAngle ToRevolutions () => new (Degrees * DEGREES_TO_REVOLUTIONS);

	public static DegreesAngle operator + (DegreesAngle a, DegreesAngle b) => new (a.Degrees + b.Degrees);
	public static DegreesAngle operator - (DegreesAngle a, DegreesAngle b) => new (a.Degrees - b.Degrees);
	public static bool operator == (DegreesAngle a, DegreesAngle b) => a.Equals (b);
	public static bool operator != (DegreesAngle a, DegreesAngle b) => !a.Equals (b);
	public static bool operator > (DegreesAngle a, DegreesAngle b) => a.Degrees > b.Degrees;
	public static bool operator < (DegreesAngle a, DegreesAngle b) => a.Degrees < b.Degrees;
	public override readonly int GetHashCode () => Degrees.GetHashCode ();
	public override readonly bool Equals (object? obj)
	{
		if (obj is not DegreesAngle other) return false;
		return Degrees == other.Degrees;
	}
}

public readonly struct RevolutionsAngle
{
	public const double MAX_REVOLUTIONS = 1;
	public readonly double Revolutions { get; }

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

	public readonly RadiansAngle ToRadians () => new (Revolutions * REVOLUTIONS_TO_RADIANS);
	public readonly DegreesAngle ToDegrees () => new (Revolutions * REVOLUTIONS_TO_DEGREES);

	public static RevolutionsAngle operator + (RevolutionsAngle a, RevolutionsAngle b) => new (a.Revolutions + b.Revolutions);
	public static RevolutionsAngle operator - (RevolutionsAngle a, RevolutionsAngle b) => new (a.Revolutions - b.Revolutions);
	public static bool operator == (RevolutionsAngle a, RevolutionsAngle b) => a.Revolutions == b.Revolutions;
	public static bool operator != (RevolutionsAngle a, RevolutionsAngle b) => a.Revolutions != b.Revolutions;
	public static bool operator > (RevolutionsAngle a, RevolutionsAngle b) => a.Revolutions > b.Revolutions;
	public static bool operator < (RevolutionsAngle a, RevolutionsAngle b) => a.Revolutions < b.Revolutions;
	public override readonly int GetHashCode () => Revolutions.GetHashCode ();
	public override readonly bool Equals (object? obj)
	{
		if (obj is not RevolutionsAngle other) return false;
		return Revolutions == other.Revolutions;
	}
}
