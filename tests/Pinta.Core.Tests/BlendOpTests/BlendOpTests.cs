using System.Collections.Immutable;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed partial class BlendOpTests
{
	private static readonly ImmutableArray<TestCaseData> normal_io_cases;
	private static readonly ImmutableArray<TestCaseData> op_name_cases;
	static BlendOpTests ()
	{
		UserBlendOps.NormalBlendOp normalOp = new ();

		normal_op = normalOp;

		normal_io_cases = [.. CreateNormalIOCases (normalOp)];
		op_name_cases = [.. NamingTests (normalOp)];
	}

	[TestCaseSource (nameof (op_name_cases))]
	public void StaticName_IsNormal (UserBlendOp blendOp, string expectedName)
	{
		Assert.That (blendOp.ToString (), Is.EqualTo (expectedName));
	}

	[TestCaseSource (nameof (normal_io_cases))]
	public void OutputIsExpected (UserBlendOp blendOp, ColorBgra bottom, ColorBgra top, ColorBgra expected)
	{
		ColorBgra result = blendOp.Apply (bottom, top);
		Assert.That (result, Is.EqualTo (expected), $"Colors not blended as expected by {blendOp}");
	}
}
