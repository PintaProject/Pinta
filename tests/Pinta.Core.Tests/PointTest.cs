using NUnit.Framework;

namespace Pinta.Core;

[TestFixture]
internal sealed class PointTest
{
	[Test]
	public void Generic_DoubleToInt_BehavesSameAsNonGeneric ()
	{
		double d = 1.4;
		int i = (int) d;
		Coordinate<double> original = new (d, d);
		Point<int> expected = new (i, i);
		Point<int> converted = original.ToPoint<int> ();
		Assert.That (converted, Is.EqualTo (expected));
	}

	[Test]
	public void Generic_IntToDouble_CastBehavesAsExpected ()
	{
		int i = 1;
		double d = i;
		Point<int> original = new (i, i);
		Coordinate<double> expected = new (d, d);
		Coordinate<double> converted = original.ToCoordinate<double> ();
		Assert.That (converted, Is.EqualTo (expected));
	}
}
