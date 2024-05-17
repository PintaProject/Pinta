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
	private readonly RectangleI[] bounds;

	public AdjustmentsBenchmarks ()
	{
		surface = TestData.Get2000PixelImage ();
		dest_surface = new ImageSurface (Format.Argb32, 2000, 2000);
		bounds = new[] { surface.GetBounds () };
	}

	[Benchmark]
	public void AutoLevelEffect ()
	{
		var effect = new AutoLevelEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void BlackAndWhiteEffect ()
	{
		var effect = new BlackAndWhiteEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void BrightnessContrastEffect ()
	{
		var effect = new BrightnessContrastEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void CurvesEffect ()
	{
		var effect = new CurvesEffect (Utilities.CreateMockServices ());

		var points = new SortedList<int, int> {
			{ 0, 0 },
			{ 75, 110 },
			{ 225, 175 },
			{ 255, 255 }
		};

		(effect.EffectData as CurvesData)!.ControlPoints = new[] { points };
		(effect.EffectData as CurvesData)!.Mode = ColorTransferMode.Luminosity;

		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void HueSaturationEffect ()
	{
		var effect = new HueSaturationEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void InvertColorsEffect ()
	{
		var effect = new InvertColorsEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void LevelsEffect ()
	{
		var effect = new LevelsEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void PosterizeEffect ()
	{
		var effect = new PosterizeEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}

	[Benchmark]
	public void SepiaEffect ()
	{
		var effect = new SepiaEffect (Utilities.CreateMockServices ());
		var preRender = effect.GetPreRender (surface, dest_surface);
		effect.Render (preRender, surface, dest_surface, bounds);
	}
}
