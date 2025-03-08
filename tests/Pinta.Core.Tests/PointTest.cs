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
		Point<double> original = new (d, d);
		Point<int> expected = new (i, i);
		Point<int> converted = original.Cast<int> ();
		Assert.That (converted, Is.EqualTo (expected));
	}

	[Test]
	public void Generic_IntToDouble_CastBehavesAsExpected ()
	{
		int i = 1;
		double d = i;
		Point<int> original = new (i, i);
		Point<double> expected = new (d, d);
		Point<double> converted = original.Cast<double> ();
		Assert.That (converted, Is.EqualTo (expected));
	}
}
