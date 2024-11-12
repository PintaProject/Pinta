/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Marco Rolappe <m_rolappe@gmx.net>                       //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class TileEffect : BaseEffect
{
	public override string Icon
		=> Resources.Icons.EffectsDistortTile;

	public sealed override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Tile Reflection");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Distort");

	public TileData Data
		=> (TileData) EffectData!;

	private readonly IChromeService chrome;
	public TileEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new TileData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	private sealed record TileSettings (
		Size size,
		float halfWidth,
		float halfHeight,
		float sin,
		float cos,
		float tileScale,
		float adjustedIntensity,
		IReadOnlyList<PointD> antiAliasPoints,
		Func<float, float> waveFunction);

	private TileSettings CreateSettings (ImageSurface source)
	{
		const int ANTI_ALIAS_LEVEL = 4;
		Size size = source.GetSize ();
		RadiansAngle rotationTheta = Data.Rotation.ToRadians ();
		float preliminaryIntensity = Data.Intensity;
		int tileSize = Data.TileSize;
		int antiAliasSample = ANTI_ALIAS_LEVEL * ANTI_ALIAS_LEVEL + 1;
		float sin = (float) Math.Sin (rotationTheta.Radians);
		float cos = (float) Math.Cos (rotationTheta.Radians);
		return new (
			size: size,
			halfWidth: size.Width / 2f,
			halfHeight: size.Height / 2f,
			sin: sin,
			cos: cos,
			tileScale: (float) Math.PI / tileSize,
			adjustedIntensity: preliminaryIntensity * preliminaryIntensity / 10 * Math.Sign (preliminaryIntensity),
			antiAliasPoints: InitializeAntiAliasPoints (ANTI_ALIAS_LEVEL, antiAliasSample, sin, cos),
			waveFunction: GetWaveFunction (Data.WaveType)
		);

		static Func<float, float> GetWaveFunction (TileType waveType)
		{
			return waveType switch {
				TileType.SharpEdges => n => (float) Math.Tan (n),
				TileType.Curved => n => (float) Math.Sin (n),
				_ => throw new InvalidEnumArgumentException (nameof (waveType), (int) waveType, typeof (TileType)),
			};
		}
	}

	private static PointD[] InitializeAntiAliasPoints (
		int antiAliasLevel,
		int antiAliasSamples,
		float sin,
		float cos)
	{
		var result = new PointD[antiAliasSamples];

		for (int i = 0; i < antiAliasSamples; ++i) {

			double x = i * antiAliasLevel / ((double) antiAliasSamples);
			double y = i / (double) antiAliasSamples;

			x -= (int) x;

			// RGSS + rotation to maximize AA quality
			result[i] = new PointD (
				X: (double) (cos * x + sin * y),
				Y: (double) (cos * y - sin * x));
		}
		return result;
	}

	protected override void Render (
		ImageSurface source,
		ImageSurface destination,
		RectangleI roi)
	{
		TileSettings settings = CreateSettings (source);
		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = destination.GetPixelData ();
		foreach (var pixel in Utility.GeneratePixelOffsets (roi, source.GetSize ()))
			dst_data[pixel.memoryOffset] = GetFinalPixelColor (
				source,
				settings,
				sourceData,
				pixel);
	}

	// Algorithm Code Ported From PDN
	private static ColorBgra GetFinalPixelColor (
		ImageSurface source,
		TileSettings settings,
		ReadOnlySpan<ColorBgra> sourceData,
		PixelOffset pixel)
	{
		int b = 0;
		int g = 0;
		int r = 0;
		int a = 0;

		float i = pixel.coordinates.X - settings.halfWidth;
		float j = pixel.coordinates.Y - settings.halfHeight;

		for (int p = 0; p < settings.antiAliasPoints.Count; ++p) {

			PointD pt = settings.antiAliasPoints[p];

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

			int wrappedSampleX = (unwrappedSampleX + settings.size.Width) % settings.size.Width;
			int adjustedSampleX =
				(wrappedSampleX < 0)
				? (wrappedSampleX + settings.size.Width) % settings.size.Width
				: wrappedSampleX;

			int wrappedSampleY = (unwrappedSampleY + settings.size.Height) % settings.size.Height;
			int adjustedSampleY =
				(wrappedSampleY < 0)
				? (wrappedSampleY + settings.size.Height) % settings.size.Height
				: wrappedSampleY;

			PointI samplePosition = new (adjustedSampleX, adjustedSampleY);

			ColorBgra sample = source.GetColorBgra (
				sourceData,
				settings.size.Width,
				samplePosition);

			b += sample.B;
			g += sample.G;
			r += sample.R;
			a += sample.A;
		}

		return ColorBgra.FromBgra (
			b: (byte) (b / settings.antiAliasPoints.Count),
			g: (byte) (g / settings.antiAliasPoints.Count),
			r: (byte) (r / settings.antiAliasPoints.Count),
			a: (byte) (a / settings.antiAliasPoints.Count));
	}

	public sealed class TileData : EffectData
	{
		[Caption ("Rotation"), MinimumValue (-45), MaximumValue (45)]
		public DegreesAngle Rotation { get; set; } = new (30);

		[Caption ("Tile Size"), MinimumValue (2), MaximumValue (200)]
		public int TileSize { get; set; } = 40;

		[Caption ("Intensity"), MinimumValue (-20), MaximumValue (20)]
		public int Intensity { get; set; } = 8;

		[Caption ("Tile Type")]
		public TileType WaveType { get; set; } = TileType.SharpEdges;
	}
}

public enum TileType
{
	[Caption ("Sharp Edges")]
	SharpEdges,

	[Caption ("Curved")]
	Curved,
}
