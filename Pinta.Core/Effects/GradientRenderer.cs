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
	private PointD start_point;
	private PointD end_point;
	private bool alpha_blending;
	private bool alpha_only;

	private bool lerp_cache_is_valid = false;
	private readonly byte[] lerp_alphas;
	private readonly ColorBgra[] lerp_colors;

	public ColorBgra StartColor {
		get => start_color;
		set {
			if (start_color != value) {
				start_color = value;
				lerp_cache_is_valid = false;
			}
		}
	}

	public ColorBgra EndColor {
		get => end_color;
		set {
			if (end_color != value) {
				end_color = value;
				lerp_cache_is_valid = false;
			}
		}
	}

	public PointD StartPoint {
		get => start_point;
		set => start_point = value;
	}

	public PointD EndPoint {
		get => end_point;
		set => end_point = value;
	}

	public bool AlphaBlending {
		get => alpha_blending;
		set => alpha_blending = value;
	}

	public bool AlphaOnly {
		get => alpha_only;
		set => alpha_only = value;
	}

	private readonly record struct AlphaBounds (byte StartAlpha, byte EndAlpha);

	public virtual void BeforeRender ()
	{
		if (lerp_cache_is_valid)
			return;

		AlphaBounds bounds;

		if (alpha_only) {
			bounds = ComputeAlphaOnlyValuesFromColors (
				start_color,
				end_color
			);
		} else {
			bounds = new (
				StartAlpha: start_color.A,
				EndAlpha: end_color.A
			);
		}

		for (int i = 0; i < 256; ++i) {
			byte a = (byte) i;
			lerp_colors[a] = ColorBgra.Blend (start_color, end_color, a);
			lerp_alphas[a] = (byte) (bounds.StartAlpha + ((bounds.EndAlpha - bounds.StartAlpha) * a) / 255);
		}

		lerp_cache_is_valid = true;
	}

	public abstract byte ComputeByteLerp (int x, int y);

	public virtual void AfterRender ()
	{
	}

	private static AlphaBounds ComputeAlphaOnlyValuesFromColors (ColorBgra startColor, ColorBgra endColor)
	{
		return new (
			StartAlpha: startColor.A,
			EndAlpha: (byte) (255 - endColor.A)
		);
	}

	public void Render (ImageSurface surface, ReadOnlySpan<RectangleI> rois)
	{
		AlphaBounds bounds;

		if (alpha_only) {
			bounds = ComputeAlphaOnlyValuesFromColors (start_color, end_color);
		} else {
			bounds = new (
				StartAlpha: start_color.A,
				EndAlpha: end_color.A
			);
		}

		surface.Flush ();

		Span<ColorBgra> src_data = surface.GetPixelData ();
		int src_width = surface.Width;

		for (int ri = 0; ri < rois.Length; ++ri) {

			RectangleI rect = rois[ri];

			if (start_point.X != end_point.X || start_point.Y != end_point.Y) {
				var mainrect = rect;
				Parallel.ForEach (
					Enumerable.Range (rect.Top, rect.Height),
					(y) => ProcessGradientLine (bounds.StartAlpha, bounds.EndAlpha, y, mainrect, surface.GetPixelData (), src_width)
				);
				continue;
			}

			// Start and End point are the same ... fill with solid color.
			for (int y = rect.Top; y <= rect.Bottom; ++y) {
				var row = src_data.Slice (y * src_width, src_width);
				for (int x = rect.Left; x <= rect.Right; ++x) {
					ref ColorBgra pixel = ref row[x];
					pixel = GetFinalSolidColor (bounds, pixel);
				}
			}
		}

		surface.MarkDirty ();
		AfterRender ();
	}

	private ColorBgra GetFinalSolidColor (AlphaBounds bounds, ColorBgra pixel)
	{
		ColorBgra result;
		if (alpha_only && alpha_blending) {
			byte resultAlpha = (byte) Utility.FastDivideShortByByte ((ushort) (pixel.A * bounds.EndAlpha), 255);
			result = pixel;
			result.A = resultAlpha;
		} else if (alpha_only && !alpha_blending) {
			result = pixel;
			result.A = bounds.EndAlpha;
		} else if (!alpha_only && alpha_blending) {
			result = normal_blend_op.Apply (pixel, end_color);
			//if (!this.alphaOnly && !this.alphaBlending)
		} else {
			result = end_color;
		}
		return result;
	}

	private bool ProcessGradientLine (byte startAlpha, byte endAlpha, int y, RectangleI rect, Span<ColorBgra> surface_data, int src_width)
	{
		var row = surface_data.Slice (y * src_width, src_width);
		var right = rect.Right;

		// Note that Cairo uses premultiplied alpha.
		if (alpha_only && alpha_blending) {
			for (var x = rect.Left; x <= right; ++x) {
				var lerpByte = ComputeByteLerp (x, y);
				var lerpAlpha = lerp_alphas[lerpByte];
				ref ColorBgra pixel = ref row[x];
				pixel = ColorBgra.FromBgra (
					b: Utility.FastScaleByteByByte (pixel.B, lerpAlpha),
					g: Utility.FastScaleByteByByte (pixel.G, lerpAlpha),
					r: Utility.FastScaleByteByByte (pixel.R, lerpAlpha),
					a: Utility.FastScaleByteByByte (pixel.A, lerpAlpha)
				);
			}
		} else if (alpha_only && !alpha_blending) {
			for (var x = rect.Left; x <= right; ++x) {
				var lerpByte = ComputeByteLerp (x, y);
				var lerpAlpha = lerp_alphas[lerpByte];
				ref ColorBgra pixel = ref row[x];

				var color = pixel.ToStraightAlpha ();
				color.A = lerpAlpha;
				pixel = color.ToPremultipliedAlpha ();
			}
		} else if (!alpha_only && (alpha_blending && (startAlpha != 255 || endAlpha != 255))) {
			// If we're doing all color channels, and we're doing alpha blending, and if alpha blending is necessary
			for (var x = rect.Left; x <= right; ++x) {
				var lerpByte = ComputeByteLerp (x, y);
				var lerpColor = lerp_colors[lerpByte];
				ref ColorBgra pixel = ref row[x];
				pixel = normal_blend_op.Apply (pixel, lerpColor);
			}
			//if (!this.alphaOnly && !this.alphaBlending) // or sC.A == 255 && eC.A == 255
		} else {
			for (var x = rect.Left; x <= right; ++x) {
				var lerpByte = ComputeByteLerp (x, y);
				var lerpColor = lerp_colors[lerpByte];
				row[x] = lerpColor;
			}
		}
		return true;
	}

	protected internal GradientRenderer (bool alphaOnly, BinaryPixelOp normalBlendOp)
	{
		normal_blend_op = normalBlendOp;
		alpha_only = alphaOnly;
		lerp_alphas = new byte[256];
		lerp_colors = new ColorBgra[256];
	}
}
