using BenchmarkDotNet.Attributes;
using Cairo;
using Pinta.Core;

namespace PintaBenchmarks;

[MemoryDiagnoser]
[Config (typeof (MillisecondConfig))]
public class BlendOpBenchmarks
{
	private readonly ImageSurface surface;
	private readonly ImageSurface dest_surface;

	public BlendOpBenchmarks ()
	{
		dest_surface = TestData.Get2000PixelImage ();

		// Test blending a partially transparent shape on top of an image.
		surface = new ImageSurface (Format.Argb32, 2000, 2000);
		using Cairo.Context c = new (surface);
		c.FillEllipse (surface.GetBounds ().ToDouble (), new Color (0, 1, 0, 0.5));
	}

	[Benchmark]
	public void AdditiveBlendOp ()
	{
		UserBlendOps.AdditiveBlendOp op = new ();
		op.Apply (dest_surface, surface);
	}

	[Benchmark]
	public void ColorBurnBlendOp ()
	{
		UserBlendOps.ColorBurnBlendOp op = new ();
		op.Apply (dest_surface, surface);
	}

	[Benchmark]
	public void ColorDodgeBlendOp ()
	{
		UserBlendOps.ColorDodgeBlendOp op = new ();
		op.Apply (dest_surface, surface);
	}

	[Benchmark]
	public void DarkenBlendOp ()
	{
		UserBlendOps.DarkenBlendOp op = new ();
		op.Apply (dest_surface, surface);
	}

	[Benchmark]
	public void DifferenceBlendOp ()
	{
		UserBlendOps.DifferenceBlendOp op = new ();
		op.Apply (dest_surface, surface);
	}

	[Benchmark]
	public void GlowBlendOp ()
	{
		UserBlendOps.GlowBlendOp op = new ();
		op.Apply (dest_surface, surface);
	}

	[Benchmark]
	public void LightenBlendOp ()
	{
		UserBlendOps.LightenBlendOp op = new ();
		op.Apply (dest_surface, surface);
	}

	[Benchmark]
	public void MultiplyBlendOp ()
	{
		UserBlendOps.MultiplyBlendOp op = new ();
		op.Apply (dest_surface, surface);
	}

	[Benchmark]
	public void NegationBlendOp ()
	{
		UserBlendOps.NegationBlendOp op = new ();
		op.Apply (dest_surface, surface);
	}

	[Benchmark]
	public void NormalBlendOp ()
	{
		UserBlendOps.NormalBlendOp op = new ();
		op.Apply (dest_surface, surface);
	}

	[Benchmark]
	public void OverlayBlendOp ()
	{
		UserBlendOps.OverlayBlendOp op = new ();
		op.Apply (dest_surface, surface);
	}

	[Benchmark]
	public void ReflectBlendOp ()
	{
		UserBlendOps.ReflectBlendOp op = new ();
		op.Apply (dest_surface, surface);
	}

	[Benchmark]
	public void ScreenBlendOp ()
	{
		UserBlendOps.ScreenBlendOp op = new ();
		op.Apply (dest_surface, surface);
	}

	[Benchmark]
	public void XorBlendOp ()
	{
		UserBlendOps.XorBlendOp op = new ();
		op.Apply (dest_surface, surface);
	}
}
