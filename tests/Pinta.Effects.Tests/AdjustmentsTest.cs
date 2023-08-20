using NUnit.Framework;

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
}
