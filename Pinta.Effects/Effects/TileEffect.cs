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
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class TileEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsDistortTile;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Tile Reflection");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Distort");

	public TileData Data => (TileData) EffectData!;

	private readonly IChromeService chrome;

	public TileEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();

		EffectData = new TileData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	#region Algorithm Code Ported From PDN

	private sealed record TileSettings (
		int width,
		int height,
		float hw,
		float hh,
		float sin,
		float cos,
		float scale,
		float intensity,
		int aaLevel,
		int aaSamples,
		int src_width);
	private TileSettings CreateSettings (ImageSurface src, ImageSurface dst)
	{
		int width = dst.Width;
		int height = dst.Height;
		RadiansAngle rotationTheta = Data.Rotation.ToRadians ();
		float preliminaryIntensity = Data.Intensity;
		int tileSize = Data.TileSize;

		int aaLevel = 4;

		return new (
			width: width,
			height: height,
			hw: width / 2f,
			hh: height / 2f,
			sin: (float) Math.Sin (rotationTheta.Radians),
			cos: (float) Math.Cos (rotationTheta.Radians),
			scale: (float) Math.PI / tileSize,
			intensity: preliminaryIntensity * preliminaryIntensity / 10 * Math.Sign (preliminaryIntensity),
			aaLevel: aaLevel,
			aaSamples: aaLevel * aaLevel + 1,
			src_width: src.Width
		);
	}

	private static void InitializeAntiAliasPoints (TileSettings settings, Span<PointD> destination)
	{
		for (int i = 0; i < settings.aaSamples; ++i) {
			double x = i * settings.aaLevel / ((double) settings.aaSamples);
			double y = i / (double) settings.aaSamples;

			x -= (int) x;

			// RGSS + rotation to maximize AA quality
			destination[i] = new PointD (
				X: (double) (settings.cos * x + settings.sin * y),
				Y: (double) (settings.cos * y - settings.sin * x)
			);
		}
	}

	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		TileSettings settings = CreateSettings (src, dst);

		Span<PointD> aaPoints = stackalloc PointD[settings.aaSamples];
		InitializeAntiAliasPoints (settings, aaPoints);

		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dst.GetPixelData ();

		foreach (var rect in rois) {
			foreach (var pixel in Utility.GeneratePixelOffsets (rect, src.GetSize ())) {
				float j = pixel.coordinates.Y - settings.hh;
				dst_data[pixel.memoryOffset] = GetFinalPixelColor (src, settings, aaPoints, src_data, j, pixel.coordinates.X);
			}
		}
	}

	private static ColorBgra GetFinalPixelColor (ImageSurface src, TileSettings settings, ReadOnlySpan<PointD> aaPoints, ReadOnlySpan<ColorBgra> src_data, float j, int x)
	{
		int b = 0;
		int g = 0;
		int r = 0;
		int a = 0;

		float i = x - settings.hw;

		for (int p = 0; p < settings.aaSamples; ++p) {
			PointD pt = aaPoints[p];

			float u = i + (float) pt.X;
			float v = j - (float) pt.Y;

			float s = settings.cos * u + settings.sin * v;
			float t = -settings.sin * u + settings.cos * v;

			s += settings.intensity * (float) Math.Tan (s * settings.scale);
			t += settings.intensity * (float) Math.Tan (t * settings.scale);
			u = settings.cos * s - settings.sin * t;
			v = settings.sin * s + settings.cos * t;

			int xSample = (int) (settings.hw + u);
			int ySample = (int) (settings.hh + v);

			xSample = (xSample + settings.width) % settings.width;
			// This makes it a little faster
			if (xSample < 0)
				xSample = (xSample + settings.width) % settings.width;

			ySample = (ySample + settings.height) % settings.height;
			// This makes it a little faster
			if (ySample < 0)
				ySample = (ySample + settings.height) % settings.height;

			PointI samplePosition = new (xSample, ySample);

			ColorBgra sample = src.GetColorBgra (src_data, settings.src_width, samplePosition);

			b += sample.B;
			g += sample.G;
			r += sample.R;
			a += sample.A;
		}

		return ColorBgra.FromBgra (
			b: (byte) (b / settings.aaSamples),
			g: (byte) (g / settings.aaSamples),
			r: (byte) (r / settings.aaSamples),
			a: (byte) (a / settings.aaSamples)
		);
	}
	#endregion


	public sealed class TileData : EffectData
	{
		[Caption ("Rotation"), MinimumValue (-45), MaximumValue (45)]
		public DegreesAngle Rotation { get; set; } = new (30);

		[Caption ("Tile Size"), MinimumValue (2), MaximumValue (200)]
		public int TileSize { get; set; } = 40;

		[Caption ("Intensity"), MinimumValue (-20), MaximumValue (20)]
		public int Intensity { get; set; } = 8;
	}
}
