using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

partial class BlendOpTests
{
	private static IEnumerable<TestCaseData> VisualTests (
		UserBlendOps.NormalBlendOp normalOp,
		UserBlendOps.MultiplyBlendOp multiplyOp,
		UserBlendOps.ScreenBlendOp screenOp)
	{
		yield return new (normalOp, "visual_blended_normal.png");
		yield return new (multiplyOp, "visual_blended_multiply.png");
		yield return new (screenOp, "visual_blended_screen.png");
	}
}
