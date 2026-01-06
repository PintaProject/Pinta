using System.Collections.Immutable;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed partial class BlendOpTests
{
	private static readonly ImmutableArray<TestCaseData> normal_io_cases;
	private static readonly ImmutableArray<TestCaseData> multiply_io_cases;
	private static readonly ImmutableArray<TestCaseData> screen_io_cases;
	private static readonly ImmutableArray<TestCaseData> darken_io_cases;
	private static readonly ImmutableArray<TestCaseData> lighten_io_cases;
	private static readonly ImmutableArray<TestCaseData> difference_io_cases;

	private static readonly ImmutableArray<TestCaseData> op_name_cases;
	private static readonly ImmutableArray<TestCaseData> visual_cases;

	static BlendOpTests ()
	{
		UserBlendOps.NormalBlendOp normalOp = new ();
		UserBlendOps.MultiplyBlendOp multiplyOp = new ();
		UserBlendOps.ScreenBlendOp screenOp = new ();
		UserBlendOps.DarkenBlendOp darkenOp = new ();
		UserBlendOps.LightenBlendOp lightenOp = new ();
		UserBlendOps.DifferenceBlendOp differenceOp = new ();

		normal_op = normalOp;
		multiply_op = multiplyOp;
		screen_op = screenOp;
		darken_op = darkenOp;
		lighten_op = lightenOp;
		difference_op = differenceOp;

		normal_io_cases = [.. CreateNormalIOCases (normalOp)];
		multiply_io_cases = [.. CreateMultiplyIOCases (multiplyOp)];
		screen_io_cases = [.. CreateScreenIOCases (screenOp)];
		darken_io_cases = [.. CreateDarkenIOCases (darkenOp)];
		lighten_io_cases = [.. CreateLightenIOCases (lightenOp)];
		difference_io_cases = [.. CreateDifferenceIOCases (differenceOp)];

		op_name_cases = [.. NamingTests (
			normalOp,
			multiplyOp,
			screenOp,
			darkenOp,
			lightenOp,
			differenceOp)];

		visual_cases = [.. VisualTests (
			normalOp,
			multiplyOp,
			screenOp,
			darkenOp,
			lightenOp,
			differenceOp)];
	}

	[TestCaseSource (nameof (op_name_cases))]
	public void StaticName_Is_Expected (UserBlendOp blendOp, string expectedName)
	{
		Assert.That (blendOp.ToString (), Is.EqualTo (expectedName));
	}

	[TestCaseSource (nameof (normal_io_cases))]
	[TestCaseSource (nameof (multiply_io_cases))]
	[TestCaseSource (nameof (screen_io_cases))]
	[TestCaseSource (nameof (darken_io_cases))]
	[TestCaseSource (nameof (lighten_io_cases))]
	[TestCaseSource (nameof (difference_io_cases))]
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
