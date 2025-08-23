/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public static class Warp
{
	public interface IEffectData
	{
		int Quality { get; }
		CenterOffset<double> CenterOffset { get; }
		EdgeBehavior EdgeBehavior { get; }
	}

	public delegate PointD TransformInverter (Settings warpSettings, PointD transform);

	public sealed record Settings (
		PointD centerOffset,
		ColorBgra primaryColor,
		ColorBgra secondaryColor,
		IReadOnlyList<PointD> antiAliasPoints,
		EdgeBehavior edgeBehavior,
		double defaultRadius,
		double defaultRadius2);

	private static bool IsOnSurface (this ImageSurface source, float u, float v)
		=> (u >= 0 && u <= (source.Width - 1) && v >= 0 && v <= (source.Height - 1));

	public static Settings CreateSettings (
		IEffectData warpData,
		RectangleI selectionBounds,
		IPaletteService palette)
	{
		int antiAliasSampleCount = warpData.Quality * warpData.Quality;
		double defaultRadius = Math.Min (selectionBounds.Width, selectionBounds.Height) * 0.5;
		IReadOnlyList<PointD> antiAliasPoints = Utility.GetRgssOffsets (antiAliasSampleCount, warpData.Quality);
		PointD centerOffset = new (
			X: selectionBounds.Left + (selectionBounds.Width * (1.0 + warpData.CenterOffset.Horizontal) * 0.5),
			Y: selectionBounds.Top + (selectionBounds.Height * (1.0 + warpData.CenterOffset.Vertical) * 0.5));
		return new (
			centerOffset: centerOffset,
			primaryColor: palette.PrimaryColor.ToColorBgra (),
			secondaryColor: palette.SecondaryColor.ToColorBgra (),
			antiAliasPoints: antiAliasPoints,
			edgeBehavior: warpData.EdgeBehavior,
			defaultRadius: defaultRadius,
			defaultRadius2: defaultRadius * defaultRadius);
	}

	public static ColorBgra GetPixelColor (
		Settings settings,
		TransformInverter transformInverter,
		ImageSurface source,
		ColorBgra originalColor,
		PixelOffset targetPixel)
	{
		PointD relative = new (
			X: targetPixel.coordinates.X - settings.centerOffset.X,
			Y: targetPixel.coordinates.Y - settings.centerOffset.Y);

		Span<ColorBgra> samples = stackalloc ColorBgra[settings.antiAliasPoints.Count];

		int sampleCount = 0;

		for (int p = 0; p < settings.antiAliasPoints.Count; ++p) {

			PointD initialTd = new (
				X: relative.X + settings.antiAliasPoints[p].X,
				Y: relative.Y - settings.antiAliasPoints[p].Y);

			PointD td = transformInverter (settings, initialTd);

			PointF preliminarySample = new (
				X: (float) (td.X + settings.centerOffset.X),
				Y: (float) (td.Y + settings.centerOffset.Y));

			samples[sampleCount] = GetSample (
				settings,
				source,
				originalColor,
				preliminarySample);

			++sampleCount;
		}

		return ColorBgra.Blend (
			colors: samples[..sampleCount],
			fallback: ColorBgra.Transparent);
	}

	private static ColorBgra GetSample (
		Settings settings,
		ImageSurface source,
		ColorBgra originalColor,
		PointF preliminarySample)
	{
		if (source.IsOnSurface (preliminarySample.X, preliminarySample.Y))
			return source.GetBilinearSample (preliminarySample.X, preliminarySample.Y);

		return settings.edgeBehavior switch {
			EdgeBehavior.Clamp => source.GetBilinearSampleClamped (preliminarySample.X, preliminarySample.Y),
			EdgeBehavior.Wrap => source.GetBilinearSampleWrapped (preliminarySample.X, preliminarySample.Y),
			EdgeBehavior.Reflect => source.GetBilinearSampleReflected (preliminarySample.X, preliminarySample.Y),
			EdgeBehavior.Primary => settings.primaryColor,
			EdgeBehavior.Secondary => settings.secondaryColor,
			EdgeBehavior.Transparent => ColorBgra.Transparent,
			EdgeBehavior.Original => originalColor,
			_ => throw new ArgumentException ($"{nameof (settings.edgeBehavior)} is out of range", nameof (settings)),
		};
	}
}
