using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

partial class BlendOpTests
{
	private static IEnumerable<TestCaseData> VisualTests (UserBlendOps.NormalBlendOp normalOp)
	{
		yield return new (normalOp, "visual_blended_normal.png");
	}
}
