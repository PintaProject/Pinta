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
			time: Data.Time);
	}

	protected override void Render (ImageSurface src, ImageSurface dest, RectangleI roi)
	{
		CausticsSettings settings = CreateSettings (dest);
		Random jitterRand = new (settings.seed.Value);
		SeededPerlinNoise noiseGenerator = new (settings.seed.Value);

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
							int dest_offset = (int) targetY * settings.canvasSize.Width + (int) targetX;
							ColorBgra pixelColor = dest_data[dest_offset];

							byte r = pixelColor.R;
							byte g = pixelColor.G;
							byte b = pixelColor.B;

							if (channel == 2)
								r = (byte) Math.Min (255, r + settings.v);
							else if (channel == 1)
								g = (byte) Math.Min (255, g + settings.v);
							else
								b = (byte) Math.Min (255, b + settings.v);

							dest_data[dest_offset] = ColorBgra.FromBgra (b, g, r, pixelColor.A);
						}
					}
				} else // No dispersion
				  {
					double targetX = sx + settings.scale * settings.focus * xDisplacement;
					double targetY = sy + settings.scale * settings.focus * yDisplacement;

					if (targetX >= 0 && targetX < settings.canvasSize.Width - 1 &&
						targetY >= 0 && targetY < settings.canvasSize.Height - 1) {
						int dest_offset = (int) targetY * settings.canvasSize.Width + (int) targetX;
						ColorBgra pixelColor = dest_data[dest_offset];

						byte r = (byte) Math.Min (255, pixelColor.R + settings.v);
						byte g = (byte) Math.Min (255, pixelColor.G + settings.v);
						byte b = (byte) Math.Min (255, pixelColor.B + settings.v);

						dest_data[dest_offset] = ColorBgra.FromBgra (b, g, r, pixelColor.A);
					}
				}
			}
		}
	}
	private static double Evaluate (CausticsSettings settings, SeededPerlinNoise noiseGenerator, double x, double y)
	{
		double xt = s_rad * x + c_rad * settings.time;
		double tt_eval = c_rad * x - c_rad * settings.time;

		double f = (settings.turbulence == 0.0)
			? noiseGenerator.Noise3 (xt, y, tt_eval)
			: Turbulence2 (xt, y, tt_eval, settings.turbulence, noiseGenerator);

		return f;
	}

	private static double Turbulence2 (double x, double y, double time, double octaves, SeededPerlinNoise noiseGenerator)
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

		[Caption ("Time (for animation)"), MinimumValue (0), MaximumValue (100)]
		public double Time { get; set; } = 0.0;

		[Caption ("Samples"), MinimumValue (1), MaximumValue (10)]
		public int Samples { get; set; } = 2;

		[Caption ("Seed")]
		public RandomSeed Seed { get; set; } = new (0);
	}
}

internal sealed class SeededPerlinNoise
{
	private const int GRADIENT_SIZE = 256;

	private readonly int[] permutation = new int[GRADIENT_SIZE * 2];
	private readonly double[] gradient_x = new double[GRADIENT_SIZE];
	private readonly double[] gradient_y = new double[GRADIENT_SIZE];
	private readonly double[] gradient_z = new double[GRADIENT_SIZE];

	// The constructor now takes a seed to generate a unique, repeatable noise pattern.
	public SeededPerlinNoise (int seed)
	{
		var randomGen = new Random (seed);
		int[] p = new int[GRADIENT_SIZE];

		for (int i = 0; i < GRADIENT_SIZE; i++)
			p[i] = i;

		for (int i = 0; i < GRADIENT_SIZE; i++) {
			int j = randomGen.Next (GRADIENT_SIZE);
			(p[i], p[j]) = (p[j], p[i]);
		}

		for (int i = 0; i < GRADIENT_SIZE; i++) {
			permutation[i] = permutation[i + GRADIENT_SIZE] = p[i];
			double invLen;
			do {
				gradient_x[i] = randomGen.NextDouble () * 2.0 - 1.0;
				gradient_y[i] = randomGen.NextDouble () * 2.0 - 1.0;
				gradient_z[i] = randomGen.NextDouble () * 2.0 - 1.0;
				invLen = gradient_x[i] * gradient_x[i] + gradient_y[i] * gradient_y[i] + gradient_z[i] * gradient_z[i];
			} while (invLen == 0);
			invLen = 1.0 / Math.Sqrt (invLen);
			gradient_x[i] *= invLen;
			gradient_y[i] *= invLen;
			gradient_z[i] *= invLen;
		}
	}
	private double Grad (int hash, double x, double y, double z)
	{
		int h = hash & (GRADIENT_SIZE - 1);
		return gradient_x[h] * x + gradient_y[h] * y + gradient_z[h] * z;
	}

	public double Noise3 (double x, double y, double z)
	{
		int X = (int) Math.Floor (x) & (GRADIENT_SIZE - 1);
		int Y = (int) Math.Floor (y) & (GRADIENT_SIZE - 1);
		int Z = (int) Math.Floor (z) & (GRADIENT_SIZE - 1);

		x -= Math.Floor (x);
		y -= Math.Floor (y);
		z -= Math.Floor (z);

		double u = PerlinNoise.Fade (x);
		double v = PerlinNoise.Fade (y);
		double w = PerlinNoise.Fade (z);

		int A = permutation[X] + Y;
		int AA = permutation[A] + Z;
		int AB = permutation[A + 1] + Z;
		int B = permutation[X + 1] + Y;
		int BA = permutation[B] + Z;
		int BB = permutation[B + 1] + Z;

		return Mathematics.Lerp (
			Mathematics.Lerp (
				Mathematics.Lerp (
					Grad (permutation[AA], x, y, z),
					Grad (permutation[BA], x - 1, y, z),
					u),
				Mathematics.Lerp (
					Grad (
						permutation[AB],
						x,
						y - 1,
						z),
					Grad (
						permutation[BB],
						x - 1,
						y - 1,
						z),
					u),
				v),
			Mathematics.Lerp (
				Mathematics.Lerp (
					Grad (
						permutation[AA + 1],
						x,
						y,
						z - 1),
					Grad (
						permutation[BA + 1],
						x - 1,
						y,
						z - 1),
					u),
				Mathematics.Lerp (
					Grad (
						permutation[AB + 1],
						x,
						y - 1,
						z - 1),
					Grad (
						permutation[BB + 1],
						x - 1,
						y - 1,
						z - 1),
					u),
				v),
			w);
	}
}
