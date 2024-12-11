/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class CanvasRenderer
{
	private static readonly Cairo.Pattern tranparent_pattern;

	private readonly ICanvasGridService? canvas_grid;
	private readonly bool enable_live_preview;

	private Size source_size;
	private Size destination_size;
	private Fraction<int> scale_factor;
	private double scale_ratio;

	private ImmutableArray<int>? d_2_s_lookup_x;
	private ImmutableArray<int>? d_2_s_lookup_y;
	private ImmutableArray<int>? s_2_d_lookup_x;
	private ImmutableArray<int>? s_2_d_lookup_y;

	public CanvasRenderer (ICanvasGridService? canvasGrid, bool enableLivePreview)
	{
		canvas_grid = canvasGrid;
		enable_live_preview = enableLivePreview;
	}

	static CanvasRenderer ()
	{
		tranparent_pattern = CairoExtensions.CreateTransparentBackgroundPattern (16);
	}

	public void Initialize (Size sourceSize, Size destinationSize)
	{
		if (sourceSize == source_size && destinationSize == destination_size)
			return;

		source_size = sourceSize;
		destination_size = destinationSize;

		Fraction<int> scaleFactor = ScaleFactor.CreateClamped (source_size.Width, destination_size.Width);
		scale_factor = scaleFactor;
		scale_ratio = scale_factor.ComputeRatio ();

		d_2_s_lookup_x = null;
		d_2_s_lookup_y = null;
		s_2_d_lookup_x = null;
		s_2_d_lookup_y = null;
	}

	public void Render (
		IReadOnlyList<Layer> layers,
		Cairo.ImageSurface dst,
		PointI offset)
	{
		dst.Flush ();

		// Our rectangle of interest
		RectangleD r = new RectangleI (offset, dst.GetBounds ().Size).ToDouble ();
		bool is_one_to_one = scale_ratio == 1;

		using Cairo.Context g = new (dst);

		// Create the transparent checkerboard background
		g.Translate (-offset.X, -offset.Y);
		g.FillRectangle (r, tranparent_pattern, new PointD (offset.X, offset.Y));

		for (int i = 0; i < layers.Count; i++) {

			Layer layer = layers[i];

			Cairo.ImageSurface surf =
				(enable_live_preview && layer == PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer && PintaCore.LivePreview.IsEnabled)
				? PintaCore.LivePreview.LivePreviewSurface // If we're in LivePreview, choose preview layer
				: layer.Surface;

			g.Save ();

			if (!is_one_to_one) {
				// Scale the source surface based on the zoom level.
				double inv_scale = 1.0 / scale_ratio;
				g.Scale (inv_scale, inv_scale);
			}

			g.Transform (layer.Transform);

			// Use nearest-neighbor interpolation when zoomed in so that there isn't any smoothing.
			ResamplingMode filter = (scale_ratio <= 1) ? ResamplingMode.NearestNeighbor : ResamplingMode.Bilinear;

			g.SetSourceSurface (surf, filter);

			g.SetBlendMode (layer.BlendMode);
			g.PaintWithAlpha (layer.Opacity);
			g.Restore ();
		}

		// Draw the grid if it's enabled and we're zoomed in far enough
		const int minGridLineDistance = 5;
		if (canvas_grid is { ShowGrid: true } && GetMinGridLineDistance (canvas_grid) >= minGridLineDistance)
			RenderPixelGrid (dst, offset, canvas_grid);

		dst.MarkDirty ();
	}

	/// <summary>
	/// Returns the minimum number of pixels that could be between the rendered grid lines.
	/// </summary>
	/// <param name="canvasGrid">Canvas grid to use for the calculations</param>
	private int GetMinGridLineDistance (ICanvasGridService canvasGrid)
	{
		int cellHeight = canvasGrid.CellHeight;
		int cellWidth = canvasGrid.CellWidth;

		int minCanvasDistance = Math.Min (cellHeight, cellWidth);
		double minRenderedDistance = minCanvasDistance / scale_ratio;

		return (int) minRenderedDistance;
	}

	// Lazily create and cache these
	private ImmutableArray<int> D2SLookupX => d_2_s_lookup_x ??= CreateLookupX (source_size.Width, destination_size.Width, scale_factor);
	private ImmutableArray<int> D2SLookupY => d_2_s_lookup_y ??= CreateLookupY (source_size.Height, destination_size.Height, scale_factor);
	private ImmutableArray<int> S2DLookupX => s_2_d_lookup_x ??= CreateS2DLookupX (source_size.Width, destination_size.Width, scale_factor);
	private ImmutableArray<int> S2DLookupY => s_2_d_lookup_y ??= CreateS2DLookupY (source_size.Height, destination_size.Height, scale_factor);

	#region Algorithms ported from PDN

	private void RenderPixelGrid (Cairo.ImageSurface dst, PointI offset, ICanvasGridService canvasGrid)
	{
		Span<ColorBgra> dst_data = dst.GetPixelData ();

		int dstHeight = dst.Height;
		int dstWidth = dst.Width;

		// Draw horizontal lines

		int sTop = D2SLookupY[offset.Y];
		int sBottom = D2SLookupY[offset.Y + dstHeight];

		var lookup_y = S2DLookupY;

		var gridHeight = canvasGrid.CellHeight;

		for (int srcY = sTop; srcY <= sBottom; srcY += gridHeight) {

			int dstY = lookup_y[srcY];
			int dstRow = dstY - offset.Y;

			if (dstRow < 0 || dstRow >= dstHeight)
				continue;

			var dst_row = dst_data.Slice (dstRow * dstWidth, dstWidth);

			for (int x = offset.X & 1; x < dst_row.Length; x += 2)
				dst_row[x] = ColorBgra.Black;
		}

		// Draw vertical lines

		int sLeft = D2SLookupX[offset.X];
		int sRight = D2SLookupX[offset.X + dstWidth];

		var lookup_x = S2DLookupX;

		var gridWidth = canvasGrid.CellWidth;

		for (int srcX = sLeft; srcX <= sRight; srcX += gridWidth) {

			int dstX = lookup_x[srcX];
			int dstCol = dstX - offset.X;

			if (dstCol < 0 || dstCol >= dstWidth)
				continue;

			for (int idx = dstCol + (offset.Y & 1) * dstWidth; idx < dst_data.Length; idx += 2 * dstWidth)
				dst_data[idx] = ColorBgra.Black;
		}
	}

	private static ImmutableArray<int> CreateLookupX (
		int srcWidth,
		int dstWidth,
		Fraction<int> scaleFactor)
	{
		int length = dstWidth + 1;
		var lookup = ImmutableArray.CreateBuilder<int> (length);
		lookup.Count = length;

		// Sometimes the scale factor is slightly different on one axis than
		// on another, simply due to accuracy. So we have to clamp this value to
		// be within bounds.
		for (var x = 0; x < lookup.Count; ++x)
			lookup[x] = Math.Clamp (scaleFactor.ScaleScalar (x), 0, srcWidth - 1);

		return lookup.MoveToImmutable ();
	}

	private static ImmutableArray<int> CreateLookupY (
		int srcHeight,
		int dstHeight,
		Fraction<int> scaleFactor)
	{
		int length = dstHeight + 1;
		var lookup = ImmutableArray.CreateBuilder<int> (length);
		lookup.Count = length;

		// Sometimes the scale factor is slightly different on one axis than
		// on another, simply due to accuracy. So we have to clamp this value to
		// be within bounds.
		for (int y = 0; y < lookup.Count; ++y)
			lookup[y] = Math.Clamp (scaleFactor.ScaleScalar (y), 0, srcHeight - 1);

		return lookup.MoveToImmutable ();
	}

	private static ImmutableArray<int> CreateS2DLookupX (
		int srcWidth,
		int dstWidth,
		Fraction<int> scaleFactor)
	{
		int length = srcWidth + 1;
		var lookup = ImmutableArray.CreateBuilder<int> (length);
		lookup.Count = length;

		// Sometimes the scale factor is slightly different on one axis than
		// on another, simply due to accuracy. So we have to clamp this value to
		// be within bounds.
		for (int x = 0; x < lookup.Count; ++x)
			lookup[x] = Math.Clamp (scaleFactor.UnscaleScalar (x), 0, dstWidth - 1);

		return lookup.MoveToImmutable ();
	}

	private static ImmutableArray<int> CreateS2DLookupY (
		int srcHeight,
		int dstHeight,
		Fraction<int> scaleFactor)
	{
		int length = srcHeight + 1;
		var lookup = ImmutableArray.CreateBuilder<int> (length);
		lookup.Count = length;

		// Sometimes the scale factor is slightly different on one axis than
		// on another, simply due to accuracy. So we have to clamp this value to
		// be within bounds.
		for (int y = 0; y < lookup.Count; ++y)
			lookup[y] = Math.Clamp (scaleFactor.UnscaleScalar (y), 0, dstHeight - 1);

		return lookup.MoveToImmutable ();
	}

	#endregion
}
