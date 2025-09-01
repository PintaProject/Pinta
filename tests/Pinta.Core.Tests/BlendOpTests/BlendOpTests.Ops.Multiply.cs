using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

partial class BlendOpTests
{
	private static readonly UserBlendOps.MultiplyBlendOp multiply_op;

	private static IEnumerable<TestCaseData> CreateMultiplyIOCases (UserBlendOps.MultiplyBlendOp multiplyOp)
	{
		// Semi-transparent over opaque
		yield return new (
			multiply_op,
			ColorBgra.FromBgra (0, 0, 100, 255), // Opaque red
			ColorBgra.FromBgra (0, 100, 0, 128), // 50% transparent green
			ColorBgra.FromBgra (0, 0, 50, 255));

		// Semi-transparent over semi-transparent
		yield return new (
			multiply_op,
			ColorBgra.FromBgra (0, 0, 100, 128), // semi-transparent red
			ColorBgra.FromBgra (0, 100, 0, 128), // semi-transparent green
			ColorBgra.FromBgra (0, 50, 50, 192));

		// Opaque gray over opaque gray
		yield return new (
			multiply_op,
			ColorBgra.FromBgra (128, 128, 128, 255), // Opaque gray
			ColorBgra.FromBgra (128, 128, 128, 255), // Opaque gray
			ColorBgra.FromBgra (64, 64, 64, 255));

		// --- Cases including invalid colors

		// Multiplying with opaque white
		yield return new (
			multiply_op,
			ColorBgra.FromBgra (50, 150, 200, 180), // Some semi-transparent color
			ColorBgra.White, // Opaque white
			ColorBgra.FromBgra (125, 225, 255, 255));

		// Multiplying with opaque black
		yield return new (
			multiply_op,
			ColorBgra.FromBgra (50, 150, 200, 180), // Some semi-transparent color
			ColorBgra.Black, // Opaque black
			ColorBgra.Black);

		// Transparent layer on top (should be identity)
		yield return new (
			multiply_op,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.Transparent,
			ColorBgra.FromBgra (50, 150, 200, 180));

		// Transparent layer on bottom (should be identity)
		yield return new (
			multiply_op,
			ColorBgra.Transparent,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.FromBgra (50, 150, 200, 180));
	}
}
