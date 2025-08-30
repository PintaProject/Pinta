using System.Collections.Generic;

namespace Pinta.Core.Tests;

partial class BlendOpTests
{
	private static IEnumerable<ColorBgra> GenerateInvalidColors ()
	{
		yield return ColorBgra.FromBgra (0, 0, 100, 0);
		yield return ColorBgra.FromBgra (50, 150, 200, 180);
		yield return ColorBgra.FromBgra (100, 50, 20, 80);
		yield return ColorBgra.FromBgra (0, 0, 255, 200);
		yield return ColorBgra.FromBgra (255, 255, 255, 128);
	}
}
