using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

partial class BlendOpTests
{
	private static IEnumerable<TestCaseData> NamingTests (UserBlendOps.NormalBlendOp normalOp)
	{
		yield return new (normalOp, "Normal");
	}
}
