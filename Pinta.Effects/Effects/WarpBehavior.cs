using System.Drawing;
using System;
using Pinta.Core;
using Pinta.Gui.Widgets;
using Cairo;

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
	double x_center_offset,
	double y_center_offset,
	ColorBgra colPrimary,
	ColorBgra colSecondary,
	ColorBgra colTransparent,
	int aaSampleCount,
	WarpEdgeBehavior edgeBehavior,
	double defaultRadius,
	double defaultRadius2);

public static class Warp
{
	public readonly record struct TransformData (double X, double Y);

	public static ColorBgra GetSample (
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
			WarpEdgeBehavior.Primary => settings.colPrimary,
			WarpEdgeBehavior.Secondary => settings.colSecondary,
			WarpEdgeBehavior.Transparent => settings.colTransparent,
			WarpEdgeBehavior.Original => src_data[target.Y * src.Width + target.X],
			_ => settings.colPrimary,
		};
	}

	private static bool IsOnSurface (ImageSurface src, float u, float v)
		=> (u >= 0 && u <= (src.Width - 1) && v >= 0 && v <= (src.Height - 1));

	public static float ReflectCoord (float value, int max)
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

		Span<PointD> aaPoints = stackalloc PointD[settings.aaSampleCount];
		Utility.GetRgssOffsets (aaPoints, settings.aaSampleCount, effect.Data.Quality);

		Span<ColorBgra> dst_data = dst.GetPixelData ();
		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();

		foreach (RectangleI rect in rois) {
			foreach (var pixel in Utility.GeneratePixelOffsets (rect, src.GetSize ())) {
				double relativeY = pixel.coordinates.Y - settings.y_center_offset;
				dst_data[pixel.memoryOffset] = effect.GetPixelColor (settings, src, src_data, aaPoints, relativeY, pixel.coordinates);
			}
		}
	}

	private static WarpSettings CreateSettings<TEffectData> (IWarpEffect<TEffectData> effect) where TEffectData : EffectData, IWarpData
	{
		RectangleI selection = PintaCore.LivePreview.RenderBounds; // TODO: Remove
		double defaultRadius = Math.Min (selection.Width, selection.Height) * 0.5;
		return new (
			x_center_offset: selection.Left + (selection.Width * (1.0 + effect.Data.CenterOffset.X) * 0.5),
			y_center_offset: selection.Top + (selection.Height * (1.0 + effect.Data.CenterOffset.Y) * 0.5),
			colPrimary: effect.Palette.PrimaryColor.ToColorBgra (),
			colSecondary: effect.Palette.SecondaryColor.ToColorBgra (),
			colTransparent: ColorBgra.Transparent,
			aaSampleCount: effect.Data.Quality * effect.Data.Quality,
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
		Span<ColorBgra> samples = stackalloc ColorBgra[settings.aaSampleCount];
		double relativeX = target.X - settings.x_center_offset;
		int sampleCount = 0;
		for (int p = 0; p < settings.aaSampleCount; ++p) {

			Warp.TransformData initialTd = new (
				X: relativeX + aaPoints[p].X,
				Y: relativeY - aaPoints[p].Y);

			Warp.TransformData td = effect.InverseTransform (initialTd, settings);

			PointF preliminarySample = new (
				x: (float) (td.X + settings.x_center_offset),
				y: (float) (td.Y + settings.y_center_offset));

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
