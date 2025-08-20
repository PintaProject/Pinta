using NUnit.Framework;
using Pinta.Effects.Tests;

namespace Pinta.Effects;

partial class EffectsTest
{
	[Test]
	public void InkSketch1 ()
	{
		InkSketchEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "inksketch1.png");
	}

	[Test]
	public void InkSketch2 ()
	{
		InkSketchEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.InkOutline = 25;
		effect.Data.Coloring = 75;
		Utilities.TestEffect (effect, "inksketch2.png");
	}

	[Test]
	public void OilPainting1 ()
	{
		OilPaintingEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "oilpainting1.png");
	}

	[Test]
	public void OilPainting2 ()
	{
		OilPaintingEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.BrushSize = 7;
		effect.Data.Coarseness = 200;
		Utilities.TestEffect (effect, "oilpainting2.png");
	}

	[Test]
	public void PencilSketch1 ()
	{
		PencilSketchEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "pencilsketch1.png");
	}

	[Test]
	public void PencilSketch2 ()
	{
		PencilSketchEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.PencilTipSize = 10;
		effect.Data.ColorRange = 15;
		Utilities.TestEffect (effect, "pencilsketch2.png");
	}
}
