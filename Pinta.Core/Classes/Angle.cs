using System;

namespace Pinta.Core.Classes;

public readonly struct Angle
{
	public double Magnitude { get; }
	public AngleUnit UnitType { get; }

	private Angle (double magnitude, AngleUnit unitType)
	{
		Magnitude = magnitude;
		UnitType = unitType;
	}

	private const double MAX_RADIANS = Math.PI * 2;
	private const double MAX_DEGREES = 360;
	private const double MAX_REVOLUTIONS = 1;

	public static Angle Radians (double radians)
	{
		return radians switch {
			>= 0 => new Angle (radians % MAX_RADIANS, AngleUnit.Radian),
			_ => new Angle (MAX_RADIANS - (radians % MAX_RADIANS), AngleUnit.Radian)
		};
	}

	public static Angle Degrees (double degrees)
	{
		return degrees switch {
			>= 0 => new Angle (degrees % MAX_DEGREES, AngleUnit.Degree),
			_ => new Angle (MAX_DEGREES - (degrees % MAX_DEGREES), AngleUnit.Degree)
		};
	}

	public static Angle Revolutions (double revolutions)
	{
		return revolutions switch {
			>= 0 => new Angle (revolutions % MAX_REVOLUTIONS, AngleUnit.Revolution),
			_ => new Angle (MAX_REVOLUTIONS - (revolutions % MAX_REVOLUTIONS), AngleUnit.Revolution)
		};
	}

	public static Angle operator + (Angle augend, Angle addend)
	{
		if (augend.UnitType == addend.UnitType) {
			return new Angle (augend.Magnitude + addend.Magnitude, augend.UnitType);
		} else {
			var normalizedAddendMagnitude = ChangeUnit (addend.Magnitude, addend.UnitType, augend.UnitType);
			return new Angle (augend.Magnitude + normalizedAddendMagnitude, augend.UnitType);
		}
	}

	public static Angle operator - (Angle minuend, Angle subtrahend)
	{
		if (minuend.UnitType == subtrahend.UnitType) {
			return new Angle (minuend.Magnitude - subtrahend.Magnitude, minuend.UnitType);
		} else {
			var normalizedSubtrahendMagnitude = ChangeUnit (subtrahend.Magnitude, subtrahend.UnitType, minuend.UnitType);
			return new Angle (minuend.Magnitude - normalizedSubtrahendMagnitude, minuend.UnitType);
		}
	}

	const double RADIANS_TO_DEGREES = 180d / Math.PI;
	const double DEGREES_TO_RADIANS = Math.PI / 180d;

	const double REVOLUTIONS_TO_RADIANS = Math.PI * 2d;
	const double RADIANS_TO_REVOLUTIONS = 1d / (Math.PI * 2);

	const double REVOLUTIONS_TO_DEGREES = 360d;
	const double DEGREES_TO_REVOLUTIONS = 1d / 360d;

	private static double ChangeUnit (double magnitude, AngleUnit originalUnit, AngleUnit destinationUnit)
	{
		if (originalUnit == destinationUnit) return magnitude;
		return originalUnit switch {
			AngleUnit.Radian => destinationUnit switch {
				AngleUnit.Degree => magnitude * RADIANS_TO_DEGREES,
				AngleUnit.Revolution => magnitude * RADIANS_TO_REVOLUTIONS,
				_ => throw new NotSupportedException ()
			},
			AngleUnit.Degree => destinationUnit switch {
				AngleUnit.Radian => magnitude * DEGREES_TO_RADIANS,
				AngleUnit.Revolution => magnitude * DEGREES_TO_REVOLUTIONS,
				_ => throw new NotSupportedException ()
			},
			AngleUnit.Revolution => destinationUnit switch {
				AngleUnit.Radian => magnitude * REVOLUTIONS_TO_RADIANS,
				AngleUnit.Degree => magnitude * REVOLUTIONS_TO_DEGREES,
				_ => throw new NotSupportedException ()
			},
			_ => throw new NotSupportedException (),
		};
	}
}

public enum AngleUnit
{
	Radian,
	Degree,
	Revolution,
}
