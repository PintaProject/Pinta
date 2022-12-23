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
		[TestCase ("", ExpectedResult = new double[] { })]
		[TestCase ("-", ExpectedResult = new[] { 3.0, 0.0 })]
		[TestCase (" ", ExpectedResult = new[] { 0.0, 3.0 })]
		[TestCase (" -", ExpectedResult = new[] { 0.0, 3.0, 3.0, 0.0 })]
		[TestCase ("-- ", ExpectedResult = new[] { 6.0, 3.0 })]
		[TestCase (" --", ExpectedResult = new[] { 0.0, 3.0, 6.0, 0.0 })]
		[TestCase ("  -", ExpectedResult = new[] { 0.0, 6.0, 3.0, 0.0 })]
		[TestCase ("$ !-", ExpectedResult = new[] { 0.0, 9.0, 3.0, 0.0 })]
		[TestCase (" - --", ExpectedResult = new[] { 0.0, 3.0, 3.0, 3.0, 6.0, 0.0 })]
		[TestCase (" - - --------", ExpectedResult = new[] { 0.0, 3.0, 3.0, 3.0, 3.0, 3.0, 24.0, 0.0 })]
		public double[] CreateDashPattern (string pattern)
		{
			return CairoExtensions.CreateDashPattern (pattern, 3.0);
		}
	}
}
