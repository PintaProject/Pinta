using NUnit.Framework;
using Pinta.Effects.Tests;

namespace Pinta.Effects;

partial class EffectsTest
{
	[Test]
	public void AddNoise1 ()
	{
		AddNoiseEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Intensity = 70;
		effect.Data.ColorSaturation = 150;
		effect.Data.Coverage = 98;
		effect.Data.Seed = new (42);
		Utilities.TestEffect (effect, "addnoise1.png");
	}

	[Test]
	public void AddNoise2 ()
	{
		AddNoiseEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Intensity = 100;
		effect.Data.ColorSaturation = 400;
		effect.Data.Coverage = 100;
		effect.Data.Seed = new (42);
		Utilities.TestEffect (effect, "addnoise2.png");
	}

	[Test]
	public void Median1 ()
	{
		MedianEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "median1.png");
	}

	[Test]
	public void Median2 ()
	{
		MedianEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Radius = 50;
		effect.Data.Percentile = 25;
		Utilities.TestEffect (effect, "median2.png");
	}
}
