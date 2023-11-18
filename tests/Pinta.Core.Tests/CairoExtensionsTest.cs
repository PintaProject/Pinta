using System;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class CairoExtensionsTest
{
	[Test]
	public void GetMatrixComponents ()
	{
		var m = CairoExtensions.CreateIdentityMatrix ();
		m.Translate (2, 3);
		m.Scale (0.6, 0.8);

		m.GetComponents (out var xx, out var yx, out var xy, out var yy, out var x0, out var y0);
		Assert.AreEqual (0.6, xx);
		Assert.AreEqual (0, yx);
		Assert.AreEqual (0, xy);
		Assert.AreEqual (0.8, yy);
		Assert.AreEqual (2, x0);
		Assert.AreEqual (3, y0);
	}

	[Test]
	public void Decompose ()
	{
		var m = CairoExtensions.CreateIdentityMatrix ();
		m.Translate (2, 3);
		m.Rotate (Math.PI / 4);
		m.Scale (0.6, 0.8);

		m.Decompose (out double sx, out double sy, out double r, out double tx, out double ty);
		Assert.AreEqual (0.6, sx);
		Assert.AreEqual (0.8, sy);
		Assert.AreEqual (Math.PI / 4, r);
		Assert.AreEqual (2, tx);
		Assert.AreEqual (3, ty);
	}
}

