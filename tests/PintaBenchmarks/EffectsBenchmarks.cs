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
		AddNoiseEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void BulgeEffect ()
	{
		BulgeEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void CloudsEffect ()
	{
		CloudsEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void DentsEffect ()
	{
		DentsEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void EdgeDetectEffect ()
	{
		EdgeDetectEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void EmbossEffect ()
	{
		EmbossEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void FragmentEffect ()
	{
		FragmentEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void FrostedGlassEffect ()
	{
		FrostedGlassEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void GaussianBlurEffect ()
	{
		GaussianBlurEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void GlowEffect ()
	{
		GlowEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void InkSketchEffect ()
	{
		InkSketchEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void JuliaFractalEffect ()
	{
		JuliaFractalEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark] // Very slow
	public void MandelbrotFractalEffect ()
	{
		MandelbrotFractalEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void MedianEffect ()
	{
		MedianEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void MotionBlurEffect ()
	{
		MotionBlurEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void OilPaintingEffect ()
	{
		OilPaintingEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void OutlineEffect ()
	{
		OutlineEdgeEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void PencilSketchEffect ()
	{
		PencilSketchEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void PixelateEffect ()
	{
		PixelateEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void PolarInversionEffect ()
	{
		PolarInversionEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void RadialBlurEffect ()
	{
		RadialBlurEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void RedEyeRemoveEffect ()
	{
		RedEyeRemoveEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void ReliefEffect ()
	{
		ReliefEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void SharpenEffect ()
	{
		SharpenEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void SoftenPortraitEffect ()
	{
		SoftenPortraitEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void TileEffect ()
	{
		TileEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void TwistEffect ()
	{
		TwistEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void UnfocusEffect ()
	{
		UnfocusEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void ZoomBlurEffect ()
	{
		ZoomBlurEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}
}
