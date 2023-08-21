using NUnit.Framework;
using Pinta.Core;

namespace Pinta.Effects.Tests;

[TestFixture]
[Parallelizable (ParallelScope.Children)]
internal sealed class EffectsTest
{
	[Test]
	[Ignore ("Produces non-deterministic results because the random seed is not fixed")]
	public void AddNoise ()
	{
		// TODO
	}

	[Test]
	public void Bulge ()
	{
		var effect = new BulgeEffect ();
		effect.Data.Amount = 56;
		effect.Data.Offset = new PointD (0, 0);
		Utilities.TestEffect (effect, "bulge1.png");
	}

	[Test]
	public void BulgeIn ()
	{
		var effect = new BulgeEffect ();
		effect.Data.Amount = -59;
		effect.Data.Offset = new PointD (-0.184, -0.304);
		Utilities.TestEffect (effect, "bulge2.png");
	}

	[Test]
	[Ignore ("Produces non-deterministic results because the random seed is not fixed, and depends on the global palette")]
	public void Clouds ()
	{
		// TODO
	}

	[Test]
	public void EdgeDetect1 ()
	{
		var effect = new EdgeDetectEffect ();
		Utilities.TestEffect (effect, "edgedetect1.png");
	}

	[Test]
	public void EdgeDetect2 ()
	{
		var effect = new EdgeDetectEffect ();
		effect.Data.Angle = 90;
		Utilities.TestEffect (effect, "edgedetect2.png");
	}

	[Test]
	public void Emboss1 ()
	{
		var effect = new EmbossEffect ();
		Utilities.TestEffect (effect, "emboss1.png");
	}

	[Test]
	public void Emboss2 ()
	{
		var effect = new EmbossEffect ();
		effect.Data.Angle = 45;
		Utilities.TestEffect (effect, "emboss2.png");
	}

	[Test]
	public void Fragment1 ()
	{
		var effect = new FragmentEffect ();
		Utilities.TestEffect (effect, "fragment1.png");
	}

	[Test]
	public void Fragment2 ()
	{
		var effect = new FragmentEffect ();
		effect.Data.Fragments = 25;
		effect.Data.Distance = 60;
		effect.Data.Rotation = 90;
		Utilities.TestEffect (effect, "fragment2.png");
	}

	[Test]
	[Ignore ("Produces non-deterministic results because the random seed is not fixed")]
	public void FrostedGlass ()
	{
		// TODO
	}

	[Test]
	public void GaussianBlur1 ()
	{
		var effect = new GaussianBlurEffect ();
		Utilities.TestEffect (effect, "gaussianblur1.png");
	}

	[Test]
	public void GaussianBlur2 ()
	{
		var effect = new GaussianBlurEffect ();
		effect.Data.Radius = 100;
		Utilities.TestEffect (effect, "gaussianblur2.png");
	}

	[Test]
	public void Glow1 ()
	{
		var effect = new GlowEffect ();
		Utilities.TestEffect (effect, "glow1.png");
	}

	[Test]
	public void Glow2 ()
	{
		var effect = new GlowEffect ();
		effect.Data.Radius = 12;
		effect.Data.Brightness = 50;
		effect.Data.Contrast = 50;
		Utilities.TestEffect (effect, "glow2.png");
	}

	[Test]
	public void InkSketch1 ()
	{
		var effect = new InkSketchEffect ();
		Utilities.TestEffect (effect, "inksketch1.png");
	}

	[Test]
	public void InkSketch2 ()
	{
		var effect = new InkSketchEffect ();
		effect.Data.InkOutline = 25;
		effect.Data.Coloring = 75;
		Utilities.TestEffect (effect, "inksketch2.png");
	}

	[Test]
	public void JuliaFractal1 ()
	{
		var effect = new JuliaFractalEffect ();
		Utilities.TestEffect (effect, "juliafractal1.png");
	}

	[Test]
	public void JuliaFractal2 ()
	{
		var effect = new JuliaFractalEffect ();
		effect.Data.Factor = 6;
		effect.Data.Quality = 4;
		effect.Data.Zoom = 25;
		effect.Data.Angle = 90;
		Utilities.TestEffect (effect, "juliafractal2.png");
	}

	[Test]
	[Ignore ("Produces different results on some platforms for unknown reasons")]
	public void MandelbrotFractal1 ()
	{
		var effect = new MandelbrotFractalEffect ();
		Utilities.TestEffect (effect, "mandelbrotfractal1.png");
	}

