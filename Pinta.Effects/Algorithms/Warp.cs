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

	public readonly record struct TransformData (double X, double Y);

	public delegate TransformData TransformInverter (
		Settings warpSettings,
		TransformData transform);

	public sealed record Settings (
		PointD centerOffset,
		ColorBgra primaryColor,
		ColorBgra secondaryColor,
		IReadOnlyList<PointD> antiAliasPoints,
		EdgeBehavior edgeBehavior,
		double defaultRadius,
		double defaultRadius2);

	private static bool IsOnSurface (this ImageSurface src, float u, float v)
		=> (u >= 0 && u <= (src.Width - 1) && v >= 0 && v <= (src.Height - 1));

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
		ImageSurface src,
		ColorBgra originalColor,
		PixelOffset targetPixel)
	{
		PointD relative = new (
			X: targetPixel.coordinates.X - settings.centerOffset.X,
			Y: targetPixel.coordinates.Y - settings.centerOffset.Y);
		Span<ColorBgra> samples = stackalloc ColorBgra[settings.antiAliasPoints.Count];
		int sampleCount = 0;
		for (int p = 0; p < settings.antiAliasPoints.Count; ++p) {

			TransformData initialTd = new (
				X: relative.X + settings.antiAliasPoints[p].X,
				Y: relative.Y - settings.antiAliasPoints[p].Y);

			TransformData td = transformInverter (settings, initialTd);

			PointF preliminarySample = new (
				X: (float) (td.X + settings.centerOffset.X),
				Y: (float) (td.Y + settings.centerOffset.Y));

			samples[sampleCount] = GetSample (
				settings,
				src,
				originalColor,
				preliminarySample);

			++sampleCount;
		}
		return ColorBgra.Blend (samples[..sampleCount]);
	}

	private static ColorBgra GetSample (
		Settings settings,
		ImageSurface src,
		ColorBgra originalColor,
		PointF preliminarySample)
	{
		if (src.IsOnSurface (preliminarySample.X, preliminarySample.Y))
			return src.GetBilinearSample (preliminarySample.X, preliminarySample.Y);

		return settings.edgeBehavior switch {
			EdgeBehavior.Clamp => src.GetBilinearSampleClamped (preliminarySample.X, preliminarySample.Y),
			EdgeBehavior.Wrap => src.GetBilinearSampleWrapped (preliminarySample.X, preliminarySample.Y),
			EdgeBehavior.Reflect => src.GetBilinearSampleReflected (preliminarySample.X, preliminarySample.Y),
			EdgeBehavior.Primary => settings.primaryColor,
			EdgeBehavior.Secondary => settings.secondaryColor,
			EdgeBehavior.Transparent => ColorBgra.Transparent,
			EdgeBehavior.Original => originalColor,
			_ => throw new ArgumentException ($"{nameof (settings.edgeBehavior)} is out of range", nameof (settings)),
		};
	}
}
