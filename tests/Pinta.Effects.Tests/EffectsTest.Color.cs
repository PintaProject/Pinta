using NUnit.Framework;
using Pinta.Effects.Tests;

namespace Pinta.Effects;

partial class EffectsTest
{
	[Test]
	public void Dithering1 ()
	{
		DitheringEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.PaletteChoice = PredefinedPalettes.OldWindows16;
		effect.Data.ErrorDiffusionMethod = PredefinedDiffusionMatrices.FloydSteinberg;
		Utilities.TestEffect (effect, "dithering1.png");
	}

	[Test]
	public void Dithering2 ()
	{
		DitheringEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.PaletteChoice = PredefinedPalettes.BlackWhite;
		effect.Data.ErrorDiffusionMethod = PredefinedDiffusionMatrices.FloydSteinberg;
		Utilities.TestEffect (effect, "dithering2.png");
	}

	[Test]
	public void Dithering3 ()
	{
		DitheringEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.PaletteChoice = PredefinedPalettes.OldWindows16;
		effect.Data.ErrorDiffusionMethod = PredefinedDiffusionMatrices.Stucki;
		Utilities.TestEffect (effect, "dithering3.png");
	}

	[Test]
	public void Dithering4 ()
	{
		DitheringEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.PaletteChoice = PredefinedPalettes.OldMsPaint;
		effect.Data.ErrorDiffusionMethod = PredefinedDiffusionMatrices.Atkinson;
		Utilities.TestEffect (effect, "dithering4.png");
	}

	[Test]
	public void ColorQuantization1 ()
	{
		ColorQuantizationEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.ColorCount = 3;
		Utilities.TestEffect (effect, "colorquantization1.png");
	}

	[Test]
	public void ColorQuantization2 ()
	{
		ColorQuantizationEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.ColorCount = 64;
		Utilities.TestEffect (effect, "colorquantization2.png");
	}
}
