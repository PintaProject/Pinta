/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Immutable;
using System.Drawing;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public interface IWarpData
{
	int Quality { get; }
	PointD CenterOffset { get; }
	WarpEdgeBehavior EdgeBehavior { get; }
}

public enum WarpEdgeBehavior
{
	[Caption ("Clamp")]
	Clamp,

	[Caption ("Wrap")]
	Wrap,

	[Caption ("Reflect")]
	Reflect,

	[Caption ("Primary")]
	Primary,

	[Caption ("Secondary")]
	Secondary,

	[Caption ("Transparent")]
	Transparent,

	[Caption ("Original")]
	Original,
}

public static class Warp
{
	public readonly record struct TransformData (double X, double Y);

	public delegate TransformData TransformInverter (
		Settings warpSettings,
		TransformData transform);

	public sealed record Settings (
		double xCenterOffset,
		double yCenterOffset,
		ColorBgra primaryColor,
		ColorBgra secondaryColor,
		ImmutableArray<PointD> antiAliasPoints,
		WarpEdgeBehavior edgeBehavior,
		double defaultRadius,
		double defaultRadius2);

	private static bool IsOnSurface (this ImageSurface src, float u, float v)
		=> (u >= 0 && u <= (src.Width - 1) && v >= 0 && v <= (src.Height - 1));

	private static float ReflectCoord (float value, int max)
	{
		bool reflection = false;

		while (value < 0) {
			value += max;
			reflection = !reflection;
		}

		while (value > max) {
			value -= max;
			reflection = !reflection;
		}

		if (reflection)
			value = max - value;

		return value;
	}

	public static Settings CreateSettings (
		IWarpData warpData,
		RectangleI selectionBounds,
		IPaletteService palette)
	{
		int antiAliasSampleCount = warpData.Quality * warpData.Quality;
		double defaultRadius = Math.Min (selectionBounds.Width, selectionBounds.Height) * 0.5;
		ImmutableArray<PointD> antiAliasPoints = Utility.GetRgssOffsets (antiAliasSampleCount, warpData.Quality);
		return new (
			xCenterOffset: selectionBounds.Left + (selectionBounds.Width * (1.0 + warpData.CenterOffset.X) * 0.5),
			yCenterOffset: selectionBounds.Top + (selectionBounds.Height * (1.0 + warpData.CenterOffset.Y) * 0.5),
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
		double relativeY = targetPixel.coordinates.Y - settings.yCenterOffset;
		Span<ColorBgra> samples = stackalloc ColorBgra[settings.antiAliasPoints.Length];
		double relativeX = targetPixel.coordinates.X - settings.xCenterOffset;
		int sampleCount = 0;
		for (int p = 0; p < settings.antiAliasPoints.Length; ++p) {

			TransformData initialTd = new (
				X: relativeX + settings.antiAliasPoints[p].X,
				Y: relativeY - settings.antiAliasPoints[p].Y);

			TransformData td = transformInverter (settings, initialTd);

			PointF preliminarySample = new (
				x: (float) (td.X + settings.xCenterOffset),
				y: (float) (td.Y + settings.yCenterOffset));

			samples[sampleCount] = GetSample (
				settings,
				src,
				originalColor,
				targetPixel,
				preliminarySample);

			++sampleCount;
		}
		return ColorBgra.Blend (samples[..sampleCount]);
	}

	private static ColorBgra GetSample (
		Settings settings,
		ImageSurface src,
		ColorBgra originalColor,
		PixelOffset targetPixel,
		PointF preliminarySample)
	{
		if (src.IsOnSurface (preliminarySample.X, preliminarySample.Y))
			return src.GetBilinearSample (preliminarySample.X, preliminarySample.Y);

		return settings.edgeBehavior switch {
			WarpEdgeBehavior.Clamp => src.GetBilinearSampleClamped (preliminarySample.X, preliminarySample.Y),
			WarpEdgeBehavior.Wrap => src.GetBilinearSampleWrapped (preliminarySample.X, preliminarySample.Y),
			WarpEdgeBehavior.Reflect => src.GetBilinearSampleClamped (ReflectCoord (preliminarySample.X, src.Width), ReflectCoord (preliminarySample.Y, src.Height)),
			WarpEdgeBehavior.Primary => settings.primaryColor,
			WarpEdgeBehavior.Secondary => settings.secondaryColor,
			WarpEdgeBehavior.Transparent => ColorBgra.Transparent,
			WarpEdgeBehavior.Original => originalColor,
			_ => throw new ArgumentException ($"{nameof (settings.edgeBehavior)} is out of range", nameof (settings)),
		};
	}
}
