using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

partial class BlendOpTests
{
	private static readonly UserBlendOps.ColorDodgeBlendOp color_dodge_op;

	private static IEnumerable<TestCaseData> CreateColorDodgeIOCases (UserBlendOps.ColorDodgeBlendOp colorDodgeOp)
	{
		// Opaque gray over opaque gray: 100 / (1 - 128/255) = 200.8
		yield return new (
			colorDodgeOp,
			ColorBgra.FromBgra (100, 100, 100, 255), // Opaque gray
			ColorBgra.FromBgra (128, 128, 128, 255), // Opaque gray
			ColorBgra.FromBgra (201, 201, 201, 255));

		// Semi-transparent over opaque
		yield return new (
			colorDodgeOp,
			ColorBgra.FromBgra (100, 100, 100, 255), // Opaque gray
			ColorBgra.FromBgra (64, 64, 64, 128), // semi-transparent gray
			ColorBgra.FromBgra (150, 150, 150, 255));

		// Semi-transparent over semi-transparent
		yield return new (
			colorDodgeOp,
			ColorBgra.FromBgra (50, 50, 50, 128), // semi-transparent gray
			ColorBgra.FromBgra (64, 64, 64, 128), // semi-transparent gray
			ColorBgra.FromBgra (107, 107, 107, 192));

		// Semi-transparent color over opaque color
		yield return new (
			colorDodgeOp,
			ColorBgra.FromBgra (20, 60, 120, 255), // Opaque color
			ColorBgra.FromBgra (30, 10, 40, 96), // semi-transparent color
			ColorBgra.FromBgra (23, 63, 152, 255));

		// Dodging black results in black
		yield return new (
			colorDodgeOp,
			ColorBgra.Black,
			ColorBgra.FromBgra (200, 200, 200, 255),
			ColorBgra.Black);

		// --- Cases with invalid colors

		// Dodging with opaque white
		yield return new (
			colorDodgeOp,
			ColorBgra.FromBgra (50, 150, 200, 180), // Some semi-transparent color
			ColorBgra.White, // Opaque white
			ColorBgra.White);

		// Dodging with opaque black: color is kept, alpha becomes opaque,
		// and invalid channel (R = 200 > A = 180) is clamped to alpha it had
		yield return new (
			colorDodgeOp,
			ColorBgra.FromBgra (50, 150, 200, 180), // Some semi-transparent color
			ColorBgra.Black, // Opaque black
			ColorBgra.FromBgra (50, 150, 180, 255));

		// Transparent layer on top (should result in non-transparent one)
		yield return new (
			colorDodgeOp,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.Transparent,
			ColorBgra.FromBgra (50, 150, 200, 180));

		// Transparent layer on bottom (should result in non-transparent on)
		yield return new (
			colorDodgeOp,
			ColorBgra.Transparent,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.FromBgra (50, 150, 200, 180));
	}

}
