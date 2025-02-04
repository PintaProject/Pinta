using System.Collections.Generic;
using NUnit.Framework;
using Pinta.Core;

namespace Pinta.Effects.Tests;

[TestFixture]
[Parallelizable (ParallelScope.Children)]
internal sealed class AdjustmentsTest
{
	[Test]
	public void AutoLevel ()
	{
		AutoLevelEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "autolevel1.png");
	}

	[Test]
	public void BlackAndWhite ()
	{
		BlackAndWhiteEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "blackandwhite1.png");
	}

	[Test]
	public void BrightnessContrastDefault ()
	{
		BrightnessContrastEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "brightnesscontrast1.png");
	}

	[Test]
	public void BrightnessContrast ()
	{
		BrightnessContrastEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Brightness = 80;
		effect.Data.Contrast = 20;
		Utilities.TestEffect (effect, "brightnesscontrast2.png");
	}

	[Test]
	public void Curves ()
	{
		CurvesEffect effect = new (Utilities.CreateMockServices ());
		SortedList<int, int> points = new () {
			{ 0, 0 },
			{ 75, 110 },
			{ 225, 175 },
			{ 255, 255 }
		};

		effect.Data.ControlPoints = [points];
		effect.Data.Mode = ColorTransferMode.Luminosity;

		Utilities.TestEffect (effect, "curves1.png");
	}

	[Test]
	public void HueSaturationDefault ()
	{
		HueSaturationEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "huesaturation1.png");
	}

	[Test]
	public void HueSaturation ()
	{
		HueSaturationEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Hue = 12;
		effect.Data.Saturation = 50;
		effect.Data.Lightness = 50;
		Utilities.TestEffect (effect, "huesaturation2.png");
	}

	[Test]
	public void InvertColors ()
	{
		InvertColorsEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "invertcolors1.png");
	}

	[Test]
	public void Level ()
	{
		LevelsEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Levels = new UnaryPixelOps.Level (
			ColorBgra.Black, ColorBgra.White,
			[0.7f, 0.8f, 0.9f],
			ColorBgra.Red, ColorBgra.Green);

		Utilities.TestEffect (effect, "level1.png");
	}

	[Test]
	public void Posterize ()
	{
		PosterizeEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Red = 6;
		effect.Data.Green = 5;
		effect.Data.Blue = 4;
		Utilities.TestEffect (effect, "posterize1.png");
	}

	[Test]
	public void Sepia ()
	{
		SepiaEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "sepia1.png");
	}
}
