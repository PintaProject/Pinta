using NUnit.Framework;
using Pinta.Effects.Tests;

namespace Pinta.Effects;

partial class EffectsTest
{
	[Test]
	public void EdgeDetect1 ()
	{
		EdgeDetectEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "edgedetect1.png");
	}

	[Test]
	public void EdgeDetect2 ()
	{
		EdgeDetectEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Angle = new (90);
		Utilities.TestEffect (effect, "edgedetect2.png");
	}

	[Test]
	public void Emboss1 ()
	{
		EmbossEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "emboss1.png");
	}

	[Test]
	public void Emboss2 ()
	{
		EmbossEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Angle = new (45);
		Utilities.TestEffect (effect, "emboss2.png");
	}

	[Test]
	public void Outline1 ()
	{
		OutlineEdgeEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "outline1.png");
	}

	[Test]
	public void Outline2 ()
	{
		OutlineEdgeEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Thickness = 25;
		effect.Data.Intensity = 20;
		Utilities.TestEffect (effect, "outline2.png");
	}

	[Test]
	public void Relief1 ()
	{
		ReliefEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "relief1.png");
	}

	[Test]
	public void Relief2 ()
	{
		ReliefEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Angle = new (90);
		Utilities.TestEffect (effect, "relief2.png");
	}
}
