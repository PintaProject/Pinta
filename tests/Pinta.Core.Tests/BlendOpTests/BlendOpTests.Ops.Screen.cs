using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

partial class BlendOpTests
{
	private static readonly UserBlendOps.ScreenBlendOp screen_op;

	private static IEnumerable<TestCaseData> CreateScreenIOCases (UserBlendOps.ScreenBlendOp screenOp)
	{
		// Semi-transparent over opaque
		yield return new (
			screenOp,
			ColorBgra.FromBgra (0, 0, 100, 255), // Opaque red
			ColorBgra.FromBgra (0, 100, 0, 128), // 50% transparent green
			ColorBgra.FromBgra (0, 100, 100, 255));

		// Semi-transparent over semi-transparent
		yield return new (
			screenOp,
			ColorBgra.FromBgra (0, 0, 100, 128), // semi-transparent red
			ColorBgra.FromBgra (0, 100, 0, 128), // semi-transparent green
			ColorBgra.FromBgra (0, 100, 100, 192));

		// Opaque gray over opaque gray
		yield return new (
			screenOp,
			ColorBgra.FromBgra (128, 128, 128, 255), // Opaque gray
			ColorBgra.FromBgra (128, 128, 128, 255), // Opaque gray
			ColorBgra.FromBgra (192, 192, 192, 255));

		// --- Cases including invalid colors

		// Screening with opaque white should result in white
		yield return new (
			screenOp,
			ColorBgra.FromBgra (50, 150, 200, 180), // Some semi-transparent color
			ColorBgra.White, // Opaque white
			ColorBgra.White);

		// Screening with opaque black should leave the color unchanged (but make it opaque)
		yield return new (
			screenOp,
			ColorBgra.FromBgra (50, 150, 200, 180), // Some semi-transparent color
			ColorBgra.Black, // Opaque black
			ColorBgra.FromBgra (50, 150, 200, 255));

		// Transparent layer on top (should be identity)
		yield return new (
			screenOp,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.Transparent,
			ColorBgra.FromBgra (50, 150, 200, 180));

		// Transparent layer on bottom (should be identity)
		yield return new (
			screenOp,
			ColorBgra.Transparent,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.FromBgra (50, 150, 200, 180));
	}
}
