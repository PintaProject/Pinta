using System.Collections.Immutable;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed partial class BlendOpTests
{
	private static readonly ImmutableArray<TestCaseData> normal_io_cases;
	private static readonly ImmutableArray<TestCaseData> multiply_io_cases;

	private static readonly ImmutableArray<TestCaseData> op_name_cases;
	private static readonly ImmutableArray<TestCaseData> visual_cases;

	static BlendOpTests ()
	{
		UserBlendOps.NormalBlendOp normalOp = new ();
		UserBlendOps.MultiplyBlendOp multiplyOp = new ();

		normal_op = normalOp;
		multiply_op = multiplyOp;

		normal_io_cases = [.. CreateNormalIOCases (normalOp)];
		multiply_io_cases = [.. CreateMultiplyIOCases (multiplyOp)];

		op_name_cases = [.. NamingTests (normalOp, multiplyOp)];
		visual_cases = [.. VisualTests (normalOp, multiplyOp)];
	}

	[TestCaseSource (nameof (op_name_cases))]
	public void StaticName_IsNormal (UserBlendOp blendOp, string expectedName)
	{
		Assert.That (blendOp.ToString (), Is.EqualTo (expectedName));
	}

	[TestCaseSource (nameof (normal_io_cases))]
	[TestCaseSource (nameof (multiply_io_cases))]
	public void Output_Is_Expected (UserBlendOp blendOp, ColorBgra bottom, ColorBgra top, ColorBgra expected)
	{
		ColorBgra result = blendOp.Apply (bottom, top);
		Assert.That (result, Is.EqualTo (expected), $"Colors not blended as expected by {blendOp}");
	}

	[TestCaseSource (nameof (visual_cases))]
	public void Visual_Blending (UserBlendOp blendOp, string nameOutput)
	{
		Utilities.TestBlendOp (blendOp, nameOutput);
	}
}
