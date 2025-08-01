/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;
using System.Threading.Tasks;
using Cairo;

namespace Pinta.Core;

public abstract class GradientRenderer
{
	private readonly BinaryPixelOp normal_blend_op;
	private ColorBgra start_color;
	private ColorBgra end_color;
	private bool lerp_cache_is_valid = false;
	private readonly byte[] lerp_alphas;
	private readonly ColorBgra[] lerp_colors;

	protected internal GradientRenderer (
		bool alphaOnly,
		BinaryPixelOp normalBlendOp)
	{
		normal_blend_op = normalBlendOp;
		AlphaOnly = alphaOnly;
		lerp_alphas = new byte[256];
		lerp_colors = new ColorBgra[256];
	}

	public ColorBgra StartColor {
		get => start_color;
		set {
			if (start_color == value) return;
			start_color = value;
			lerp_cache_is_valid = false;
		}
	}

	public ColorBgra EndColor {
		get => end_color;
		set {
			if (end_color == value) return;
			end_color = value;
			lerp_cache_is_valid = false;
		}
	}

	public PointD StartPoint { get; set; }
	public PointD EndPoint { get; set; }
	public bool AlphaBlending { get; set; }
	public bool AlphaOnly { get; set; }

	private readonly record struct AlphaBounds (
		byte StartAlpha,
		byte EndAlpha);

	public virtual void BeforeRender ()
	{
		if (lerp_cache_is_valid)
			return;

		AlphaBounds bounds =
			AlphaOnly
			? ComputeAlphaOnlyValuesFromColors (start_color, end_color)
			: new (StartAlpha: start_color.A, EndAlpha: end_color.A);

		for (int i = 0; i < 256; ++i) {
			byte a = (byte) i;
			lerp_colors[a] = ColorBgra.Lerp (start_color, end_color, a);
			lerp_alphas[a] = Mathematics.LerpByte (bounds.StartAlpha, bounds.EndAlpha, a);
		}

		lerp_cache_is_valid = true;
	}

	public abstract byte ComputeByteLerp (int x, int y);

	private static AlphaBounds ComputeAlphaOnlyValuesFromColors (
		ColorBgra startColor,
		ColorBgra endColor)
	{
		return new (
			StartAlpha: startColor.A,
			EndAlpha: (byte) (255 - endColor.A));
	}

	public void Render (
		ImageSurface surface,
		ReadOnlySpan<RectangleI> rois)
	{
		AlphaBounds bounds =
			AlphaOnly
			? ComputeAlphaOnlyValuesFromColors (start_color, end_color)
			: new (StartAlpha: start_color.A, EndAlpha: end_color.A);

		surface.Flush ();

		Span<ColorBgra> src_data = surface.GetPixelData ();
		int src_width = surface.Width;

		for (int ri = 0; ri < rois.Length; ++ri) {

			RectangleI rect = rois[ri];
			Parallel.ForEach (
				Enumerable.Range (rect.Top, rect.Height),
				y => ProcessGradientLine (
					bounds.StartAlpha,
					bounds.EndAlpha,
					y,
					rect,
					surface.GetPixelData (),
					src_width)
			);
		}

		surface.MarkDirty ();
	}

	private bool ProcessGradientLine (
		byte startAlpha,
		byte endAlpha,
		int y,
		RectangleI rect,
		Span<ColorBgra> surface_data,
		int src_width)
	{
		var row = surface_data.Slice (y * src_width, src_width);
		int right = rect.Right;

		// Note that Cairo uses premultiplied alpha.
		if (AlphaOnly && AlphaBlending) {
			for (var x = rect.Left; x <= right; ++x) {
				byte lerpByte = ComputeByteLerp (x, y);
				byte lerpAlpha = lerp_alphas[lerpByte];
				ColorBgra originalPixel = row[x];
				row[x] = ColorBgra.FromBgra (
					b: Utility.FastScaleByteByByte (originalPixel.B, lerpAlpha),
					g: Utility.FastScaleByteByByte (originalPixel.G, lerpAlpha),
					r: Utility.FastScaleByteByByte (originalPixel.R, lerpAlpha),
					a: Utility.FastScaleByteByByte (originalPixel.A, lerpAlpha));
			}
		} else if (AlphaOnly && !AlphaBlending) {
			for (var x = rect.Left; x <= right; ++x) {
				byte lerpByte = ComputeByteLerp (x, y);
				byte lerpAlpha = lerp_alphas[lerpByte];
				ColorBgra original = row[x];
				row[x] = original.NewAlpha (lerpAlpha);
			}
		} else if (!AlphaOnly && (AlphaBlending && (startAlpha != 255 || endAlpha != 255))) {
			// If we're doing all color channels, and we're doing alpha blending, and if alpha blending is necessary
			for (var x = rect.Left; x <= right; ++x) {
				byte lerpByte = ComputeByteLerp (x, y);
				ColorBgra lerpColor = lerp_colors[lerpByte];
				ColorBgra originalPixel = row[x];
				row[x] = normal_blend_op.Apply (originalPixel, lerpColor);
			}
		} else {
			for (int x = rect.Left; x <= right; ++x) {
				byte lerpByte = ComputeByteLerp (x, y);
				ColorBgra lerpColor = lerp_colors[lerpByte];
				row[x] = lerpColor;
			}
		}
		return true;
	}
}
