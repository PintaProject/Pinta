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
	private readonly IPaletteService palette;
	private readonly IWorkspaceService workspace;
	public TileEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		palette = services.GetService<IPaletteService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new TileData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	private sealed record TileSettings (
		Size size,
		float halfWidth,
		float halfHeight,
		float sin,
		float cos,
		float tileScale,
		float adjustedIntensity,
		EdgeBehavior edgeBehavior,
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
		float sin = (float) Math.Sin (-rotationTheta.Radians);
		float cos = (float) Math.Cos (-rotationTheta.Radians);
		return new (
			size: size,
			halfWidth: size.Width / 2f,
			halfHeight: size.Height / 2f,
			sin: sin,
			cos: cos,
			tileScale: (float) Math.PI / tileSize,
			adjustedIntensity: preliminaryIntensity * preliminaryIntensity / 10 * Math.Sign (preliminaryIntensity),
			edgeBehavior: Data.EdgeBehavior,
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
		Span<ColorBgra> destinationData = destination.GetPixelData ();
		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, source.GetSize ()))
			destinationData[pixel.memoryOffset] = GetFinalPixelColor (
				source,
				settings,
				sourceData,
				pixel);
	}

	// Algorithm Code Ported From PDN
	private ColorBgra GetFinalPixelColor (
		ImageSurface source,
		TileSettings settings,
		ReadOnlySpan<ColorBgra> sourceData,
		PixelOffset pixel)
	{
		ColorBgra original = sourceData[pixel.memoryOffset];

		float i = pixel.coordinates.X - settings.halfWidth;
		float j = pixel.coordinates.Y - settings.halfHeight;

		ColorBgra.Blender aggregate = new ();

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
			float preliminaryX = settings.halfWidth + finalU;
			float preliminaryY = settings.halfHeight + finalV;

			aggregate += GetSample (
				settings,
				source,
				sourceData,
				original,
				preliminaryX,
				preliminaryY);
		}

		return aggregate.Blend ();
	}

	private ColorBgra GetSample (
		TileSettings settings,
		ImageSurface source,
		ReadOnlySpan<ColorBgra> sourceData,
		ColorBgra original,
		float preliminaryX,
		float preliminaryY)
	{
		if (IsOnSurface (settings, preliminaryX, preliminaryY)) {
			int floorX = (int) preliminaryX;
			int floorY = (int) preliminaryY;
			int rowOffset = floorY * settings.size.Width;
			int columnOffset = floorX;
			int pixelOffset = rowOffset + columnOffset;
			return sourceData[pixelOffset];
		}

		return settings.edgeBehavior switch {
			EdgeBehavior.Clamp => source.GetBilinearSampleClamped (sourceData, settings.size.Width, settings.size.Height, preliminaryX, preliminaryY),
			EdgeBehavior.Wrap => source.GetBilinearSampleWrapped (sourceData, settings.size.Width, settings.size.Height, preliminaryX, preliminaryY),
			EdgeBehavior.Reflect => source.GetBilinearSampleReflected (sourceData, settings.size.Width, settings.size.Height, preliminaryX, preliminaryY),
			EdgeBehavior.Primary => palette.PrimaryColor.ToColorBgra (),
			EdgeBehavior.Secondary => palette.SecondaryColor.ToColorBgra (),
			EdgeBehavior.Transparent => ColorBgra.Transparent,
			EdgeBehavior.Original => original,
			_ => throw new ArgumentException ($"{nameof (settings.edgeBehavior)} is out of range", nameof (settings)),
		};
	}

	private static bool IsOnSurface (TileSettings settings, float u, float v)
		=> (u >= 0 && u <= (settings.size.Width - 1) && v >= 0 && v <= (settings.size.Height - 1));

	public sealed class TileData : EffectData
	{
		[Caption ("Rotation")]
		[MinimumValue (-45), MaximumValue (45)]
		public DegreesAngle Rotation { get; set; } = new (30);

		[Caption ("Tile Size")]
		[MinimumValue (2), MaximumValue (200)]
		public int TileSize { get; set; } = 40;

		[Caption ("Intensity")]
		[MinimumValue (-20), MaximumValue (20)]
		public int Intensity { get; set; } = 8;

		[Caption ("Tile Type")]
		public TileType WaveType { get; set; } = TileType.SharpEdges;

		[Caption ("Edge Behavior")]
		public EdgeBehavior EdgeBehavior { get; set; } = EdgeBehavior.Wrap;
	}
}

public enum TileType
{
	[Caption ("Sharp Edges")]
	SharpEdges,

	[Caption ("Curved")]
	Curved,
}
