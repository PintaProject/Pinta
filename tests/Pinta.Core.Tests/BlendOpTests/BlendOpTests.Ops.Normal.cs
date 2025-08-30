using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

partial class BlendOpTests
{
	private static readonly UserBlendOps.NormalBlendOp normal_op;

	private static IEnumerable<TestCaseData> CreateNormalIOCases (UserBlendOps.NormalBlendOp normalOp)
	{
		// Semi-transparent over opaque
		yield return new (
			normalOp,
			ColorBgra.FromBgra (0, 0, 100, 255), // Some opaque red
			ColorBgra.FromBgra (0, 100, 0, 128), // Some 50% transparent green
			ColorBgra.FromBgra (0, 100, 50, 255));

		// Semi-transparent over semi-transparent
		yield return new (
			normalOp,
			ColorBgra.FromBgra (0, 0, 100, 128), // Some red, semi-transparent
			ColorBgra.FromBgra (0, 100, 0, 128), // Some green, semi-transparent
			ColorBgra.FromBgra (0, 100, 50, 192));

		yield return new (
			normalOp,
			ColorBgra.FromBgra (0, 0, 0, 128),  // Black, semi-transparent
			ColorBgra.FromBgra (0, 0, 0, 128),  // Black, semi-transparent
			ColorBgra.FromBgra (0, 0, 0, 192)); // Black, slightly less transparent

		// Goes through rounding (up) logic
		yield return new (
			normalOp,
			ColorBgra.FromBgra (0, 0, 1, 255),
			ColorBgra.FromBgra (0, 0, 0, 128),
			ColorBgra.FromBgra (0, 0, 1, 255));

		// Goes through rounding (down) logic
		yield return new (
			normalOp,
			ColorBgra.FromBgra (0, 0, 1, 255),
			ColorBgra.FromBgra (0, 0, 0, 129),
			ColorBgra.FromBgra (0, 0, 0, 255));
	}
}
