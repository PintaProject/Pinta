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
		PointF<double> original = new (d, d);
		PointI<int> expected = new (i, i);
		PointI<int> converted = original.ToInteger<int> ();
		Assert.That (converted, Is.EqualTo (expected));
	}

	[Test]
	public void Generic_IntToDouble_CastBehavesAsExpected ()
	{
		int i = 1;
		double d = i;
		PointI<int> original = new (i, i);
		PointF<double> expected = new (d, d);
		PointF<double> converted = original.ToFloatingPoint<double> ();
		Assert.That (converted, Is.EqualTo (expected));
	}
}
