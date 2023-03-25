using BenchmarkDotNet.Attributes;
using Cairo;
using Pinta.Core;
using Pinta.Effects;

namespace PintaBenchmarks;

[MemoryDiagnoser]
[Config (typeof (MillisecondConfig))]
public class AdjustmentsBenchmarks
{
	private readonly ImageSurface surface;
	private readonly ImageSurface dest_surface;
	private readonly RectangleI bounds;

	public AdjustmentsBenchmarks ()
	{
		surface = TestData.Get2000PixelImage ();
		dest_surface = new ImageSurface (Format.Argb32, 2000, 2000);
		bounds = surface.GetBounds ();
	}

	[Benchmark]
	public void AutoLevelEffect ()
	{
		var effect = new AutoLevelEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void BlackAndWhiteEffect ()
	{
		var effect = new BlackAndWhiteEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void BrightnessContrastEffect ()
	{
		var effect = new BrightnessContrastEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void CurvesEffect ()
	{
		var effect = new CurvesEffect ();

		var points = new SortedList<int, int> ();

		points.Add (0, 0);
		points.Add (75, 110);
		points.Add (225, 175);
		points.Add (255, 255);

		(effect.EffectData as CurvesData)!.ControlPoints = new[] { points };
		(effect.EffectData as CurvesData)!.Mode = ColorTransferMode.Luminosity;

		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void HueSaturationEffect ()
	{
		var effect = new HueSaturationEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void InvertColorsEffect ()
	{
		var effect = new InvertColorsEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void LevelsEffect ()
	{
		var effect = new LevelsEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void PosterizeEffect ()
	{
		var effect = new PosterizeEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}

	[Benchmark]
	public void SepiaEffect ()
	{
		var effect = new SepiaEffect ();
		effect.Render (surface, dest_surface, new[] { bounds });
	}
}
