using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

partial class BlendOpTests
{
	private static readonly UserBlendOps.DarkenBlendOp darken_op;

	private static IEnumerable<TestCaseData> CreateDarkenIOCases (UserBlendOps.DarkenBlendOp darkenOp)
	{
		// Semi-transparent over opaque
		yield return new (
			darkenOp,
			ColorBgra.FromBgra (0, 0, 100, 255), // Opaque red
			ColorBgra.FromBgra (0, 100, 0, 128), // 50% transparent green
			ColorBgra.FromBgra (0, 0, 50, 255));

		// Semi-transparent over semi-transparent
		yield return new (
			darkenOp,
			ColorBgra.FromBgra (0, 0, 100, 128), // semi-transparent red
			ColorBgra.FromBgra (0, 100, 0, 128), // semi-transparent green
			ColorBgra.FromBgra (0, 50, 50, 192));

		// Opaque gray over opaque lighter gray
		yield return new (
			darkenOp,
			ColorBgra.FromBgra (192, 192, 192, 255), // Opaque light gray
			ColorBgra.FromBgra (128, 128, 128, 255), // Opaque gray
			ColorBgra.FromBgra (128, 128, 128, 255));

		// --- Special Cases

		// Blending with opaque white should leave the color unchanged (but make it opaque)
		yield return new (
			darkenOp,
			ColorBgra.FromBgra (50, 150, 200, 180), // Some semi-transparent color
			ColorBgra.White, // Opaque white
			ColorBgra.FromBgra (125, 225, 255, 255));

		// Blending with opaque black should result in black
		yield return new (
			darkenOp,
			ColorBgra.FromBgra (50, 150, 200, 180), // Some semi-transparent color
			ColorBgra.Black, // Opaque black
			ColorBgra.Black);

		// Transparent layer on top (should be identity)
		yield return new (
			darkenOp,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.Transparent,
			ColorBgra.FromBgra (50, 150, 200, 180));

		// Transparent layer on bottom (should be identity)
		yield return new (
			darkenOp,
			ColorBgra.Transparent,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.FromBgra (50, 150, 200, 180));
	}
}
