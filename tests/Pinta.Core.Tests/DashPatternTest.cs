using Cairo;
using NUnit.Framework;

namespace Pinta.Core.Tests
{
	[TestFixture]
	class DashPatternTest
	{
		[TestCase (LineCap.Butt, "", new double[] { }, 0.0)]
		[TestCase (LineCap.Butt, "-", new[] { 3.0, 0.0 }, 0.0)]
		[TestCase (LineCap.Butt, " ", new double[] { }, 0.0)]
		[TestCase (LineCap.Butt, " -", new[] { 3.0, 3.0 }, 3.0)]
		[TestCase (LineCap.Butt, "- -", new[] { 3.0, 3.0, 3.0, 0.0 }, 0.0)]
		[TestCase (LineCap.Butt, "-- ", new[] { 6.0, 3.0 }, 0.0)]
		[TestCase (LineCap.Butt, " --", new[] { 6.0, 3.0 }, 6.0)]
		[TestCase (LineCap.Butt, "  -", new[] { 3.0, 6.0 }, 3.0)]
		[TestCase (LineCap.Butt, "$ !-", new[] { 3.0, 9.0 }, 3.0)]
		[TestCase (LineCap.Butt, " - --", new[] { 3.0, 3.0, 6.0, 3.0 }, 12.0)]
		[TestCase (LineCap.Butt, " - - --------", new[] { 3.0, 3.0, 3.0, 3.0, 24.0, 3.0 }, 36.0)]

		[TestCase (LineCap.Square, "", new double[] { }, 0.0)]
		[TestCase (LineCap.Square, "-", new[] { 1.0, 3.0 }, 0.0)]
		[TestCase (LineCap.Square, " ", new double[] { }, 0.0)]
		[TestCase (LineCap.Square, " -", new[] { 1.0, 6.0 }, 2.5)]
		[TestCase (LineCap.Square, "- -", new[] { 1.0, 6.0, 1.0, 3.0 }, 0.0)]
		[TestCase (LineCap.Square, "-- ", new[] { 3.0, 6.0 }, 0.0)]
		[TestCase (LineCap.Square, " --", new[] { 3.0, 6.0 }, 4.5)]
		[TestCase (LineCap.Square, "  -", new[] { 1.0, 9.0 }, 2.5)]
		[TestCase (LineCap.Square, "$ !-", new[] { 1.0, 12.0 }, 2.5)]
		[TestCase (LineCap.Square, " - --", new[] { 1.0, 6.0, 3.0, 6.0 }, 11.5)]
		[TestCase (LineCap.Square, " - - --------", new[] { 1.0, 6.0, 1.0, 6.0, 21.0, 6.0 }, 36.5)]
		public void CreateDashPattern (Cairo.LineCap line_cap, string pattern, double[] expected_dashes, double expected_offset)
		{
			CairoExtensions.CreateDashPattern (pattern, 3.0, line_cap, out var dashes, out var offset);
			Assert.AreEqual (expected_dashes, dashes);
			Assert.AreEqual (expected_offset, offset);
		}
	}
}
