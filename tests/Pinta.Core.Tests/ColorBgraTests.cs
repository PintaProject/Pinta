using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class ColorBgraTests
{
	[TestCaseSource (nameof (new_alpha_cases))]
	public void NewAlpha (ColorBgra input, int newAlpha, ColorBgra expected)
	{
		Assert.That (input.NewAlpha ((byte) newAlpha), Is.EqualTo (expected));
	}

	static readonly IReadOnlyList<TestCaseData> new_alpha_cases = [
		new (ColorBgra.FromBgra (255, 0, 128, 255), 128, ColorBgra.FromBgra (128, 0, 64, 128)),
		new (ColorBgra.Transparent, 255, ColorBgra.Black),
	];
}
