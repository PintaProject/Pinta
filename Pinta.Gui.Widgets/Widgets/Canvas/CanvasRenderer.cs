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
	class CanvasRenderer
	{
		private Size source_size;
		private Size destination_size;
		private Layer offset_layer;
		private bool enable_pixel_grid;

		private ScaleFactor scale_factor;
		private bool generated;
		private int[] d2sLookupX;
		private int[] d2sLookupY;
		private int[] s2dLookupX;
		private int[] s2dLookupY;

		public CanvasRenderer (bool enable_pixel_grid)
		{
			this.enable_pixel_grid = enable_pixel_grid;
		}

		public void Initialize (Size sourceSize, Size destinationSize)
		{
			if (sourceSize == source_size && destinationSize == destination_size)
				return;
				
			source_size = sourceSize;
			destination_size = destinationSize;
			
			scale_factor = new ScaleFactor (source_size.Width, destination_size.Width);
			generated = false;
		}

		public void Render (List<Layer> layers, Cairo.ImageSurface dst, Gdk.Point offset)
		{
			dst.Flush ();
		
			if (scale_factor.Ratio == 1)
				RenderOneToOne (layers, dst, offset);
			else if (scale_factor.Ratio < 1)
				RenderZoomIn (layers, dst, offset);
			else
				RenderZoomOut (layers, dst, offset, destination_size);
			
			dst.MarkDirty ();
		}

		private Layer OffsetLayer {
			get {
				// Create one if we don't have one
				if (offset_layer == null)
					offset_layer = new Layer (new Cairo.ImageSurface (Cairo.Format.ARGB32, source_size.Width, source_size.Height));

				// If we have the wrong size one, dispose it and create the correct size
				if (offset_layer.Surface.Width != source_size.Width || offset_layer.Surface.Height != source_size.Height) {
					(offset_layer.Surface as IDisposable).Dispose ();
					offset_layer = new Layer (new Cairo.ImageSurface (Cairo.Format.ARGB32, source_size.Width, source_size.Height));
				}

				return offset_layer;
			}
		}

		private Layer CreateLivePreviewLayer (Layer original)
		{
			var preview_layer = new Layer (PintaCore.LivePreview.LivePreviewSurface);

			preview_layer.BlendMode = original.BlendMode;
			preview_layer.Transform.InitMatrix(original.Transform);
			preview_layer.Opacity = original.Opacity;
			preview_layer.Hidden = original.Hidden;

			return preview_layer;
		}

		private Layer CreateOffsetLayer (Layer original)
		{
			var offset = OffsetLayer;
			offset.Surface.Clear ();

			using (var g = new Cairo.Context (offset.Surface)) {
				original.Draw(g, original.Surface, 1);
			}

			offset.BlendMode = original.BlendMode;
			offset.Transform.InitMatrix(original.Transform);
			offset.Opacity = original.Opacity;

			return offset;
		}

		#region Algorithms ported from PDN
		private unsafe void RenderOneToOne (List<Layer> layers, Cairo.ImageSurface dst, Gdk.Point offset)
		{
			// The first layer should be blended with the transparent checkerboard
			var checker = true;
			CheckerBoardOperation checker_op = null;

			for (int i = 0; i < layers.Count; i++) {
				var layer = layers[i];

				// If we're in LivePreview, substitute current layer with the preview layer
				if (layer == PintaCore.Layers.CurrentLayer && PintaCore.LivePreview.IsEnabled)
					layer = CreateLivePreviewLayer (layer);

				// If the layer is offset, handle it here
				if (!layer.Transform.IsIdentity ())
					layer = CreateOffsetLayer (layer);

				var src = layer.Surface;

				// Get the blend mode for this layer and opacity
				var blend_op = UserBlendOps.GetBlendOp (layer.BlendMode, layer.Opacity);
				
				if (checker)
					checker_op = new CheckerBoardOperation (layer.Opacity);

				// Figure out where our source and destination intersect
				var srcRect = new Gdk.Rectangle (offset, dst.GetBounds ().Size);
				srcRect.Intersect (src.GetBounds ());

				// Get pointers to our surfaces
				var src_ptr = (ColorBgra*)src.DataPtr;
				var dst_ptr = (ColorBgra*)dst.DataPtr;

				// Cache widths
				int src_width = src.Width;
				int dst_width = dst.Width;

				for (int dstRow = 0; dstRow < srcRect.Height; ++dstRow) {
					ColorBgra* dstRowPtr = dst.GetRowAddressUnchecked (dst_ptr, dst_width, dstRow);
					ColorBgra* srcRowPtr = src.GetPointAddressUnchecked (src_ptr, src_width, offset.X, dstRow + offset.Y);

					int dstCol = offset.X;
					int dstColEnd = offset.X + srcRect.Width;
					int checkerY = dstRow + offset.Y;

					while (dstCol < dstColEnd) {
						// Blend it over the checkerboard background
						if (checker)
							*dstRowPtr = checker_op.Apply (*srcRowPtr, dstCol, checkerY);
						else
							*dstRowPtr = blend_op.Apply (*dstRowPtr, *srcRowPtr);
					
						++dstRowPtr;
						++srcRowPtr;
						++dstCol;
					}
				}

				// Only checker the first layer
				checker = false;
			}
		}

		private unsafe void RenderZoomIn (List<Layer> layers, Cairo.ImageSurface dst, Gdk.Point offset)
		{
			if (!generated) {
				d2sLookupX = CreateLookupX (source_size.Width, destination_size.Width, scale_factor);
				d2sLookupY = CreateLookupY (source_size.Height, destination_size.Height, scale_factor);
				s2dLookupX = CreateS2DLookupX (source_size.Width, destination_size.Width, scale_factor);
				s2dLookupY = CreateS2DLookupY (source_size.Height, destination_size.Height, scale_factor);

				generated = true;
			}

			// The first layer should be blended with the transparent checkerboard
			var checker = true;
			CheckerBoardOperation checker_op = null;

			for (int i = 0; i < layers.Count; i++) {
				var layer = layers[i];

				// If we're in LivePreview, substitute current layer with the preview layer
				if (layer == PintaCore.Layers.CurrentLayer && PintaCore.LivePreview.IsEnabled)
					layer = CreateLivePreviewLayer (layer);

				// If the layer is offset, handle it here
				if (!layer.Transform.IsIdentity ())
					layer = CreateOffsetLayer (layer);

				var src = layer.Surface;

				// Get the blend mode for this layer and opacity
				var blend_op = UserBlendOps.GetBlendOp (layer.BlendMode, layer.Opacity);

				if (checker)
					checker_op = new CheckerBoardOperation (layer.Opacity);
				
				ColorBgra* src_ptr = (ColorBgra*)src.DataPtr;
				ColorBgra* dst_ptr = (ColorBgra*)dst.DataPtr;

				int src_width = src.Width;
				int dst_width = dst.Width;
				int dst_height = dst.Height;

				for (int dstRow = 0; dstRow < dst_height; ++dstRow) {
					int nnY = dstRow + offset.Y;
					int srcY = d2sLookupY[nnY];

					ColorBgra* dstPtr = dst.GetRowAddressUnchecked (dst_ptr, dst_width, dstRow);
					ColorBgra* srcRow = src.GetRowAddressUnchecked (src_ptr, src_width, srcY);

					for (int dstCol = 0; dstCol < dst_width; ++dstCol) {
						int nnX = dstCol + offset.X;
						int srcX = d2sLookupX[nnX];

						// Blend it over the checkerboard background
						if (checker)
							*dstPtr = checker_op.Apply (*(srcRow + srcX), dstCol + offset.X, dstRow + offset.Y);
						else
							*dstPtr = blend_op.Apply (*dstPtr, *(srcRow + srcX));

						++dstPtr;
					}
				}

				// Only checker the first layer
				checker = false;
			}

			// If we are at least 200% and grid is requested, draw it
			if (enable_pixel_grid && PintaCore.Actions.View.PixelGrid.Active && scale_factor.Ratio <= 0.5d)
				RenderPixelGrid (dst, offset);
		}

		private unsafe void RenderZoomOut (List<Layer> layers, Cairo.ImageSurface dst, Gdk.Point offset, Gdk.Size destinationSize)
		{
			// The first layer should be blended with the transparent checkerboard
			var checker = true;
			CheckerBoardOperation checker_op = null;

			for (int i = 0; i < layers.Count; i++) {
				var layer = layers[i];

				// If we're in LivePreview, substitute current layer with the preview layer
				if (layer == PintaCore.Layers.CurrentLayer && PintaCore.LivePreview.IsEnabled)
					layer = CreateLivePreviewLayer (layer);

				// If the layer is offset, handle it here
				if (!layer.Transform.IsIdentity ())
					layer = CreateOffsetLayer (layer);

				var src = layer.Surface;

				// Get the blend mode for this layer and opacity
				var blend_op = UserBlendOps.GetBlendOp (layer.BlendMode, layer.Opacity);

				if (checker)
					checker_op = new CheckerBoardOperation (layer.Opacity);

				const int fpShift = 12;
				const int fpFactor = (1 << fpShift);

				Gdk.Size sourceSize = src.GetBounds ().Size;
				long fDstLeftLong = ((long)offset.X * fpFactor * (long)sourceSize.Width) / (long)destinationSize.Width;
				long fDstTopLong = ((long)offset.Y * fpFactor * (long)sourceSize.Height) / (long)destinationSize.Height;
				long fDstRightLong = ((long)(offset.X + dst.Width) * fpFactor * (long)sourceSize.Width) / (long)destinationSize.Width;
				long fDstBottomLong = ((long)(offset.Y + dst.Height) * fpFactor * (long)sourceSize.Height) / (long)destinationSize.Height;
				int fDstLeft = (int)fDstLeftLong;
				int fDstTop = (int)fDstTopLong;
				int fDstRight = (int)fDstRightLong;
				int fDstBottom = (int)fDstBottomLong;
				int dx = (fDstRight - fDstLeft) / dst.Width;
				int dy = (fDstBottom - fDstTop) / dst.Height;

				ColorBgra* src_ptr = (ColorBgra*)src.DataPtr;
				ColorBgra* dst_ptr = (ColorBgra*)dst.DataPtr;
				int src_width = src.Width;
				int dst_width = dst.Width;
			
				for (int dstRow = 0, fDstY = fDstTop; dstRow < dst.Height && fDstY < fDstBottom; ++dstRow, fDstY += dy) {
					int srcY1 = fDstY >> fpShift;                            // y
					int srcY2 = (fDstY + (dy >> 2)) >> fpShift;              // y + 0.25
					int srcY3 = (fDstY + (dy >> 1)) >> fpShift;              // y + 0.50
					int srcY4 = (fDstY + (dy >> 1) + (dy >> 2)) >> fpShift;  // y + 0.75

					ColorBgra* src1 = src.GetRowAddressUnchecked (src_ptr, src_width, srcY1);
					ColorBgra* src2 = src.GetRowAddressUnchecked (src_ptr, src_width, srcY2);
					ColorBgra* src3 = src.GetRowAddressUnchecked (src_ptr, src_width, srcY3);
					ColorBgra* src4 = src.GetRowAddressUnchecked (src_ptr, src_width, srcY4);
					ColorBgra* dstPtr = dst.GetRowAddressUnchecked (dst_ptr, dst_width, dstRow);
					int checkerY = dstRow + offset.Y;
					int checkerX = offset.X;
					int maxCheckerX = checkerX + dst.Width;

					for (int fDstX = fDstLeft; checkerX < maxCheckerX && fDstX < fDstRight; ++checkerX, fDstX += dx) {
						int srcX1 = (fDstX + (dx >> 2)) >> fpShift;             // x + 0.25
						int srcX2 = (fDstX + (dx >> 1) + (dx >> 2)) >> fpShift; // x + 0.75
						int srcX3 = fDstX >> fpShift;                           // x
						int srcX4 = (fDstX + (dx >> 1)) >> fpShift;             // x + 0.50
						ColorBgra* p1 = src1 + srcX1;
						ColorBgra* p2 = src2 + srcX2;
						ColorBgra* p3 = src3 + srcX3;
						ColorBgra* p4 = src4 + srcX4;

						int r = (2 + p1->R + p2->R + p3->R + p4->R) >> 2;
						int g = (2 + p1->G + p2->G + p3->G + p4->G) >> 2;
						int b = (2 + p1->B + p2->B + p3->B + p4->B) >> 2;
						int a = (2 + p1->A + p2->A + p3->A + p4->A) >> 2;

						// Blend it over the checkerboard background
						if (checker)
							*dstPtr = checker_op.Apply (ColorBgra.FromUInt32 ((uint)b + ((uint)g << 8) + ((uint)r << 16) + ((uint)a << 24)), checkerX, checkerY);
						else
							*dstPtr = blend_op.Apply (*dstPtr, ColorBgra.FromUInt32 ((uint)b + ((uint)g << 8) + ((uint)r << 16) + ((uint)a << 24)));
					
						++dstPtr;
					}
				}

				// Only checker the first layer
				checker = false;
			}
		}

		private unsafe void RenderPixelGrid (Cairo.ImageSurface dst, Gdk.Point offset)
		{
			// Draw horizontal lines
			var dst_ptr = (ColorBgra*)dst.DataPtr; 
			int dstHeight = dst.Height;
			int dstWidth = dst.Width;
			int dstStride = dst.Stride;
			int sTop = d2sLookupY[offset.Y];
			int sBottom = d2sLookupY[offset.Y + dstHeight];

			for (int srcY = sTop; srcY <= sBottom; ++srcY) {
				int dstY = s2dLookupY[srcY];
				int dstRow = dstY - offset.Y;

				if (dstRow >= 0 && dstRow < dstHeight) {
					ColorBgra* dstRowPtr = dst.GetRowAddressUnchecked (dst_ptr, dstWidth, dstRow);
					ColorBgra* dstRowEndPtr = dstRowPtr + dstWidth;

					dstRowPtr += offset.X & 1;

					while (dstRowPtr < dstRowEndPtr) {
						*dstRowPtr = ColorBgra.Black;
						dstRowPtr += 2;
					}
				}
			}

			// Draw vertical lines
			int sLeft = d2sLookupX[offset.X];
			int sRight = d2sLookupX[offset.X + dstWidth];

			for (int srcX = sLeft; srcX <= sRight; ++srcX) {
				int dstX = s2dLookupX[srcX];
				int dstCol = dstX - offset.X;

				if (dstCol >= 0 && dstCol < dstWidth) {
					byte* dstColPtr = (byte*)dst.GetPointAddress (dstCol, 0);
					byte* dstColEndPtr = dstColPtr + dstStride * dstHeight;

					dstColPtr += (offset.Y & 1) * dstStride;

					while (dstColPtr < dstColEndPtr) {
						*((ColorBgra*)dstColPtr) = ColorBgra.Black;
						dstColPtr += 2 * dstStride;
					}
				}
			}
		}

		private int[] CreateLookupX (int srcWidth, int dstWidth, ScaleFactor scaleFactor)
		{
			var lookup = new int[dstWidth + 1];

			for (int x = 0; x < lookup.Length; ++x) {
				Gdk.Point pt = new Gdk.Point (x, 0);
				Gdk.Point clientPt = scaleFactor.ScalePoint (pt);

				// Sometimes the scale factor is slightly different on one axis than
				// on another, simply due to accuracy. So we have to clamp this value to
				// be within bounds.
				lookup[x] = Utility.Clamp (clientPt.X, 0, srcWidth - 1);
			}

			return lookup;
		}

		private int[] CreateLookupY (int srcHeight, int dstHeight, ScaleFactor scaleFactor)
		{
			var lookup = new int[dstHeight + 1];

			for (int y = 0; y < lookup.Length; ++y) {
				Gdk.Point pt = new Gdk.Point (0, y);
				Gdk.Point clientPt = scaleFactor.ScalePoint (pt);

				// Sometimes the scale factor is slightly different on one axis than
				// on another, simply due to accuracy. So we have to clamp this value to
				// be within bounds.
				lookup[y] = Utility.Clamp (clientPt.Y, 0, srcHeight - 1);
			}

			return lookup;
		}
		
		private int[] CreateS2DLookupX(int srcWidth, int dstWidth, ScaleFactor scaleFactor)
		{
			var lookup = new int[srcWidth + 1];

			for (int x = 0; x < lookup.Length; ++x) {
				Gdk.Point pt = new Gdk.Point (x, 0);
				Gdk.Point clientPt = scaleFactor.UnscalePoint (pt);

				// Sometimes the scale factor is slightly different on one axis than
				// on another, simply due to accuracy. So we have to clamp this value to
				// be within bounds.
				lookup[x] = Utility.Clamp (clientPt.X, 0, dstWidth - 1);
			}

			return lookup;
		}

		private int[] CreateS2DLookupY (int srcHeight, int dstHeight, ScaleFactor scaleFactor)
		{
			var lookup = new int[srcHeight + 1];

			for (int y = 0; y < lookup.Length; ++y) {
				Gdk.Point pt = new Gdk.Point (0, y);
				Gdk.Point clientPt = scaleFactor.UnscalePoint (pt);

				// Sometimes the scale factor is slightly different on one axis than
				// on another, simply due to accuracy. So we have to clamp this value to
				// be within bounds.
				lookup[y] = Utility.Clamp (clientPt.Y, 0, dstHeight - 1);
			}

			return lookup;
		}
		#endregion
	}
}
