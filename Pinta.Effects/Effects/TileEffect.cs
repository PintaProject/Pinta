/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Marco Rolappe <m_rolappe@gmx.net>                       //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
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
		float halfWidth,
		float halfHeight,
		float sin,
		float cos,
		float tileScale,
		float adjustedIntensity,
		int antiAliasLevel,
		int antiAliasSamples,
		int src_width,
		Func<float, float> waveFunction);
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
			halfWidth: width / 2f,
			halfHeight: height / 2f,
			sin: (float) Math.Sin (rotationTheta.Radians),
			cos: (float) Math.Cos (rotationTheta.Radians),
			tileScale: MathF.PI / tileSize,
			adjustedIntensity: preliminaryIntensity * preliminaryIntensity / 10 * Math.Sign (preliminaryIntensity),
			antiAliasLevel: aaLevel,
			antiAliasSamples: aaLevel * aaLevel + 1,
			src_width: src.Width,
			waveFunction: GetWaveFunction (Data.WaveType)
		);

		static Func<float, float> GetWaveFunction (WaveType waveType)
		{
			return waveType switch {
				WaveType.Sine => MathF.Sin,
				WaveType.Cosine => MathF.Cos,
				WaveType.Tangent => MathF.Tan,
				WaveType.Square => n => Math.Sign (MathF.Sin (n)),
				WaveType.Sawtooth => n => 2f * (n / (2f * MathF.PI) - MathF.Floor (0.5f + n / (2f * MathF.PI))),
				_ => throw new InvalidEnumArgumentException (nameof (waveType), (int) waveType, typeof (WaveType)),
			};
		}
	}

	private static void InitializeAntiAliasPoints (TileSettings settings, Span<PointD> destination)
	{
		for (int i = 0; i < settings.antiAliasSamples; ++i) {

			double x = i * settings.antiAliasLevel / ((double) settings.antiAliasSamples);
			double y = i / (double) settings.antiAliasSamples;

			x -= (int) x;

			// RGSS + rotation to maximize AA quality
			destination[i] = new PointD (
				X: (double) (settings.cos * x + settings.sin * y),
				Y: (double) (settings.cos * y - settings.sin * x));
		}
	}

	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		TileSettings settings = CreateSettings (src, dst);

		Span<PointD> aaPoints = stackalloc PointD[settings.antiAliasSamples];
		InitializeAntiAliasPoints (settings, aaPoints);

		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dst.GetPixelData ();

		foreach (var rect in rois) {
			foreach (var pixel in Utility.GeneratePixelOffsets (rect, src.GetSize ())) {
				float j = pixel.coordinates.Y - settings.halfHeight;
				dst_data[pixel.memoryOffset] = GetFinalPixelColor (src, settings, aaPoints, src_data, j, pixel.coordinates.X);
			}
		}
	}

	private static ColorBgra GetFinalPixelColor (
		ImageSurface src,
		TileSettings settings,
		ReadOnlySpan<PointD> aaPoints,
		ReadOnlySpan<ColorBgra> src_data,
		float j,
		int x)
	{
		int b = 0;
		int g = 0;
		int r = 0;
		int a = 0;

		float i = x - settings.halfWidth;

		for (int p = 0; p < settings.antiAliasSamples; ++p) {

			PointD pt = aaPoints[p];

			// Initial coordinates after applying anti-aliasing offsets

			float initialU = i + (float) pt.X;
			float initialV = j - (float) pt.Y;

			// Rotate coordinates to align with the effect's orientation

			float rotatedS = settings.cos * initialU + settings.sin * initialV;
			float rotatedT = -settings.sin * initialU + settings.cos * initialV;

			// Apply wave function

			float functionS = settings.waveFunction (rotatedS * settings.tileScale);
			float functionT = settings.waveFunction (rotatedT * settings.tileScale);

			// Apply intensity transformation to create the tile effect

			float transformedS = rotatedS + settings.adjustedIntensity * functionS;
			float transformedT = rotatedT + settings.adjustedIntensity * functionT;

			// Rotate back to the original coordinate space

			float finalU = settings.cos * transformedS - settings.sin * transformedT;
			float finalV = settings.sin * transformedS + settings.cos * transformedT;

			// Translate back to image coordinates

			int unwrappedSampleX = (int) (settings.halfWidth + finalU);
			int unwrappedSampleY = (int) (settings.halfHeight + finalV);

			// Ensure coordinates wrap around the image dimensions

			int wrappedSampleX = (unwrappedSampleX + settings.width) % settings.width;
			int adjustedSampleX =
				(wrappedSampleX < 0)
				? (wrappedSampleX + settings.width) % settings.width
				: wrappedSampleX;

			int wrappedSampleY = (unwrappedSampleY + settings.height) % settings.height;
			int adjustedSampleY =
				(wrappedSampleY < 0)
				? (wrappedSampleY + settings.height) % settings.height
				: wrappedSampleY;

			PointI samplePosition = new (adjustedSampleX, adjustedSampleY);

			ColorBgra sample = src.GetColorBgra (src_data, settings.src_width, samplePosition);

			b += sample.B;
			g += sample.G;
			r += sample.R;
			a += sample.A;
		}

		return ColorBgra.FromBgra (
			b: (byte) (b / settings.antiAliasSamples),
			g: (byte) (g / settings.antiAliasSamples),
			r: (byte) (r / settings.antiAliasSamples),
			a: (byte) (a / settings.antiAliasSamples));
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

		[Caption ("Wave Type")]
		public WaveType WaveType { get; set; } = WaveType.Tangent;
	}

	public enum WaveType
	{
		Sine,
		Cosine,
		Tangent,
		Square,
		Sawtooth,
	}
}
