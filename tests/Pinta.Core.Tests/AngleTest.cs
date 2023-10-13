using System;
using NUnit.Framework;
using Pinta.Core.Classes;

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
		Assert.AreEqual (angle.Radians, expectedPropertyValue);
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
		Assert.AreEqual (angle.Degrees, expectedPropertyValue);
	}

	[TestCase (0.5d, 0.5d)]
	[TestCase (1d, 0d)]
	[TestCase (-0.5d, 0.5d)]
	[TestCase (-1d, 0d)]
	[TestCase (0d, 0d)]
	public void RevolutionsAngle_Creation (double constructorArgument, double expectedPropertyValue)
	{
		RevolutionsAngle angle = new (constructorArgument);
		Assert.AreEqual (angle.Revolutions, expectedPropertyValue);
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
		Assert.AreEqual (result.Radians, expectedResult);
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
		Assert.AreEqual (result.Degrees, expectedResult);
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
		Assert.AreEqual (result.Revolutions, expectedResult);
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
		Assert.AreEqual (result.Radians, expectedResult);
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
		Assert.AreEqual (result.Degrees, expectedResult);
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
		Assert.AreEqual (result.Revolutions, expectedResult);
	}
}
