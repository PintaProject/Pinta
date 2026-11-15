using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class UnaryPixelOpsTests
{
	[Test]
	public void LuminosityCurve_LeavesTransparentPixelUnchanged ()
	{
		UnaryPixelOps.LuminosityCurve curve = new ();       
		// Raise the left end of the curve       
		curve.Curve[0] = 100;

		ColorBgra input = ColorBgra.Transparent;
		ColorBgra result = curve.Apply (input);

		Assert.That (result, Is.EqualTo (input));
	}
}
