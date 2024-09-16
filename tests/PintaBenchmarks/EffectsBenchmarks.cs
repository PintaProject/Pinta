using BenchmarkDotNet.Attributes;
using Cairo;
using Pinta.Core;
using Pinta.Effects;

namespace PintaBenchmarks;

[MemoryDiagnoser]
[Config (typeof (MillisecondConfig))]
public class EffectsBenchmarks
{
	private readonly ImageSurface surface;
	private readonly ImageSurface dest_surface;
	private readonly RectangleI[] bounds;

	public EffectsBenchmarks ()
	{
		surface = TestData.Get2000PixelImage ();
		dest_surface = new ImageSurface (Format.Argb32, 2000, 2000);
		bounds = new[] { surface.GetBounds () };
	}

	[Benchmark]
	public void AddNoiseEffect ()
	{
		var effect = new AddNoiseEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void BulgeEffect ()
	{
		var effect = new BulgeEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	//[Benchmark] // Requires initialized PintaCore
	public void CloudsEffect ()
	{
		var effect = new CloudsEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void EdgeDetectEffect ()
	{
		var effect = new EdgeDetectEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void EmbossEffect ()
	{
		var effect = new EmbossEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void FragmentEffect ()
	{
		var effect = new FragmentEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void FrostedGlassEffect ()
	{
		var effect = new FrostedGlassEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void GaussianBlurEffect ()
	{
		var effect = new GaussianBlurEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void GlowEffect ()
	{
		var effect = new GlowEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void InkSketchEffect ()
	{
		var effect = new InkSketchEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void JuliaFractalEffect ()
	{
		var effect = new JuliaFractalEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark] // Very slow
	public void MandelbrotFractalEffect ()
	{
		var effect = new MandelbrotFractalEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void MedianEffect ()
	{
		var effect = new MedianEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void MotionBlurEffect ()
	{
		var effect = new MotionBlurEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void OilPaintingEffect ()
	{
		var effect = new OilPaintingEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void OutlineEffect ()
	{
		var effect = new OutlineEdgeEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void PencilSketchEffect ()
	{
		var effect = new PencilSketchEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void PixelateEffect ()
	{
		var effect = new PixelateEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	//[Benchmark] // Requires initialized PintaCore
	public void PolarInversionEffect ()
	{
		var effect = new PolarInversionEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void RadialBlurEffect ()
	{
		var effect = new RadialBlurEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void RedEyeRemoveEffect ()
	{
		var effect = new RedEyeRemoveEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void ReliefEffect ()
	{
		var effect = new ReliefEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void SharpenEffect ()
	{
		var effect = new SharpenEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void SoftenPortraitEffect ()
	{
		var effect = new SoftenPortraitEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void TileEffect ()
	{
		var effect = new TileEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void TwistEffect ()
	{
		var effect = new TwistEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void UnfocusEffect ()
	{
		var effect = new UnfocusEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void ZoomBlurEffect ()
	{
		var effect = new ZoomBlurEffect (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}
}