	[Test]
	public void MandelbrotFractal2 ()
	{
		var effect = new MandelbrotFractalEffect ();
		effect.Data.Factor = 6;
		effect.Data.Quality = 4;
		effect.Data.Zoom = 25;
		effect.Data.Angle = 90;
		effect.Data.InvertColors = true;
		Utilities.TestEffect (effect, "mandelbrotfractal2.png");
	}

	[Test]
	public void Median1 ()
	{
		var effect = new MedianEffect ();
		Utilities.TestEffect (effect, "median1.png");
	}

	[Test]
	public void Median2 ()
	{
		var effect = new MedianEffect ();
		effect.Data.Radius = 50;
		effect.Data.Percentile = 25;
		Utilities.TestEffect (effect, "median2.png");
	}

	[Test]
	public void MotionBlur1 ()
	{
		var effect = new MotionBlurEffect ();
		Utilities.TestEffect (effect, "motionblur1.png");
	}

	[Test]
	public void MotionBlur2 ()
	{
		var effect = new MotionBlurEffect ();
		effect.Data.Angle = 50;
		effect.Data.Distance = 25;
		effect.Data.Centered = false;
		Utilities.TestEffect (effect, "motionblur2.png");
	}

	[Test]
	public void OilPainting1 ()
	{
		var effect = new OilPaintingEffect ();
		Utilities.TestEffect (effect, "oilpainting1.png");
	}

	[Test]
	public void OilPainting2 ()
	{
		var effect = new OilPaintingEffect ();
		effect.Data.BrushSize = 7;
		effect.Data.Coarseness = 200;
		Utilities.TestEffect (effect, "oilpainting2.png");
	}

	[Test]
	public void Outline1 ()
	{
		var effect = new OutlineEffect ();
		Utilities.TestEffect (effect, "outline1.png");
	}

	[Test]
	public void Outline2 ()
	{
		var effect = new OutlineEffect ();
		effect.Data.Thickness = 25;
		effect.Data.Intensity = 20;
		Utilities.TestEffect (effect, "outline2.png");
	}

	[Test]
	public void PencilSketch1 ()
	{
		var effect = new PencilSketchEffect ();
		Utilities.TestEffect (effect, "pencilsketch1.png");
	}

	[Test]
	public void PencilSketch2 ()
	{
		var effect = new PencilSketchEffect ();
		effect.Data.PencilTipSize = 10;
		effect.Data.ColorRange = 15;
		Utilities.TestEffect (effect, "pencilsketch2.png");
	}

	[Test]
	public void Pixelate1 ()
	{
		var effect = new PixelateEffect ();
		Utilities.TestEffect (effect, "pixelate1.png");
	}

	[Test]
	public void Pixelate2 ()
	{
		var effect = new PixelateEffect ();
		effect.Data.CellSize = 10;
		Utilities.TestEffect (effect, "pixelate2.png");
	}

	[Test]
	[Ignore ("Depends on PintaCore being initialized")]
	public void PolarInversion ()
	{
		// TODO
	}

	[Test]
	public void RadialBlur1 ()
	{
		var effect = new RadialBlurEffect ();
		Utilities.TestEffect (effect, "radialblur1.png");
	}

	[Test]
	public void RadialBlur2 ()
	{
		var effect = new RadialBlurEffect ();
		effect.Data.Angle = 90;
		effect.Data.Offset = new (20, 20);
		effect.Data.Quality = 4;
		Utilities.TestEffect (effect, "radialblur2.png");
	}

	[Test]
	public void RedEyeRemove1 ()
	{
		var effect = new RedEyeRemoveEffect ();
		Utilities.TestEffect (effect, "redeyeremove1.png");
	}

	[Test]
	public void RedEyeRemove2 ()
	{
		var effect = new RedEyeRemoveEffect ();
		effect.Data.Tolerance = 20;
		effect.Data.Saturation = 20;
		Utilities.TestEffect (effect, "redeyeremove2.png");
	}

	[Test]
	public void Relief1 ()
	{
		var effect = new ReliefEffect ();
		Utilities.TestEffect (effect, "relief1.png");
	}

	[Test]
	public void Relief2 ()
	{
		var effect = new ReliefEffect ();
		effect.Data.Angle = 90;
		Utilities.TestEffect (effect, "relief2.png");
	}

	[Test]
	[Ignore ("Depends on PintaCore being initialized")]
	public void WarpEffect ()
	{
		// TODO
	}
}
