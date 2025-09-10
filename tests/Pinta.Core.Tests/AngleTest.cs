using System;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class AngleTest
{
	[TestCase (1d, 1d)]
	[TestCase (Math.PI, Math.PI)]
	[TestCase (Math.PI * 2d, 0d)]
	[TestCase (-Math.PI, Math.PI)]
	[TestCase (-(Math.PI * 2d), 0d)]
	[TestCase (0d, 0d)]
	public void RadiansAngle_Creation (double constructorArgument, double expectedPropertyValue)
	{
		RadiansAngle angle = new (constructorArgument);
		Assert.That (expectedPropertyValue, Is.EqualTo (angle.Radians));
	}

	[TestCase (1d, 1d)]
	[TestCase (180d, 180d)]
	[TestCase (360d, 0)]
	[TestCase (-180d, 180d)]
	[TestCase (-360d, 0d)]
	[TestCase (0d, 0d)]
	public void DegreesAngle_Creation (double constructorArgument, double expectedPropertyValue)
	{
		DegreesAngle angle = new (constructorArgument);
		Assert.That (expectedPropertyValue, Is.EqualTo (angle.Degrees));
	}

	[TestCase (0.5d, 0.5d)]
	[TestCase (1d, 0d)]
	[TestCase (-0.5d, 0.5d)]
	[TestCase (-1d, 0d)]
	[TestCase (0d, 0d)]
	public void RevolutionsAngle_Creation (double constructorArgument, double expectedPropertyValue)
	{
		RevolutionsAngle angle = new (constructorArgument);
		Assert.That (expectedPropertyValue, Is.EqualTo (angle.Revolutions));
	}

	[TestCase (double.NaN)]
	[TestCase (double.PositiveInfinity)]
	[TestCase (double.NegativeInfinity)]
	public void Constructor_Throws_If_Not_Finite (double constructorArgument)
	{
		Assert.Throws<ArgumentOutOfRangeException> (() => new DegreesAngle (constructorArgument));
		Assert.Throws<ArgumentOutOfRangeException> (() => new RadiansAngle (constructorArgument));
		Assert.Throws<ArgumentOutOfRangeException> (() => new RevolutionsAngle (constructorArgument));
	}

	[TestCase (1d, 1d, 2d)]
	[TestCase (Math.PI, Math.PI, 0)]
	[TestCase (Math.PI * 1.5d, Math.PI * 1.5d, Math.PI)]
	[TestCase (0d, 0d, 0d)]
	[TestCase (Math.PI * 2d, Math.PI * 2d, 0d)]
	[TestCase (-Math.PI, Math.PI * 2d, Math.PI)]
	public void RadiansAngle_Addition (double leftArgument, double rightArgument, double expectedResult)
	{
		RadiansAngle left = new (leftArgument);
		RadiansAngle right = new (rightArgument);
		var result = left + right;
		Assert.That (expectedResult, Is.EqualTo (result.Radians));
	}

	[TestCase (1d, 1d, 2d)]
	[TestCase (180d, 180d, 0d)]
	[TestCase (270d, 270d, 180d)]
	[TestCase (0d, 0d, 0d)]
	[TestCase (360d, 360d, 0d)]
	[TestCase (-180d, 360d, 180d)]
	public void DegreesAngle_Addition (double leftArgument, double rightArgument, double expectedResult)
	{
		DegreesAngle left = new (leftArgument);
		DegreesAngle right = new (rightArgument);
		var result = left + right;
		Assert.That (expectedResult, Is.EqualTo (result.Degrees));
	}

	[TestCase (0.125d, 0.125d, 0.25d)]
	[TestCase (0.5d, 0.5d, 0d)]
	[TestCase (0.75d, 0.75d, 0.5d)]
	[TestCase (0d, 0d, 0d)]
	[TestCase (1d, 1d, 0d)]
	[TestCase (-0.5d, 1d, 0.5d)]
	public void RevolutionsAngle_Addition (double leftArgument, double rightArgument, double expectedResult)
	{
		RevolutionsAngle left = new (leftArgument);
		RevolutionsAngle right = new (rightArgument);
		var result = left + right;
		Assert.That (expectedResult, Is.EqualTo (result.Revolutions));
	}

	[TestCase (Math.PI * 0.5, Math.PI, Math.PI * 1.5)]
	[TestCase (0d, Math.PI, Math.PI)]
	[TestCase (0d, Math.PI * 2, 0d)]
	[TestCase (Math.PI, Math.PI * 0.5, Math.PI * 0.5)]
	public void RadiansAngle_Subtraction (double leftArgument, double rightArgument, double expectedResult)
	{
		RadiansAngle left = new (leftArgument);
		RadiansAngle right = new (rightArgument);
		var result = left - right;
		Assert.That (expectedResult, Is.EqualTo (result.Radians));
	}

	[TestCase (90d, 180d, 270d)]
	[TestCase (0d, 180d, 180d)]
	[TestCase (0d, 360d, 0d)]
	[TestCase (180d, 90d, 90d)]
	public void DegreesAngle_Subtraction (double leftArgument, double rightArgument, double expectedResult)
	{
		DegreesAngle left = new (leftArgument);
		DegreesAngle right = new (rightArgument);
		var result = left - right;
		Assert.That (expectedResult, Is.EqualTo (result.Degrees));
	}

	[TestCase (0.25d, 0.5d, 0.75d)]
	[TestCase (0d, 0.5d, 0.5d)]
	[TestCase (0d, 1d, 0d)]
	[TestCase (0.5d, 0.25d, 0.25d)]
	public void RevolutionsAngle_Subtraction (double leftArgument, double rightArgument, double expectedResult)
	{
		RevolutionsAngle left = new (leftArgument);
		RevolutionsAngle right = new (rightArgument);
		var result = left - right;
		Assert.That (expectedResult, Is.EqualTo (result.Revolutions));
	}

	[TestCase (0d, 0d)]
	[TestCase (Math.PI * 0.5d, 90d)]
	[TestCase (Math.PI, 180d)]
	[TestCase (Math.PI * 1.5d, 270d)]
	[TestCase (-Math.PI, 180d)]
	public void Radians_To_Degrees (double radians, double expectedDegrees)
	{
		RadiansAngle radiansAngle = new (radians);
		DegreesAngle degreesAngle = radiansAngle.ToDegrees ();
		Assert.That (expectedDegrees, Is.EqualTo (degreesAngle.Degrees));
	}

	[TestCase (0d, 0d)]
	[TestCase (Math.PI * 0.5d, 0.25d)]
	[TestCase (Math.PI, 0.5d)]
	[TestCase (Math.PI * 1.5d, 0.75d)]
	[TestCase (-Math.PI, 0.5d)]
	public void Radians_To_Revolutions (double radians, double expectedRevolutions)
	{
		RadiansAngle radiansAngle = new (radians);
		RevolutionsAngle revolutionsAngle = radiansAngle.ToRevolutions ();
		Assert.That (expectedRevolutions, Is.EqualTo (revolutionsAngle.Revolutions));
	}

	[TestCase (0d, 0d)]
	[TestCase (90d, Math.PI * 0.5d)]
	[TestCase (180d, Math.PI)]
	[TestCase (270d, Math.PI * 1.5d)]
	[TestCase (-180d, Math.PI)]
	public void Degrees_To_Radians (double degrees, double expectedRadians)
	{
		DegreesAngle degreesAngle = new (degrees);
		RadiansAngle radiansAngle = degreesAngle.ToRadians ();
		Assert.That (expectedRadians, Is.EqualTo (radiansAngle.Radians));
	}

	[TestCase (0d, 0d)]
	[TestCase (90d, 0.25d)]
	[TestCase (180d, 0.5d)]
	[TestCase (270d, 0.75d)]
	[TestCase (-180d, 0.5)]
	public void Degrees_To_Revolutions (double degrees, double expectedRevolutions)
	{
		DegreesAngle degreesAngle = new (degrees);
		RevolutionsAngle revolutionsAngle = degreesAngle.ToRevolutions ();
		Assert.That (expectedRevolutions, Is.EqualTo (revolutionsAngle.Revolutions));
	}

	[TestCase (0d, 0d)]
	[TestCase (0.25d, Math.PI * 0.5d)]
	[TestCase (0.5d, Math.PI)]
	[TestCase (0.75d, Math.PI * 1.5d)]
	[TestCase (-0.5d, Math.PI)]
	public void Revolutions_To_Radians (double revolutions, double expectedRadians)
	{
		RevolutionsAngle revolutionsAngle = new (revolutions);
		RadiansAngle radiansAngle = revolutionsAngle.ToRadians ();
		Assert.That (expectedRadians, Is.EqualTo (radiansAngle.Radians));
	}

	[TestCase (0d, 0d)]
	[TestCase (0.25d, 90d)]
	[TestCase (0.5d, 180d)]
	[TestCase (0.75d, 270d)]
	[TestCase (-0.5, 180d)]
	public void Revolutions_To_Degrees (double revolutions, double expectedDegrees)
	{
		RevolutionsAngle revolutionsAngle = new (revolutions);
		DegreesAngle degreesAngle = revolutionsAngle.ToDegrees ();
		Assert.That (expectedDegrees, Is.EqualTo (degreesAngle.Degrees));
	}
}
