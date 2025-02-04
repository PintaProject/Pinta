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
		bounds = [surface.GetBounds ()];
	}

	[Benchmark]
	public void AutoLevelEffect ()
	{
		AutoLevelEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void BlackAndWhiteEffect ()
	{
		BlackAndWhiteEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void BrightnessContrastEffect ()
	{
		BrightnessContrastEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void CurvesEffect ()
	{
		CurvesEffect effect = new (Utilities.CreateMockServices ());

		SortedList<int, int> points = new () {
			{ 0, 0 },
			{ 75, 110 },
			{ 225, 175 },
			{ 255, 255 }
		};

		(effect.EffectData as CurvesData)!.ControlPoints = [points];
		(effect.EffectData as CurvesData)!.Mode = ColorTransferMode.Luminosity;

		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void HueSaturationEffect ()
	{
		HueSaturationEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void InvertColorsEffect ()
	{
		InvertColorsEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void LevelsEffect ()
	{
		LevelsEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void PosterizeEffect ()
	{
		PosterizeEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}

	[Benchmark]
	public void SepiaEffect ()
	{
		SepiaEffect effect = new (Utilities.CreateMockServices ());
		effect.Render (surface, dest_surface, bounds);
	}
}
