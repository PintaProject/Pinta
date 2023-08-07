using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class OtherExtensionsTest
{
	[Test]
	public void ToReadOnlyCollection_SecondInvocationReturnsSelf ()
	{
		var source = Enumerable.Range (0, 10);
		var materialized1 = source.ToReadOnlyCollection ();
		var materialized2 = materialized1.ToReadOnlyCollection ();
		Assert.AreSame (materialized1, materialized2);
	}

	[TestCaseSource (nameof (create_polygon_set_arguments_for_empty))]
	public void EmptyStencilReturnsEmptyPolygonSet (RectangleD bounds, int translateX, int translateY)
	{
		var bitmask = new BitMask (0, 0);
		var polygonSet = bitmask.CreatePolygonSet (bounds, translateX, translateY);
		Assert.Zero (polygonSet.Count);
	}

	private static readonly IReadOnlyList<TestCaseData> create_polygon_set_arguments_for_empty = new TestCaseData[] {
		new (new RectangleD(0, 0, 1, 1), 1, 1),
	};
}
