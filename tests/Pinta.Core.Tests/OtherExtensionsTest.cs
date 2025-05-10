using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class OtherExtensionsTest
{
	[TestCaseSource (nameof (create_polygon_set_arguments_for_empty))]
	public void EmptyStencilReturnsEmptyPolygonSet (RectangleD bounds, PointI translateOffset)
	{
		BitMask bitmask = new (0, 0);
		var polygonSet = bitmask.CreatePolygonSet (bounds, translateOffset);
		Assert.That (polygonSet.Count, Is.Zero);
	}

	private static readonly IReadOnlyList<TestCaseData> create_polygon_set_arguments_for_empty = [
		new (new RectangleD(0, 0, 1, 1), new PointI (1, 1)),
	];
}
