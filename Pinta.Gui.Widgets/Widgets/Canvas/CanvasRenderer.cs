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
using Gdk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class CanvasRenderer
	{
		private static readonly Cairo.Pattern tranparent_pattern;

		private readonly bool enable_pixel_grid;
		private readonly bool enable_live_preview;

		private Size source_size;
		private Size destination_size;
		private Layer? offset_layer;
		private ScaleFactor scale_factor;

		private int[]? d2sLookupX;
		private int[]? d2sLookupY;
		private int[]? s2dLookupX;
		private int[]? s2dLookupY;

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

			d2sLookupX = null;
			d2sLookupY = null;
			s2dLookupX = null;
			s2dLookupY = null;
		}

		public void Render (List<Layer> layers, Cairo.ImageSurface dst, Point offset)
		{
			dst.Flush ();

			// Our rectangle of interest
			var r = new Rectangle (offset, dst.GetBounds ().Size).ToCairoRectangle ();
			var is_one_to_one = scale_factor.Ratio == 1;

			using (var cache_surface = is_one_to_one ? null : CairoExtensions.CreateImageSurface (Cairo.Format.Argb32, dst.Width, dst.Height))
			using (var g = new Cairo.Context (dst)) {
				// Create the transparent checkerboard background
				g.Translate (-offset.X, -offset.Y);
				g.FillRectangle (r, tranparent_pattern, new Cairo.PointD (offset.X, offset.Y));

				for (var i = 0; i < layers.Count; i++) {
					var layer = layers[i];

					// If we're in LivePreview, substitute current layer with the preview layer
					if (enable_live_preview && layer == PintaCore.Workspace.ActiveDocument.Layers.CurrentUserLayer && PintaCore.LivePreview.IsEnabled)
						layer = CreateLivePreviewLayer (layer);

					// If the layer is offset, handle it here
					if (!layer.Transform.IsIdentity ())
						layer = CreateOffsetLayer (layer);

					// No need to resize the surface if we're at 100% zoom
					if (is_one_to_one)
						layer.Draw (g, layer.Surface, layer.Opacity, false);
					else {
						g.Save ();
						// Have to undo the translate set above
						g.Translate (offset.X, offset.Y);
						CopyScaled (layer.Surface, cache_surface!, r.ToGdkRectangle ());
						layer.Draw (g, cache_surface!, layer.Opacity, false);
						g.Restore ();
					}
				}
			}

			// If we are at least 200% and grid is requested, draw it
			if (enable_pixel_grid && PintaCore.Actions.View.PixelGrid.Value && scale_factor.Ratio <= 0.5d)
				RenderPixelGrid (dst, offset);

			dst.MarkDirty ();
		}

		private Layer OffsetLayer {
			get {
				// Create one if we don't have one
				if (offset_layer == null)
					offset_layer = new Layer (CairoExtensions.CreateImageSurface (Cairo.Format.ARGB32, source_size.Width, source_size.Height));

				// If we have the wrong size one, dispose it and create the correct size
				if (offset_layer.Surface.Width != source_size.Width || offset_layer.Surface.Height != source_size.Height) {
					(offset_layer.Surface as IDisposable).Dispose ();
					offset_layer = new Layer (CairoExtensions.CreateImageSurface (Cairo.Format.ARGB32, source_size.Width, source_size.Height));
				}

				return offset_layer;
			}
		}

		private Layer CreateLivePreviewLayer (Layer original)
		{
			var preview_layer = new Layer (PintaCore.LivePreview.LivePreviewSurface) {
				BlendMode = original.BlendMode,
				Opacity = original.Opacity,
				Hidden = original.Hidden
			};

			preview_layer.Transform.InitMatrix (original.Transform);

			return preview_layer;
		}

		private Layer CreateOffsetLayer (Layer original)
		{
			var offset = OffsetLayer;
			offset.Surface.Clear ();

			using (var g = new Cairo.Context (offset.Surface))
				original.Draw (g, original.Surface, 1);

			offset.BlendMode = original.BlendMode;
			offset.Transform.InitMatrix (original.Transform);
			offset.Opacity = original.Opacity;

			return offset;
		}

		// Lazily create and cache these
		private int[] D2SLookupX => d2sLookupX ??= CreateLookupX (source_size.Width, destination_size.Width, scale_factor);
		private int[] D2SLookupY => d2sLookupY ??= CreateLookupY (source_size.Height, destination_size.Height, scale_factor);
		private int[] S2DLookupX => s2dLookupX ??= CreateS2DLookupX (source_size.Width, destination_size.Width, scale_factor);
		private int[] S2DLookupY => s2dLookupY ??= CreateS2DLookupY (source_size.Height, destination_size.Height, scale_factor);

		#region Algorithms ported from PDN
		private void CopyScaled (Cairo.ImageSurface src, Cairo.ImageSurface dst, Rectangle roi)
		{
			if (scale_factor.Ratio < 1)
				CopyScaledZoomIn (src, dst, roi);
			else
				CopyScaledZoomOut (src, dst, roi);
		}

		private unsafe void CopyScaledZoomIn (Cairo.ImageSurface src, Cairo.ImageSurface dst, Rectangle roi)
		{
			// Tell Cairo we need the latest raw data
			dst.Flush ();

			// Cache pointers to surface raw data
			var src_ptr = (ColorBgra*) src.DataPtr;
			var dst_ptr = (ColorBgra*) dst.DataPtr;

			// Cache surface sizes
			var src_width = src.Width;
			var dst_width = dst.Width;
			var dst_height = dst.Height;

			// Cache lookup tables
			var lookup_x = D2SLookupX;
			var lookup_y = D2SLookupY;

			for (var dst_row = 0; dst_row < dst_height; ++dst_row) {
				// For each dest row, look up the src row to copy from
				var nnY = dst_row + roi.Y;
				var srcY = lookup_y[nnY];

				// Get pointers to src and dest rows
				var dst_row_ptr = dst.GetRowAddressUnchecked (dst_ptr, dst_width, dst_row);
				var src_row_ptr = src.GetRowAddressUnchecked (src_ptr, src_width, srcY);

				for (var dstCol = 0; dstCol < dst_width; ++dstCol) {
					// Look up the src column to copy from
					var nnX = dstCol + roi.X;
					var srcX = lookup_x[nnX];

					// Copy source to destination
					*dst_row_ptr++ = *(src_row_ptr + srcX);
				}
			}

			// Tell Cairo we changed the raw data
			dst.MarkDirty ();
		}

		private unsafe void CopyScaledZoomOut (Cairo.ImageSurface src, Cairo.ImageSurface dst, Rectangle roi)
		{
			// Tell Cairo we need the latest raw data
			dst.Flush ();

			const int fpShift = 12;
			const int fpFactor = (1 << fpShift);

			var source_size = src.GetBounds ().Size;

			// Find destination bounds
			var dst_left = (int) (((long) roi.X * fpFactor * (long) source_size.Width) / (long) destination_size.Width);
			var dst_top = (int) (((long) roi.Y * fpFactor * (long) source_size.Height) / (long) destination_size.Height);
			var dst_right = (int) (((long) (roi.X + dst.Width) * fpFactor * (long) source_size.Width) / (long) destination_size.Width);
			var dst_bottom = (int) (((long) (roi.Y + dst.Height) * fpFactor * (long) source_size.Height) / (long) destination_size.Height);
			var dx = (dst_right - dst_left) / dst.Width;
			var dy = (dst_bottom - dst_top) / dst.Height;

			// Cache pointers to surface raw data and sizes
			var src_ptr = (ColorBgra*) src.DataPtr;
			var dst_ptr = (ColorBgra*) dst.DataPtr;
			var src_width = src.Width;
			var dst_width = dst.Width;
			var dst_height = dst.Height;

			for (int dstRow = 0, fDstY = dst_top; dstRow < dst_height && fDstY < dst_bottom; ++dstRow, fDstY += dy) {
				var srcY1 = fDstY >> fpShift;                            // y
				var srcY2 = (fDstY + (dy >> 2)) >> fpShift;              // y + 0.25
				var srcY3 = (fDstY + (dy >> 1)) >> fpShift;              // y + 0.50
				var srcY4 = (fDstY + (dy >> 1) + (dy >> 2)) >> fpShift;  // y + 0.75

				var src1 = src.GetRowAddressUnchecked (src_ptr, src_width, srcY1);
				var src2 = src.GetRowAddressUnchecked (src_ptr, src_width, srcY2);
				var src3 = src.GetRowAddressUnchecked (src_ptr, src_width, srcY3);
				var src4 = src.GetRowAddressUnchecked (src_ptr, src_width, srcY4);
				var dstPtr = dst.GetRowAddressUnchecked (dst_ptr, dst_width, dstRow);

				var checkerY = dstRow + roi.Y;
				var checkerX = roi.X;
				var maxCheckerX = checkerX + dst.Width;

				for (var fDstX = dst_left; checkerX < maxCheckerX && fDstX < dst_right; ++checkerX, fDstX += dx) {
					var srcX1 = (fDstX + (dx >> 2)) >> fpShift;             // x + 0.25
					var srcX2 = (fDstX + (dx >> 1) + (dx >> 2)) >> fpShift; // x + 0.75
					var srcX3 = fDstX >> fpShift;                           // x
					var srcX4 = (fDstX + (dx >> 1)) >> fpShift;             // x + 0.50

					var p1 = src1 + srcX1;
					var p2 = src2 + srcX2;
					var p3 = src3 + srcX3;
					var p4 = src4 + srcX4;

					var r = (2 + p1->R + p2->R + p3->R + p4->R) >> 2;
					var g = (2 + p1->G + p2->G + p3->G + p4->G) >> 2;
					var b = (2 + p1->B + p2->B + p3->B + p4->B) >> 2;
					var a = (2 + p1->A + p2->A + p3->A + p4->A) >> 2;

					// Copy color to destination
					*dstPtr++ = ColorBgra.FromUInt32 ((uint) b + ((uint) g << 8) + ((uint) r << 16) + ((uint) a << 24));
				}
			}

			// Tell Cairo we changed the raw data
			dst.MarkDirty ();
		}

		private unsafe void RenderPixelGrid (Cairo.ImageSurface dst, Point offset)
		{
			// Draw horizontal lines
			var dst_ptr = (ColorBgra*) dst.DataPtr;
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
					var dstRowPtr = dst.GetRowAddressUnchecked (dst_ptr, dstWidth, dstRow);
					var dstRowEndPtr = dstRowPtr + dstWidth;

					dstRowPtr += offset.X & 1;

					while (dstRowPtr < dstRowEndPtr) {
						*dstRowPtr = ColorBgra.Black;
						dstRowPtr += 2;
					}
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
					var dstColPtr = (byte*) dst.GetPointAddress (dstCol, 0);
					var dstColEndPtr = dstColPtr + dstStride * dstHeight;

					dstColPtr += (offset.Y & 1) * dstStride;

					while (dstColPtr < dstColEndPtr) {
						*((ColorBgra*) dstColPtr) = ColorBgra.Black;
						dstColPtr += 2 * dstStride;
					}
				}
			}
		}

		private static int[] CreateLookupX (int srcWidth, int dstWidth, ScaleFactor scaleFactor)
		{
			var lookup = new int[dstWidth + 1];

			// Sometimes the scale factor is slightly different on one axis than
			// on another, simply due to accuracy. So we have to clamp this value to
			// be within bounds.
			for (var x = 0; x < lookup.Length; ++x)
				lookup[x] = Utility.Clamp (scaleFactor.ScaleScalar (x), 0, srcWidth - 1);

			return lookup;
		}

		private static int[] CreateLookupY (int srcHeight, int dstHeight, ScaleFactor scaleFactor)
		{
			var lookup = new int[dstHeight + 1];

			// Sometimes the scale factor is slightly different on one axis than
			// on another, simply due to accuracy. So we have to clamp this value to
			// be within bounds.
			for (var y = 0; y < lookup.Length; ++y)
				lookup[y] = Utility.Clamp (scaleFactor.ScaleScalar (y), 0, srcHeight - 1);

			return lookup;
		}

		private static int[] CreateS2DLookupX (int srcWidth, int dstWidth, ScaleFactor scaleFactor)
		{
			var lookup = new int[srcWidth + 1];

			// Sometimes the scale factor is slightly different on one axis than
			// on another, simply due to accuracy. So we have to clamp this value to
			// be within bounds.
			for (var x = 0; x < lookup.Length; ++x)
				lookup[x] = Utility.Clamp (scaleFactor.UnscaleScalar (x), 0, dstWidth - 1);

			return lookup;
		}

		private static int[] CreateS2DLookupY (int srcHeight, int dstHeight, ScaleFactor scaleFactor)
		{
			var lookup = new int[srcHeight + 1];

			// Sometimes the scale factor is slightly different on one axis than
			// on another, simply due to accuracy. So we have to clamp this value to
			// be within bounds.
			for (var y = 0; y < lookup.Length; ++y)
				lookup[y] = Utility.Clamp (scaleFactor.UnscaleScalar (y), 0, dstHeight - 1);

			return lookup;
		}
		#endregion
	}
}
