using System;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class GradientTests
{
	[TestCase (0, 0)]
	[TestCase (1, 0)]
	public void Constructor_Rejects_Inconsistent_Bounds (double minPosition, double maxPosition)
	{
		Assert.Throws<ArgumentException> (() => ColorMapping.Gradient (minPosition, maxPosition));
	}

	[TestCase (0, 1)]
	[TestCase (-1, 0)]
	[TestCase (-1, 1)]
	[TestCase (1, 2)]
	public void Constructor_Accepts_Consistent_Bounds (double minPosition, double maxPosition)
	{
		Assert.DoesNotThrow (() => ColorMapping.Gradient (minPosition, maxPosition));
	}
}
