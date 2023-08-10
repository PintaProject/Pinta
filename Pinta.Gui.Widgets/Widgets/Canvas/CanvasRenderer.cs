/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Collections.Immutable;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class CanvasRenderer
{
	private static readonly Cairo.Pattern tranparent_pattern;

	private readonly bool enable_pixel_grid;
	private readonly bool enable_live_preview;

	private Size source_size;
	private Size destination_size;
	private ScaleFactor scale_factor;

	private ImmutableArray<int>? d_2_s_lookup_x;
	private ImmutableArray<int>? d_2_s_lookup_y;
	private ImmutableArray<int>? s_2_d_lookup_x;
	private ImmutableArray<int>? s_2_d_lookup_y;

	public CanvasRenderer (bool enable_pixel_grid, bool enableLivePreview)
	{
		this.enable_pixel_grid = enable_pixel_grid;
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

		scale_factor = new ScaleFactor (source_size.Width, destination_size.Width);

		d_2_s_lookup_x = null;
		d_2_s_lookup_y = null;
		s_2_d_lookup_x = null;
		s_2_d_lookup_y = null;
	}

	public void Render (List<Layer> layers, Cairo.ImageSurface dst, PointI offset)
	{
		dst.Flush ();

		// Our rectangle of interest
		var r = new RectangleI (offset, dst.GetBounds ().Size).ToDouble ();
		var is_one_to_one = scale_factor.Ratio == 1;

		var g = new Cairo.Context (dst);

		// Create the transparent checkerboard background
		g.Translate (-offset.X, -offset.Y);
		g.FillRectangle (r, tranparent_pattern, new PointD (offset.X, offset.Y));

		for (var i = 0; i < layers.Count; i++) {
			var layer = layers[i];
			var surf = layer.Surface;

			// If we're in LivePreview, substitute current layer with the preview layer
			if (enable_live_preview && layer == PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer && PintaCore.LivePreview.IsEnabled)
				surf = PintaCore.LivePreview.LivePreviewSurface;

			g.Save ();
			if (!is_one_to_one) {
				// Scale the source surface based on the zoom level.
				double inv_scale = 1.0 / scale_factor.Ratio;
				g.Scale (inv_scale, inv_scale);
			}

			g.Transform (layer.Transform);

			// Use nearest-neighbor interpolation when zoomed in so that there isn't any smoothing.
			var filter = (scale_factor.Ratio <= 1) ? Cairo.Filter.Nearest : Cairo.Filter.Bilinear;
			var src_pattern = new Cairo.SurfacePattern (surf) { Filter = filter };
			g.SetSource (src_pattern);

			g.SetBlendMode (layer.BlendMode);
			g.PaintWithAlpha (layer.Opacity);
			g.Restore ();
		}

		// If we are at least 200% and grid is requested, draw it
		if (enable_pixel_grid && PintaCore.Actions.View.PixelGrid.Value && scale_factor.Ratio <= 0.5d)
			RenderPixelGrid (dst, offset);

		dst.MarkDirty ();
	}

	// Lazily create and cache these
	private ImmutableArray<int> D2SLookupX => d_2_s_lookup_x ??= CreateLookupX (source_size.Width, destination_size.Width, scale_factor);
	private ImmutableArray<int> D2SLookupY => d_2_s_lookup_y ??= CreateLookupY (source_size.Height, destination_size.Height, scale_factor);
	private ImmutableArray<int> S2DLookupX => s_2_d_lookup_x ??= CreateS2DLookupX (source_size.Width, destination_size.Width, scale_factor);
	private ImmutableArray<int> S2DLookupY => s_2_d_lookup_y ??= CreateS2DLookupY (source_size.Height, destination_size.Height, scale_factor);

	#region Algorithms ported from PDN
	private void RenderPixelGrid (Cairo.ImageSurface dst, PointI offset)
	{
		// Draw horizontal lines
		var dst_data = dst.GetPixelData ();
		var dstHeight = dst.Height;
		var dstWidth = dst.Width;
		var dstStride = dst.Stride;
		var sTop = D2SLookupY[offset.Y];
		var sBottom = D2SLookupY[offset.Y + dstHeight];
		var lookup_y = S2DLookupY;

		for (var srcY = sTop; srcY <= sBottom; ++srcY) {
			var dstY = lookup_y[srcY];
			var dstRow = dstY - offset.Y;

			if (dstRow >= 0 && dstRow < dstHeight) {
				var dst_row = dst_data.Slice (dstRow * dstWidth, dstWidth);

				for (int x = offset.X & 1; x < dst_row.Length; x += 2)
					dst_row[x] = ColorBgra.Black;
			}
		}

		// Draw vertical lines
		var sLeft = D2SLookupX[offset.X];
		var sRight = D2SLookupX[offset.X + dstWidth];
		var lookup_x = S2DLookupX;

		for (var srcX = sLeft; srcX <= sRight; ++srcX) {
			var dstX = lookup_x[srcX];
			var dstCol = dstX - offset.X;

			if (dstCol >= 0 && dstCol < dstWidth) {
				for (int idx = dstCol + (offset.Y & 1) * dstWidth; idx < dst_data.Length; idx += 2 * dstWidth)
					dst_data[idx] = ColorBgra.Black;
			}
		}
	}

	private static ImmutableArray<int> CreateLookupX (int srcWidth, int dstWidth, ScaleFactor scaleFactor)
	{
		var lookup = ImmutableArray.CreateBuilder<int> (dstWidth + 1);

		// Sometimes the scale factor is slightly different on one axis than
		// on another, simply due to accuracy. So we have to clamp this value to
		// be within bounds.
		for (var x = 0; x < lookup.Count; ++x)
			lookup[x] = Utility.Clamp (scaleFactor.ScaleScalar (x), 0, srcWidth - 1);

		return lookup.MoveToImmutable ();
	}

	private static ImmutableArray<int> CreateLookupY (int srcHeight, int dstHeight, ScaleFactor scaleFactor)
	{
		var lookup = ImmutableArray.CreateBuilder<int> (dstHeight + 1); ;

		// Sometimes the scale factor is slightly different on one axis than
		// on another, simply due to accuracy. So we have to clamp this value to
		// be within bounds.
		for (var y = 0; y < lookup.Count; ++y)
			lookup[y] = Utility.Clamp (scaleFactor.ScaleScalar (y), 0, srcHeight - 1);

		return lookup.MoveToImmutable ();
	}

	private static ImmutableArray<int> CreateS2DLookupX (int srcWidth, int dstWidth, ScaleFactor scaleFactor)
	{
		var lookup = ImmutableArray.CreateBuilder<int> (srcWidth + 1);

		// Sometimes the scale factor is slightly different on one axis than
		// on another, simply due to accuracy. So we have to clamp this value to
		// be within bounds.
		for (var x = 0; x < lookup.Count; ++x)
			lookup[x] = Utility.Clamp (scaleFactor.UnscaleScalar (x), 0, dstWidth - 1);

		return lookup.MoveToImmutable ();
	}

	private static ImmutableArray<int> CreateS2DLookupY (int srcHeight, int dstHeight, ScaleFactor scaleFactor)
	{
		var lookup = ImmutableArray.CreateBuilder<int> (srcHeight + 1);

		// Sometimes the scale factor is slightly different on one axis than
		// on another, simply due to accuracy. So we have to clamp this value to
		// be within bounds.
		for (var y = 0; y < lookup.Count; ++y)
			lookup[y] = Utility.Clamp (scaleFactor.UnscaleScalar (y), 0, dstHeight - 1);

		return lookup.MoveToImmutable ();
	}
	#endregion
}
