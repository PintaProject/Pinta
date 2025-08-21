using NUnit.Framework;
using Pinta.Effects.Tests;

namespace Pinta.Effects;

partial class EffectsTest
{
	[Test]
	public void AlignObject1 ()
	{
		AlignObjectEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Position = AlignPosition.TopLeft;
		Utilities.TestEffect (effect, "alignobject1.png", source_image_name: "alignobjectinput.png");
	}

	[Test]
	public void AlignObject2 ()
	{
		AlignObjectEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Position = AlignPosition.Center;
		Utilities.TestEffect (effect, "alignobject2.png", source_image_name: "alignobjectinput.png");
	}

	[Test]
	public void AlignObject3 ()
	{
		AlignObjectEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Position = AlignPosition.BottomRight;
		Utilities.TestEffect (effect, "alignobject3.png", source_image_name: "alignobjectinput.png");
	}

	[Test]
	public void Feather1 ()
	{
		FeatherEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Tolerance = 20;
		effect.Data.Radius = 5;
		effect.Data.FeatherCanvasEdge = true;
		Utilities.TestEffect (effect, "feather1.png");
	}

	[Test]
	public void Feather2 ()
	{
		FeatherEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Tolerance = 20;
		effect.Data.Radius = 10;
		effect.Data.FeatherCanvasEdge = true;
		Utilities.TestEffect (effect, "feather2.png");
	}

	[Test]
	public void OutlineObject1 ()
	{
		OutlineObjectEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Radius = 10;
		effect.Data.Tolerance = 130;
		effect.Data.AlphaGradient = true;
		effect.Data.ColorGradient = false;
		effect.Data.OutlineBorder = false;
		effect.Data.FillObjectBackground = false;
		Utilities.TestEffect (effect, "outlineobject1.png", source_image_name: "outlineobjectinput.png");
	}
	[Test]
	public void OutlineObject2 ()
	{
		OutlineObjectEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Radius = 10;
		effect.Data.Tolerance = 20;
		effect.Data.AlphaGradient = false;
		effect.Data.ColorGradient = true;
		effect.Data.OutlineBorder = true;
		effect.Data.FillObjectBackground = false;
		Utilities.TestEffect (effect, "outlineobject2.png", source_image_name: "outlineobjectinput.png");
	}
	[Test]
	public void OutlineObject3 ()
	{
		OutlineObjectEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Radius = 10;
		effect.Data.Tolerance = 20;
		effect.Data.AlphaGradient = true;
		effect.Data.ColorGradient = true;
		effect.Data.OutlineBorder = false;
		effect.Data.FillObjectBackground = true;
		Utilities.TestEffect (effect, "outlineobject3.png", source_image_name: "outlineobjectinput.png");
	}
	[Test]
	public void OutlineObject4 ()
	{
		OutlineObjectEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Radius = 1;
		effect.Data.Tolerance = 20;
		effect.Data.AlphaGradient = false;
		effect.Data.ColorGradient = false;
		effect.Data.OutlineBorder = false;
		effect.Data.FillObjectBackground = false;
		Utilities.TestEffect (effect, "outlineobject4.png", source_image_name: "outlineobjectinput.png");
	}
	[Test]
	public void OutlineObject5 ()
	{
		OutlineObjectEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Radius = 0;
		effect.Data.Tolerance = 20;
		effect.Data.AlphaGradient = false;
		effect.Data.ColorGradient = false;
		effect.Data.OutlineBorder = false;
		effect.Data.FillObjectBackground = false;
		Utilities.TestEffect (effect, "outlineobject5.png", source_image_name: "outlineobjectinput.png");
	}
}
