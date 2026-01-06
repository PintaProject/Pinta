using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

partial class BlendOpTests
{
	private static readonly UserBlendOps.DifferenceBlendOp difference_op;

	private static IEnumerable<TestCaseData> CreateDifferenceIOCases (UserBlendOps.DifferenceBlendOp differenceOp)
	{
		// Semi-transparent over opaque
		yield return new (
			differenceOp,
			ColorBgra.FromBgra (0, 0, 100, 255), // Opaque red
			ColorBgra.FromBgra (0, 100, 0, 128), // 50% transparent green
			ColorBgra.FromBgra (0, 100, 100, 255));

		// Semi-transparent over semi-transparent
		yield return new (
			differenceOp,
			ColorBgra.FromBgra (0, 0, 100, 128), // semi-transparent red
			ColorBgra.FromBgra (0, 100, 0, 128), // semi-transparent green
			ColorBgra.FromBgra (0, 100, 100, 192));

		// Opaque gray over opaque lighter gray
		yield return new (
			differenceOp,
			ColorBgra.FromBgra (192, 192, 192, 255), // Opaque light gray
			ColorBgra.FromBgra (128, 128, 128, 255), // Opaque gray
			ColorBgra.FromBgra (64, 64, 64, 255));

		// Blending with opaque white should, in technical terms, invert the backdrop
		yield return new (
			differenceOp,
			ColorBgra.FromBgra (50, 150, 200, 255), // Some solid color
			ColorBgra.White, // Opaque white
			ColorBgra.FromBgra (205, 105, 55, 255));

		// Blending with opaque black should result in the other color
		yield return new (
			differenceOp,
			ColorBgra.FromBgra (50, 150, 200, 255), // Some solid color
			ColorBgra.Black, // Opaque black
			ColorBgra.FromBgra (50, 150, 200, 255));

		// Transparent layer on top (should be identity)
		yield return new (
			differenceOp,
			ColorBgra.Cyan,
			ColorBgra.Transparent,
			ColorBgra.Cyan);

		// Transparent layer on bottom (should be identity)
		yield return new (
			differenceOp,
			ColorBgra.Transparent,
			ColorBgra.Cyan,
			ColorBgra.Cyan);

		// --- Special Cases

		// Blending a semi-transparent color with opaque white
		yield return new (
			differenceOp,
			ColorBgra.FromBgra (50, 150, 200, 180), // Some semi-transparent color
			ColorBgra.White, // Opaque white
			ColorBgra.FromBgra (205, 105, 95, 255));

		// Blending a semi-transparent color with opaque black
		yield return new (
			differenceOp,
			ColorBgra.FromBgra (50, 150, 200, 180), // Some semi-transparent color
			ColorBgra.Black, // Opaque black
			ColorBgra.FromBgra (50, 150, 200, 255));

		// Transparent layer on top (should be identity)
		yield return new (
			differenceOp,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.Transparent,
			ColorBgra.FromBgra (50, 150, 200, 180));

		// Transparent layer on bottom (should be identity)
		yield return new (
			differenceOp,
			ColorBgra.Transparent,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.FromBgra (50, 150, 200, 180));
	}
}
