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
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void BulgeEffect ()
	{
		var effect = new BulgeEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	//[Benchmark] // Requires initialized PintaCore
	public void CloudsEffect ()
	{
		var effect = new CloudsEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void EdgeDetectEffect ()
	{
		var effect = new EdgeDetectEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void EmbossEffect ()
	{
		var effect = new EmbossEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void FragmentEffect ()
	{
		var effect = new FragmentEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void FrostedGlassEffect ()
	{
		var effect = new FrostedGlassEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void GaussianBlurEffect ()
	{
		var effect = new GaussianBlurEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void GlowEffect ()
	{
		var effect = new GlowEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void InkSketchEffect ()
	{
		var effect = new InkSketchEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void JuliaFractalEffect ()
	{
		var effect = new JuliaFractalEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark] // Very slow
	public void MandelbrotFractalEffect ()
	{
		var effect = new MandelbrotFractalEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void MedianEffect ()
	{
		var effect = new MedianEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void MotionBlurEffect ()
	{
		var effect = new MotionBlurEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void OilPaintingEffect ()
	{
		var effect = new OilPaintingEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void OutlineEffect ()
	{
		var effect = new OutlineEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void PencilSketchEffect ()
	{
		var effect = new PencilSketchEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void PixelateEffect ()
	{
		var effect = new PixelateEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	//[Benchmark] // Requires initialized PintaCore
	public void PolarInversionEffect ()
	{
		var effect = new PolarInversionEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void RadialBlurEffect ()
	{
		var effect = new RadialBlurEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void RedEyeRemoveEffect ()
	{
		var effect = new RedEyeRemoveEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void ReliefEffect ()
	{
		var effect = new ReliefEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void SharpenEffect ()
	{
		var effect = new SharpenEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void SoftenPortraitEffect ()
	{
		var effect = new SoftenPortraitEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void TileEffect ()
	{
		var effect = new TileEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void TwistEffect ()
	{
		var effect = new TwistEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void UnfocusEffect ()
	{
		var effect = new UnfocusEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void ZoomBlurEffect ()
	{
		var effect = new ZoomBlurEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}
}
