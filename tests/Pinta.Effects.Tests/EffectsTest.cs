using NUnit.Framework;
using Pinta.Core;

namespace Pinta.Effects.Tests;

[TestFixture]
[Parallelizable (ParallelScope.Children)]
internal sealed class EffectsTest
{
	[Test]
	public void AddNoise1 ()
	{
		var effect = new AddNoiseEffect ();
		effect.Data.Intensity = 70;
		effect.Data.ColorSaturation = 150;
		effect.Data.Coverage = 98;
		effect.Data.Seed = new (42);
		Utilities.TestEffect (effect, "addnoise1.png");
	}

	[Test]
	public void AddNoise2 ()
	{
		var effect = new AddNoiseEffect ();
		effect.Data.Intensity = 100;
		effect.Data.ColorSaturation = 400;
		effect.Data.Coverage = 100;
		effect.Data.Seed = new (42);
		Utilities.TestEffect (effect, "addnoise2.png");
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
	[Ignore ("Produces non-deterministic results because the random seed is not fixed")]
	public void Clouds1 ()
	{
		// TODO:
	}

	[Test]
	public void Dithering1 ()
	{
		var effect = new DitheringEffect ();
		effect.Data.PaletteChoice = PredefinedPalettes.OldWindows16;
		effect.Data.ErrorDiffusionMethod = PredefinedDiffusionMatrices.FloydSteinberg;
		Utilities.TestEffect (effect, "dithering1.png");
	}

	[Test]
	public void Dithering2 ()
	{
		var effect = new DitheringEffect ();
		effect.Data.PaletteChoice = PredefinedPalettes.BlackWhite;
		effect.Data.ErrorDiffusionMethod = PredefinedDiffusionMatrices.FloydSteinberg;
		Utilities.TestEffect (effect, "dithering2.png");
	}

	[Test]
	public void Dithering3 ()
	{
		var effect = new DitheringEffect ();
		effect.Data.PaletteChoice = PredefinedPalettes.OldWindows16;
		effect.Data.ErrorDiffusionMethod = PredefinedDiffusionMatrices.Stucki;
		Utilities.TestEffect (effect, "dithering3.png");
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
		effect.Data.Angle = new (90);
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
		effect.Data.Angle = new (45);
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
		effect.Data.Rotation = new (90);
		Utilities.TestEffect (effect, "fragment2.png");
	}

	[Test]
	public void FrostedGlass ()
	{
		var effect = new FrostedGlassEffect ();
		effect.Data.Amount = 7;
		effect.Data.Seed = new (42);
		Utilities.TestEffect (effect, "frostedglass1.png");
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
		var effect = new JuliaFractalEffect (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "juliafractal1.png");
	}

	[Test]
	public void JuliaFractal2 ()
	{
		var effect = new JuliaFractalEffect (Utilities.CreateMockServices ());
		effect.Data.Factor = 6;
		effect.Data.Quality = 4;
		effect.Data.Zoom = 25;
		effect.Data.Angle = new (90);
		Utilities.TestEffect (effect, "juliafractal2.png");
	}

	[Test]
	[Ignore ("Produces different results on some platforms for unknown reasons")]
	public void MandelbrotFractal1 ()
	{
		var effect = new MandelbrotFractalEffect (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "mandelbrotfractal1.png");
	}

	[Test]
	public void MandelbrotFractal2 ()
	{
		var effect = new MandelbrotFractalEffect (Utilities.CreateMockServices ());
		effect.Data.Factor = 6;
		effect.Data.Quality = 4;
		effect.Data.Zoom = 25;
		effect.Data.Angle = new (90);
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
		effect.Data.Angle = new (50);
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
	public void PolarInversion1 ()
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
		effect.Data.Angle = new (90);
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
		effect.Data.Angle = new (90);
		Utilities.TestEffect (effect, "relief2.png");
	}

	[Test]
	public void Sharpen1 ()
	{
		var effect = new SharpenEffect ();
		Utilities.TestEffect (effect, "sharpen1.png");
	}

	[Test]
	public void Sharpen2 ()
	{
		var effect = new SharpenEffect ();
		effect.Data.Amount = 16;
		Utilities.TestEffect (effect, "sharpen2.png");
	}

	[Test]
	public void SoftenPortrait1 ()
	{
		var effect = new SoftenPortraitEffect ();
		Utilities.TestEffect (effect, "softenportrait1.png");
	}

	[Test]
	public void SoftenPortrait2 ()
	{
		var effect = new SoftenPortraitEffect ();
		effect.Data.Softness = 8;
		effect.Data.Lighting = -10;
		effect.Data.Warmth = 15;
		Utilities.TestEffect (effect, "softenportrait2.png");
	}

	[Test]
	public void Tile1 ()
	{
		var effect = new TileEffect ();
		Utilities.TestEffect (effect, "tile1.png");
	}

	[Test]
	public void Tile2 ()
	{
		var effect = new TileEffect ();
		effect.Data.Rotation = 90;
		effect.Data.TileSize = 32;
		effect.Data.Intensity = 4;
		Utilities.TestEffect (effect, "tile2.png");
	}

	[Test]
	public void Twist1 ()
	{
		var effect = new TwistEffect ();
		Utilities.TestEffect (effect, "twist1.png");
	}

	[Test]
	public void Twist2 ()
	{
		var effect = new TwistEffect ();
		effect.Data.Amount = -20;
		effect.Data.Antialias = 4;
		Utilities.TestEffect (effect, "twist2.png");
	}

	[Test]
	public void Unfocus1 ()
	{
		var effect = new UnfocusEffect ();
		Utilities.TestEffect (effect, "unfocus1.png");
	}

	[Test]
	public void Unfocus2 ()
	{
		var effect = new UnfocusEffect ();
		effect.Data.Radius = 50;
		Utilities.TestEffect (effect, "unfocus2.png");
	}

	[Test]
	public void Voronoi1 ()
	{
		var effect = new VoronoiDiagramEffect ();
		Utilities.TestEffect (effect, "voronoi1.png");
	}

	[Test]
	public void Voronoi2 ()
	{
		var effect = new VoronoiDiagramEffect ();
		effect.Data.NumberOfCells = 200;
		Utilities.TestEffect (effect, "voronoi2.png");
	}

	[Test]
	public void Voronoi3 ()
	{
		var effect = new VoronoiDiagramEffect ();
		effect.Data.DistanceMetric = VoronoiDiagramEffect.DistanceMetric.Manhattan;
		Utilities.TestEffect (effect, "voronoi3.png");
	}

	[Test]
	public void Voronoi4 ()
	{
		var effect = new VoronoiDiagramEffect ();
		effect.Data.ColorSorting = VoronoiDiagramEffect.ColorSorting.HorizontalBGR;
		Utilities.TestEffect (effect, "voronoi4.png");
	}

	[Test]
	public void ZoomBlur1 ()
	{
		var effect = new ZoomBlurEffect ();
		Utilities.TestEffect (effect, "zoomblur1.png");
	}

	[Test]
	public void ZoomBlur2 ()
	{
		var effect = new ZoomBlurEffect ();
		effect.Data.Amount = 50;
		effect.Data.Offset = new (10, 20);
		Utilities.TestEffect (effect, "zoomblur2.png");
	}
}
