// Copyright (c) 2025 Jerry Huxtable
//
// MIT License: http://www.opensource.org/licenses/mit-license.php
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// Ported to Pinta by Martin del Rio

using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class CausticsEffect : BaseEffect
{
	private readonly IChromeService chrome;
	private readonly IPaletteService palette;
	private readonly IWorkspaceService workspace;

	public sealed override bool IsTileable => false;

	public override string Name => Translations.GetString ("Caustics");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Render");

	public CausticsData Data => (CausticsData) EffectData!;  // NRT - Set in constructor

	private static readonly double s_rad = Math.Sin (0.1);
	private static readonly double c_rad = Math.Cos (0.1);

	public CausticsEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		palette = services.GetService<IPaletteService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new CausticsData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	// Algorithm Code Ported From JH Labs

	private sealed record CausticsSettings (
		Size canvasSize,
		int v,
		int samples,
		double dispersion,
		double scale,
		double invScale,
		double d,
		double focus,
		double turbulence,
		double time,
		ColorBgra background,
		RandomSeed seed);
	private CausticsSettings CreateSettings (ImageSurface dest)
	{
		int valor = Data.Brightness / Math.Max (1, Data.Samples);
		if (valor == 0)
			valor = (Data.Brightness > 0 && Data.Samples > 0) ? 1 : 0;

		return new (
			canvasSize: dest.GetSize (),
			v: valor,
			samples: Data.Samples,
			dispersion: Data.Dispersion,
			scale: Data.Scale,
			invScale: 1.0 / Data.Scale,
			d: 0.95,
			focus: 0.1 + Data.Amount,
			seed: Data.Seed,
			turbulence: Data.Turbulence,
			background: palette.PrimaryColor.ToColorBgra (),
			time: Data.TimeOffset);
	}
	static ColorBgra AddPremultipliedBrightness (ColorBgra premult, int brightness)
	{
		// Convert to straight-alpha (undo premultiplied)
		var straight = premult.ToStraightAlpha ();
		// Bump each channel
		straight = ColorBgra.FromBgraClamped (
		    straight.B + brightness,
		    straight.G + brightness,
		    straight.R + brightness,
		    straight.A);
		// Re-premultiply
		return straight.ToPremultipliedAlpha ();
	}

	protected override void Render (ImageSurface src, ImageSurface dest, RectangleI roi)
	{
		CausticsSettings settings = CreateSettings (dest);
		Random jitterRand = new (settings.seed.Value);
		PerlinNoise3D noiseGenerator = new (settings.seed.Value);

		Span<ColorBgra> dest_data = dest.GetPixelData ();

		foreach (var pixel in roi.GeneratePixelOffsets (settings.canvasSize))
			dest_data[pixel.memoryOffset] = settings.background;

		foreach (var pixel in roi.GeneratePixelOffsets (settings.canvasSize)) {
			// Loop for multi-sampling
			for (int s_sample = 0; s_sample < settings.samples; s_sample++) {
				// Add random jitter for sampling
				double sx = pixel.coordinates.X + jitterRand.NextDouble ();
				double sy = pixel.coordinates.Y + jitterRand.NextDouble ();

				// Normalized coordinates for noise evaluation
				double nx = sx * settings.invScale;
				double ny = sy * settings.invScale;

				// Evaluate noise for displacement
				double xDisplacement = Evaluate (settings, noiseGenerator, nx - settings.d, ny) - Evaluate (settings, noiseGenerator, nx + settings.d, ny);
				double yDisplacement = Evaluate (settings, noiseGenerator, nx, ny + settings.d) - Evaluate (settings, noiseGenerator, nx, ny - settings.d);

				if (settings.dispersion > 0.0) {
					for (int channel = 0; channel < 3; channel++) // R, G, B channels
					{
						double ca = (1.0 + channel * settings.dispersion);
						double targetX = sx + settings.scale * settings.focus * xDisplacement * ca;
						double targetY = sy + settings.scale * settings.focus * yDisplacement * ca;

						if (targetX >= 0 && targetX < settings.canvasSize.Width - 1 &&
							targetY >= 0 && targetY < settings.canvasSize.Height - 1) {
							int dest_offset = (int) targetY * settings.canvasSize.Width + (int) targetX; int dst = dest_offset;
							// only one call instead of manual alphaFactor gymnastics:
							dest_data[dst] = AddPremultipliedBrightness (dest_data[dst], settings.v);
						}
					}
				} else // No dispersion
				  {
					double targetX = sx + settings.scale * settings.focus * xDisplacement;
					double targetY = sy + settings.scale * settings.focus * yDisplacement;

					if (targetX >= 0 && targetX < settings.canvasSize.Width - 1 &&
						targetY >= 0 && targetY < settings.canvasSize.Height - 1) {
						int dest_offset = (int) targetY * settings.canvasSize.Width + (int) targetX;
						int dst = dest_offset;
						dest_data[dst] = AddPremultipliedBrightness (dest_data[dst], settings.v);
					}
				}
			}
		}
	}
	private static double Evaluate (CausticsSettings settings, PerlinNoise3D noiseGenerator, double x, double y)
	{
		double xt = s_rad * x + c_rad * settings.time;
		double tt_eval = c_rad * x - c_rad * settings.time;

		double f = (settings.turbulence == 0.0)
			? noiseGenerator.Noise3 (xt, y, tt_eval)
			: Turbulence2 (xt, y, tt_eval, settings.turbulence, noiseGenerator);

		return f;
	}

	private static double Turbulence2 (double x, double y, double time, double octaves, PerlinNoise3D noiseGenerator)
	{
		double value = 0.0;
		double lacunarity = 2.0;
		double f = 1.0;

		x += 371;
		y += 529;

		for (int i = 0; i < (int) octaves; i++) {
			value += noiseGenerator.Noise3 (x, y, time) / f;
			x *= lacunarity;
			y *= lacunarity;
			f *= 2.0;
		}

		double remainder = octaves - (int) octaves;
		if (remainder != 0)
			value += remainder * noiseGenerator.Noise3 (x, y, time) / f;

		return value;
	}

	public sealed class CausticsData : EffectData // All CausticsData fields 
	{
		[Caption ("Scale"), MinimumValue (1), MaximumValue (300)]
		public double Scale { get; set; } = 32;

		[Caption ("Brightness"), MinimumValue (0), MaximumValue (100)]
		public int Brightness { get; set; } = 10;

		[Caption ("Amount"), MinimumValue (0), MaximumValue (5)]
		public double Amount { get; set; } = 1.0;

		[Caption ("Turbulence"), MinimumValue (0), MaximumValue (10)]
		public double Turbulence { get; set; } = 1.0;

		[Caption ("Dispersion"), MinimumValue (0), MaximumValue (1)]
		public double Dispersion { get; set; } = 0.0;

		[Caption ("Time Offset"), MinimumValue (0), MaximumValue (100)]
		public double TimeOffset { get; set; } = 0.0;

		[Caption ("Samples"), MinimumValue (1), MaximumValue (10)]
		public int Samples { get; set; } = 2;

		[Caption ("Seed")]
		public RandomSeed Seed { get; set; } = new (0);
	}
}

