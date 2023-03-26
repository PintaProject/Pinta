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
	private readonly RectangleI bounds;

	public EffectsBenchmarks ()
	{
		surface = TestData.Get2000PixelImage ();
		dest_surface = new ImageSurface (Format.Argb32, 2000, 2000);
		bounds = surface.GetBounds ();
	}

	[Benchmark]
	public void AddNoiseEffect ()
	{
		var effect = new AddNoiseEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void BulgeEffect ()
	{
		var effect = new BulgeEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	//[Benchmark] // Requires initialized PintaCore
	public void CloudsEffect ()
	{
		var effect = new CloudsEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void EdgeDetectEffect ()
	{
		var effect = new EdgeDetectEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void EmbossEffect ()
	{
		var effect = new EmbossEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void FragmentEffect ()
	{
		var effect = new FragmentEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void FrostedGlassEffect ()
	{
		var effect = new FrostedGlassEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void GaussianBlurEffect ()
	{
		var effect = new GaussianBlurEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void GlowEffect ()
	{
		var effect = new GlowEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void InkSketchEffect ()
	{
		var effect = new InkSketchEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void JuliaFractalEffect ()
	{
		var effect = new JuliaFractalEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark] // Very slow
	public void MandelbrotFractalEffect ()
	{
		var effect = new MandelbrotFractalEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void MedianEffect ()
	{
		var effect = new MedianEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void MotionBlurEffect ()
	{
		var effect = new MotionBlurEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void OilPaintingEffect ()
	{
		var effect = new OilPaintingEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void OutlineEffect ()
	{
		var effect = new OutlineEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void PencilSketchEffect ()
	{
		var effect = new PencilSketchEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void PixelateEffect ()
	{
		var effect = new PixelateEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	//[Benchmark] // Requires initialized PintaCore
	public void PolarInversionEffect ()
	{
		var effect = new PolarInversionEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void RadialBlurEffect ()
	{
		var effect = new RadialBlurEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void RedEyeRemoveEffect ()
	{
		var effect = new RedEyeRemoveEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void ReliefEffect ()
	{
		var effect = new ReliefEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void SharpenEffect ()
	{
		var effect = new SharpenEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void SoftenPortraitEffect ()
	{
		var effect = new SoftenPortraitEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void TileEffect ()
	{
		var effect = new TileEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void TwistEffect ()
	{
		var effect = new TwistEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void UnfocusEffect ()
	{
		var effect = new UnfocusEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void ZoomBlurEffect ()
	{
		var effect = new ZoomBlurEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}
}
