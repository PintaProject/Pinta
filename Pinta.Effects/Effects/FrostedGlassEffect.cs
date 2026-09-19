/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Marco Rolappe <m_rolappe@gmx.net>                       //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class FrostedGlassEffect : BaseEffect
{
	public override string Icon => Resources.Icons.EffectsDistortFrostedGlass;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Frosted Glass");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Distort");

	public FrostedGlassData Data => (FrostedGlassData) EffectData!;

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public FrostedGlassEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new FrostedGlassData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	// Algorithm Code Ported From PDN

	private sealed record FrostedGlassSettings (
		double min_radius,
		double radius_offset,
		double bias,
		int num_samples,
		int src_width,
		int src_height,
		RandomSeed seed);

	private FrostedGlassSettings CreateSettings (ImageSurface src)
	{
		var data = Data;
		return new (
			min_radius: Math.Min (data.MinScatterRadius, data.MaxScatterRadius),
			radius_offset: Math.Max (data.MaxScatterRadius - data.MinScatterRadius, 0),
			bias: data.Diffusion,
			num_samples: data.Smoothness,
			src_width: src.Width,
			src_height: src.Height,
			seed: data.Seed
		);
	}

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		FrostedGlassSettings settings = CreateSettings (source);

		ReadOnlySpan<ColorBgra> src_data = source.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = destination.GetPixelData ();

		Random random = new (settings.seed.GetValueForRegion (roi));

		for (int y = roi.Top; y <= roi.Bottom; ++y) {

			var dst_row = dst_data.Slice (y * settings.src_width, settings.src_width);

			for (int x = roi.Left; x <= roi.Right; ++x)
				dst_row[x] = GetFinalPixelColor (settings, random, source, src_data, x, y);
		}
	}

	private static ColorBgra GetFinalPixelColor (FrostedGlassSettings settings, Random random, ImageSurface src, ReadOnlySpan<ColorBgra> src_data, int x, int y)
	{
		Span<ColorBgra> samples = stackalloc ColorBgra[settings.num_samples];

		// Blend together several samples between the min & max radius.
		// Increasing "diffusion" biases the distance offset toward the max radius.
		for (int sampleIndex = 0; sampleIndex < settings.num_samples; ++sampleIndex) {
			double angle = random.NextDouble () * Math.PI * 2;
			double t = random.NextDouble ();
			double offset = Bias (t, settings.bias) * settings.radius_offset;
			double distance = settings.min_radius + offset;

			(double sin, double cos) = Math.SinCos (angle);
			double sampleX = x + distance * cos;
			double sampleY = y + distance * sin;

			samples[sampleIndex] = src.GetBilinearSampleReflected (
				src_data, settings.src_width, settings.src_height,
				(float) sampleX, (float) sampleY);
		}

		return ColorBgra.Blend (samples, ColorBgra.Transparent);
	}

	/// <summary>
	/// Shlick's bias function.
	/// This is used to bias the random samples toward the min or max radius.
	/// A value of 0.5 (default) is linear, so the samples are spread out evenly.
	/// </summary>
	private static double Bias (double x, double bias)
	{
		return x / ((1.0 / bias - 2.0) * (1.0 - x) + 1.0);
	}

	public sealed class FrostedGlassData : EffectData
	{
		[Caption ("Maximum Scatter Radius")]
		[MinimumValue (0), MaximumValue (200)]
		public int MaxScatterRadius { get; set; } = 3;

		[Caption ("Minimum Scatter Radius")]
		[MinimumValue (0), MaximumValue (200)]
		public int MinScatterRadius { get; set; } = 0;

		[Caption ("Diffusion")]
		[MinimumValue (0), MaximumValue (1)]
		public double Diffusion { get; set; } = 0.5;

		[Caption ("Smoothness")]
		[MinimumValue (1), MaximumValue (16)]
		public int Smoothness { get; set; } = 2;

		[Caption ("Random Noise Seed")]
		public RandomSeed Seed { get; set; } = new (0);
	}
}
