using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

partial class BlendOpTests
{
	private static IEnumerable<TestCaseData> NamingTests (
		UserBlendOps.NormalBlendOp normalOp,
		UserBlendOps.MultiplyBlendOp multiplyOp,
		UserBlendOps.ScreenBlendOp screenOp,
		UserBlendOps.DarkenBlendOp darkenOp,
		UserBlendOps.LightenBlendOp lightenOp)
	{
		yield return new (normalOp, "Normal");
		yield return new (multiplyOp, "Multiply");
		yield return new (screenOp, "Screen");
		yield return new (darkenOp, "Darken");
		yield return new (lightenOp, "Lighten");
	}
}
