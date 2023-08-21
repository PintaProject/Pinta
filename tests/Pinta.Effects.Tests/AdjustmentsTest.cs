using System.Collections.Generic;
using NUnit.Framework;
using Pinta.Core;

namespace Pinta.Effects.Tests;

[TestFixture]
internal sealed class AdjustmentsTest
{
	[Test]
	public void AutoLevel ()
	{
		var effect = new AutoLevelEffect ();
		Utilities.TestEffect (effect, "autolevel1.png");
	}

	[Test]
	public void BlackAndWhite ()
	{
		var effect = new BlackAndWhiteEffect ();
		Utilities.TestEffect (effect, "blackandwhite1.png");
	}

	[Test]
	public void BrightnessContrastDefault ()
	{
		var effect = new BrightnessContrastEffect ();
		Utilities.TestEffect (effect, "brightnesscontrast1.png");
	}

	[Test]
	public void BrightnessContrast ()
	{
		var effect = new BrightnessContrastEffect ();
		effect.Data.Brightness = 80;
		effect.Data.Contrast = 20;
		Utilities.TestEffect (effect, "brightnesscontrast2.png");
	}

	[Test]
	public void Curves ()
	{
		var effect = new CurvesEffect ();
		var points = new SortedList<int, int> {
			{ 0, 0 },
			{ 75, 110 },
			{ 225, 175 },
			{ 255, 255 }
		};

		effect.Data.ControlPoints = new[] { points };
		effect.Data.Mode = ColorTransferMode.Luminosity;

		Utilities.TestEffect (effect, "curves1.png");
	}

	[Test]
	public void HueSaturationDefault ()
	{
		var effect = new HueSaturationEffect ();
		Utilities.TestEffect (effect, "huesaturation1.png");
	}

	[Test]
	public void HueSaturation ()
	{
		var effect = new HueSaturationEffect ();
		effect.Data.Hue = 12;
		effect.Data.Saturation = 50;
		effect.Data.Lightness = 50;
		Utilities.TestEffect (effect, "huesaturation2.png");
	}

	[Test]
	public void InvertColors ()
	{
		var effect = new InvertColorsEffect ();
		Utilities.TestEffect (effect, "invertcolors1.png");
	}

	[Test]
	public void Level ()
	{
		var effect = new LevelsEffect ();
		effect.Data.Levels = new UnaryPixelOps.Level (
			ColorBgra.Black, ColorBgra.White,
			new float[] { 0.7f, 0.8f, 0.9f },
			ColorBgra.Red, ColorBgra.Green);

		Utilities.TestEffect (effect, "level1.png");
	}

	[Test]
	public void Posterize ()
	{
		var effect = new PosterizeEffect ();
		effect.Data.Red = 6;
		effect.Data.Green = 5;
		effect.Data.Blue = 4;
		Utilities.TestEffect (effect, "posterize1.png");
	}

	[Test]
	public void Sepia ()
	{
		var effect = new SepiaEffect ();
		Utilities.TestEffect (effect, "sepia1.png");
	}
}
