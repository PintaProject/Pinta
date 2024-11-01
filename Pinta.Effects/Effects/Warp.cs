/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
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

public interface IWarpEffect<TEffectData> where TEffectData : EffectData, IWarpData
{
	Warp.TransformData InverseTransform (
		Warp.TransformData data,
		WarpSettings settings);

	TEffectData Data { get; }

	// TODO: Remove service dependencies
	LivePreviewManager LivePreview { get; }
	IPaletteService Palette { get; }
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

public sealed record WarpSettings (
	double xCenterOffset,
	double yCenterOffset,
	ColorBgra primaryColor,
	ColorBgra secondaryColor,
	int antiAliasSampleCount,
	WarpEdgeBehavior edgeBehavior,
	double defaultRadius,
	double defaultRadius2);

public static class Warp
{
	public readonly record struct TransformData (double X, double Y);

	private static ColorBgra GetSample (
		this WarpSettings settings,
		ImageSurface src,
		ReadOnlySpan<ColorBgra> src_data,
		PointI target,
		PointF preliminarySample)
	{
		if (IsOnSurface (src, preliminarySample.X, preliminarySample.Y))
			return src.GetBilinearSample (preliminarySample.X, preliminarySample.Y);

		return settings.edgeBehavior switch {
			WarpEdgeBehavior.Clamp => src.GetBilinearSampleClamped (preliminarySample.X, preliminarySample.Y),
			WarpEdgeBehavior.Wrap => src.GetBilinearSampleWrapped (preliminarySample.X, preliminarySample.Y),
			WarpEdgeBehavior.Reflect => src.GetBilinearSampleClamped (ReflectCoord (preliminarySample.X, src.Width), ReflectCoord (preliminarySample.Y, src.Height)),
			WarpEdgeBehavior.Primary => settings.primaryColor,
			WarpEdgeBehavior.Secondary => settings.secondaryColor,
			WarpEdgeBehavior.Transparent => ColorBgra.Transparent,
			WarpEdgeBehavior.Original => src_data[target.Y * src.Width + target.X],
			_ => settings.primaryColor,
		};
	}

	private static bool IsOnSurface (ImageSurface src, float u, float v)
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

	public static void RenderWarpEffect<TEffectData> (
		this IWarpEffect<TEffectData> effect,
		ImageSurface src,
		ImageSurface dst,
		ReadOnlySpan<RectangleI> rois
	)
		where TEffectData : EffectData, IWarpData
	{
		WarpSettings settings = CreateSettings (effect);

		Span<PointD> aaPoints = stackalloc PointD[settings.antiAliasSampleCount];
		Utility.GetRgssOffsets (aaPoints, settings.antiAliasSampleCount, effect.Data.Quality);

		Span<ColorBgra> dst_data = dst.GetPixelData ();
		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();

		foreach (RectangleI rect in rois) {
			foreach (var pixel in Utility.GeneratePixelOffsets (rect, src.GetSize ())) {
				double relativeY = pixel.coordinates.Y - settings.yCenterOffset;
				dst_data[pixel.memoryOffset] = effect.GetPixelColor (
					settings,
					src,
					src_data,
					aaPoints,
					relativeY,
					pixel.coordinates);
			}
		}
	}

	private static WarpSettings CreateSettings<TEffectData> (IWarpEffect<TEffectData> effect) where TEffectData : EffectData, IWarpData
	{
		RectangleI selection = effect.LivePreview.RenderBounds;
		double defaultRadius = Math.Min (selection.Width, selection.Height) * 0.5;
		return new (
			xCenterOffset: selection.Left + (selection.Width * (1.0 + effect.Data.CenterOffset.X) * 0.5),
			yCenterOffset: selection.Top + (selection.Height * (1.0 + effect.Data.CenterOffset.Y) * 0.5),
			primaryColor: effect.Palette.PrimaryColor.ToColorBgra (),
			secondaryColor: effect.Palette.SecondaryColor.ToColorBgra (),
			antiAliasSampleCount: effect.Data.Quality * effect.Data.Quality,
			edgeBehavior: effect.Data.EdgeBehavior,
			defaultRadius: defaultRadius,
			defaultRadius2: defaultRadius * defaultRadius);
	}

	private static ColorBgra GetPixelColor<TEffectData> (
		this IWarpEffect<TEffectData> effect,
		WarpSettings settings,
		ImageSurface src,
		ReadOnlySpan<ColorBgra> src_data,
		ReadOnlySpan<PointD> aaPoints,
		double relativeY,
		PointI target
	)
		where TEffectData : EffectData, IWarpData
	{
		Span<ColorBgra> samples = stackalloc ColorBgra[settings.antiAliasSampleCount];
		double relativeX = target.X - settings.xCenterOffset;
		int sampleCount = 0;
		for (int p = 0; p < settings.antiAliasSampleCount; ++p) {

			Warp.TransformData initialTd = new (
				X: relativeX + aaPoints[p].X,
				Y: relativeY - aaPoints[p].Y);

			Warp.TransformData td = effect.InverseTransform (initialTd, settings);

			PointF preliminarySample = new (
				x: (float) (td.X + settings.xCenterOffset),
				y: (float) (td.Y + settings.yCenterOffset));

			samples[sampleCount] = settings.GetSample (
				src,
				src_data,
				target,
				preliminarySample);

			++sampleCount;
		}
		return ColorBgra.Blend (samples[..sampleCount]);
	}
}
