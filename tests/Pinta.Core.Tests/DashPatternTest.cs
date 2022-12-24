using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Pinta.Core;

namespace Pinta.Core.Tests
{
	[TestFixture]
	class DashPatternTest
	{
		[TestCase ("", new double[] { }, 0.0)]
		[TestCase ("-", new[] { 3.0, 0.0 }, 0.0)]
		[TestCase (" ", new double[] { }, 0.0)]
		[TestCase (" -", new[] { 3.0, 3.0 }, 3.0)]
		[TestCase ("- -", new[] { 3.0, 3.0, 3.0, 0.0 }, 0.0)]
		[TestCase ("-- ", new[] { 6.0, 3.0 }, 0.0)]
		[TestCase (" --", new[] { 6.0, 3.0 }, 6.0)]
		[TestCase ("  -", new[] { 3.0, 6.0 }, 3.0)]
		[TestCase ("$ !-", new[] { 3.0, 9.0 }, 3.0)]
		[TestCase (" - --", new[] { 3.0, 3.0, 6.0, 3.0 }, 12.0)]
		[TestCase (" - - --------", new[] { 3.0, 3.0, 3.0, 3.0, 24.0, 3.0 }, 36.0)]
		public void CreateDashPattern (string pattern, double[] expected_dashes, double expected_offset)
		{
			CairoExtensions.CreateDashPattern (pattern, 3.0, out var dashes, out var offset);
			Assert.AreEqual (dashes, expected_dashes);
			Assert.AreEqual (offset, expected_offset);
		}
	}
}
