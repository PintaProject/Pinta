using System;
using System.Collections.Immutable;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed partial class BlendOpTests
{
	private static readonly ImmutableArray<TestCaseData> normal_io_cases;
	private static readonly ImmutableArray<TestCaseData> op_name_cases;
	private static readonly ImmutableArray<UserBlendOp> blend_ops;
	private static readonly ImmutableArray<ColorBgra> invalid_inputs;
	static BlendOpTests ()
	{
		UserBlendOps.NormalBlendOp normalOp = new ();

		normal_op = normalOp;

		normal_io_cases = [.. CreateNormalIOCases (normalOp)];
		op_name_cases = [.. NamingTests (normalOp)];

		blend_ops = [normalOp];

		invalid_inputs = [.. GenerateInvalidColors ()];
	}

	[Test]
	public void InputValidation (
		[ValueSource (nameof (blend_ops))] UserBlendOp blendOp,
		[ValueSource (nameof (invalid_inputs))] ColorBgra invalid)
	{
		ColorBgra valid = ColorBgra.Zero;
		Assert.DoesNotThrow (() => blendOp.Apply (valid, valid), "Rejects valid inputs");
		Assert.Throws<ArgumentException> (() => blendOp.Apply (invalid, valid), "Does not reject invalid lhs");
		Assert.Throws<ArgumentException> (() => blendOp.Apply (valid, invalid), "Does not reject invalid rhs");
		Assert.Throws<ArgumentException> (() => blendOp.Apply (invalid, invalid), "Does not reject invalid inputs");
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
