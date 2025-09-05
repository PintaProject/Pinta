using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

partial class BlendOpTests
{
	private static readonly UserBlendOps.LightenBlendOp lighten_op;

	private static IEnumerable<TestCaseData> CreateLightenIOCases (UserBlendOps.LightenBlendOp lightenOp)
	{
		// Semi-transparent over opaque
		yield return new (
			lightenOp,
			ColorBgra.FromBgra (0, 0, 100, 255), // Opaque red
			ColorBgra.FromBgra (0, 100, 0, 128), // 50% transparent green
			ColorBgra.FromBgra (0, 100, 100, 255));

		// Semi-transparent over semi-transparent
		yield return new (
			lightenOp,
			ColorBgra.FromBgra (0, 0, 100, 128), // semi-transparent red
			ColorBgra.FromBgra (0, 100, 0, 128), // semi-transparent green
			ColorBgra.FromBgra (0, 100, 100, 192));

		// Opaque gray over opaque darker gray
		yield return new (
			lightenOp,
			ColorBgra.FromBgra (128, 128, 128, 255), // Opaque gray
			ColorBgra.FromBgra (64, 64, 64, 255), // Opaque dark gray
			ColorBgra.FromBgra (128, 128, 128, 255));

		// --- Special Cases

		// Blending with opaque white should result in white
		yield return new (
			lightenOp,
			ColorBgra.FromBgra (50, 150, 200, 180), // Some semi-transparent color
			ColorBgra.White, // Opaque white
			ColorBgra.White);

		// Blending with opaque black should leave the color unchanged (but make it opaque)
		yield return new (
			lightenOp,
			ColorBgra.FromBgra (50, 150, 200, 180), // Some semi-transparent color
			ColorBgra.Black, // Opaque black
			ColorBgra.FromBgra (50, 150, 200, 255));

		// Transparent layer on top (should be identity)
		yield return new (
			lightenOp,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.Transparent,
			ColorBgra.FromBgra (50, 150, 200, 180));

		// Transparent layer on bottom (should be identity)
		yield return new (
			lightenOp,
			ColorBgra.Transparent,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.FromBgra (50, 150, 200, 180));
	}
}
