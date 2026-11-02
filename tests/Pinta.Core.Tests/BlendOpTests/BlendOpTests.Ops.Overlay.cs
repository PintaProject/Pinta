using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

partial class BlendOpTests
{
	private static readonly UserBlendOps.OverlayBlendOp overlay_op;

	private static IEnumerable<TestCaseData> CreateOverlayIOCases (UserBlendOps.OverlayBlendOp overlayOp)
	{
		// Overlaying with gray == identity, either side
		yield return new (
			overlayOp,
			ColorBgra.FromBgra (80, 80, 80, 255), // Dark gray
			ColorBgra.FromBgra (128, 128, 128, 255), // Mid gray
			ColorBgra.FromBgra (80, 80, 80, 255));

		yield return new (
			overlayOp,
			ColorBgra.FromBgra (200, 200, 200, 255), // Light gray
			ColorBgra.FromBgra (128, 128, 128, 255), // Mid gray
			ColorBgra.FromBgra (200, 200, 200, 255));

		// Semi-transparent 50% gray over opaque = identity as well
		yield return new (
			overlayOp,
			ColorBgra.FromBgra (100, 100, 100, 255), // Opaque gray
			ColorBgra.FromBgra (64, 64, 64, 128), // 50% transparent gray
			ColorBgra.FromBgra (100, 100, 100, 255));

		// Semi-transparent over semi-transparent
		yield return new (
			overlayOp,
			ColorBgra.FromBgra (50, 50, 50, 128),
			ColorBgra.FromBgra (64, 64, 64, 128),
			ColorBgra.FromBgra (82, 82, 82, 192));

		// --- Cases including invalid colors

		// Overlaying with opaque black (dark channels turn black, light ones darken)
		yield return new (
			overlayOp,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.Black,
			ColorBgra.FromBgra (0, 120, 220, 255));

		// Overlaying with opaque white (light channels turn white, dark ones lighten)
		yield return new (
			overlayOp,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.White,
			ColorBgra.FromBgra (175, 255, 255, 255));

		// Transparent on top == identity
		yield return new (
			overlayOp,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.Transparent,
			ColorBgra.FromBgra (50, 150, 200, 180));

		// Transparent on bottom == identity
		yield return new (
			overlayOp,
			ColorBgra.Transparent,
			ColorBgra.FromBgra (50, 150, 200, 180),
			ColorBgra.FromBgra (50, 150, 200, 180));
	}
}
