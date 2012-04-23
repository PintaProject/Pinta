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
	class NewCanvasRenderer
	{
		private Size source_size;
		private Size destination_size;
		private Layer scratch_layer;
		private Layer offset_layer;

		private ScaleFactor scale_factor;
		private bool generated;
		private int[] d2sLookupX;
		private int[] d2sLookupY;
		private int[] s2dLookupX;
		private int[] s2dLookupY;

		public ScaleFactor ScaleFactor { get {return scale_factor;}}
		public int[] Dst2SrcLookupX { get {return d2sLookupX;}}
		public int[] Dst2SrcLookupY { get {return d2sLookupY;}}
		public int[] Src2DstLookupX { get {return s2dLookupX;}}
		public int[] Src2DstLookupY { get {return s2dLookupY;}}

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

		private Layer ScratchLayer {
			get {
				// Create one if we don't have one
				if (scratch_layer == null)
					scratch_layer = new Layer (new Cairo.ImageSurface (Cairo.Format.ARGB32, source_size.Width, source_size.Height));

				// If we have the wrong size one, dispose it and create the correct size
				if (scratch_layer.Surface.Width != source_size.Width || scratch_layer.Surface.Height != source_size.Height) {
					(scratch_layer.Surface as IDisposable).Dispose ();
					scratch_layer = new Layer (new Cairo.ImageSurface (Cairo.Format.ARGB32, source_size.Width, source_size.Height));
				}

				return scratch_layer;
			}
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
			var scratch = ScratchLayer;
			scratch.Surface.Clear ();

			using (var g = new Cairo.Context (scratch.Surface)) {
				g.SetSource (original.Surface);
				g.Paint ();

				g.Save ();

				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.Clip ();
				g.SetSource (PintaCore.LivePreview.LivePreviewSurface);
				g.Paint ();

				g.Restore ();
			}

			scratch.BlendMode = original.BlendMode;
			scratch.Offset = original.Offset;
			scratch.Opacity = original.Opacity;

			return scratch;
		}

		private Layer CreateOffsetLayer (Layer original, Point canvas_offset)
		{
			var offset = OffsetLayer;
			offset.Surface.Clear ();

			using (var g = new Cairo.Context (offset.Surface)) {
				g.SetSourceSurface (original.Surface, canvas_offset.X + (int)original.Offset.X, canvas_offset.Y + (int)original.Offset.Y);
				g.Paint ();
			}

			offset.BlendMode = original.BlendMode;
			offset.Offset = original.Offset;
			offset.Opacity = original.Opacity;

			return offset;
		}

		#region Algorithms ported from PDN
		private unsafe void RenderOneToOne (List<Layer> layers, Cairo.ImageSurface dst, Gdk.Point offset)
		{
			// The first layer should be blended with the transparent checkerboard
			var checker = true;

			for (int i = 0; i < layers.Count; i++) {
				var layer = layers[i];

				// If we're in LivePreview, substitute current layer with the preview layer
				if (layer == PintaCore.Layers.CurrentLayer && PintaCore.LivePreview.IsEnabled)
					layer = CreateLivePreviewLayer (layer);

				// If the layer is offset, handle it here
				if (!layer.Offset.IsEmpty ())
					layer = CreateOffsetLayer (layer, offset);

				var src = layer.Surface;

				// Get the blend mode for this layer and opacity
				var blend_op = UserBlendOps.GetBlendOp (layer.BlendMode, layer.Opacity);
				
				// Figure out where our source and destination intersect
				var srcRect = new Gdk.Rectangle (offset, dst.GetBounds ().Size);
				srcRect.Intersect (src.GetBounds ());

				// Get pointers to our surfaces
				var src_ptr = (ColorBgra*)src.DataPtr;
				var dst_ptr = (ColorBgra*)dst.DataPtr;

				// Cache widths and heights
				int src_width = src.Width;
				int dst_width = dst.Width;
				int dst_height = dst.Height;

				for (int dstRow = 0; dstRow < srcRect.Height; ++dstRow) {
					ColorBgra* dstRowPtr = dst.GetRowAddressUnchecked (dst_ptr, dst_width, dstRow);
					ColorBgra* srcRowPtr = src.GetPointAddressUnchecked (src_ptr, src_width, offset.X, dstRow + offset.Y);

					int dstCol = offset.X;
					int dstColEnd = offset.X + srcRect.Width;
					int checkerY = dstRow + offset.Y;

					while (dstCol < dstColEnd) {
						// Blend it over the checkerboard background
						if (checker) {
							int b = srcRowPtr->B;
							int g = srcRowPtr->G;
							int r = srcRowPtr->R;
							int a = srcRowPtr->A;

							int v = (((dstCol ^ checkerY) & 8) << 3) + 191;
							a = a + (a >> 7);
							int vmia = v * (256 - a);

							r = ((r * a) + vmia) >> 8;
							g = ((g * a) + vmia) >> 8;
							b = ((b * a) + vmia) >> 8;

							dstRowPtr->Bgra = (uint)b + ((uint)g << 8) + ((uint)r << 16) + ((uint)255 << 24);
						} else {
							*dstRowPtr = blend_op.Apply (*dstRowPtr, *srcRowPtr);
						}
					
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
			// The first layer should be blended with the transparent checkerboard
			var checker = true;

			for (int i = 0; i < layers.Count; i++) {
				var layer = layers[i];

				// If we're in LivePreview, substitute current layer with the preview layer
				if (layer == PintaCore.Layers.CurrentLayer && PintaCore.LivePreview.IsEnabled)
					layer = CreateLivePreviewLayer (layer);

				// If the layer is offset, handle it here
				if (!layer.Offset.IsEmpty ())
					layer = CreateOffsetLayer (layer, offset);

				var src = layer.Surface;

				// Get the blend mode for this layer and opacity
				var blend_op = UserBlendOps.GetBlendOp (layer.BlendMode, layer.Opacity);
				
				ColorBgra* src_ptr = (ColorBgra*)src.DataPtr;
				ColorBgra* dst_ptr = (ColorBgra*)dst.DataPtr;

				int src_width = src.Width;
				int dst_width = dst.Width;
				int dst_height = dst.Height;

				if (!generated) {
					d2sLookupX = CreateLookupX (src_width, destination_size.Width, scale_factor);
					d2sLookupY = CreateLookupY (src.Height, destination_size.Height, scale_factor);
					s2dLookupX = CreateS2DLookupX (src_width, destination_size.Width, scale_factor);
					s2dLookupY = CreateS2DLookupY (src.Height, destination_size.Height, scale_factor);

					generated = true;
				}

				for (int dstRow = 0; dstRow < dst_height; ++dstRow) {
					int nnY = dstRow + offset.Y;
					int srcY = d2sLookupY[nnY];

					ColorBgra* dstPtr = dst.GetRowAddressUnchecked (dst_ptr, dst_width, dstRow);
					ColorBgra* srcRow = src.GetRowAddressUnchecked (src_ptr, src_width, srcY);

					for (int dstCol = 0; dstCol < dst_width; ++dstCol) {
						int nnX = dstCol + offset.X;
						int srcX = d2sLookupX[nnX];
					
						if (checker) {
							ColorBgra src2 = *(srcRow + srcX);
							int b = src2.B;
							int g = src2.G;
							int r = src2.R;
							int a = src2.A;

							// Blend it over the checkerboard background
							int v = (((dstCol + offset.X) ^ (dstRow + offset.Y)) & 8) * 8 + 191;
							a = a + (a >> 7);
							int vmia = v * (256 - a);

							r = ((r * a) + vmia) >> 8;
							g = ((g * a) + vmia) >> 8;
							b = ((b * a) + vmia) >> 8;

							dstPtr->Bgra = (uint)b + ((uint)g << 8) + ((uint)r << 16) + ((uint)255 << 24);
						} else {
							*dstPtr = blend_op.Apply (*dstPtr, *(srcRow + srcX));
						}

						++dstPtr;
					}
				}

				// Only checker the first layer
				checker = false;
			}
		}

		private unsafe void RenderZoomOut (List<Layer> layers, Cairo.ImageSurface dst, Gdk.Point offset, Gdk.Size destinationSize)
		{
			// The first layer should be blended with the transparent checkerboard
			var checker = true;

			for (int i = 0; i < layers.Count; i++) {
				var layer = layers[i];

				// If we're in LivePreview, substitute current layer with the preview layer
				if (layer == PintaCore.Layers.CurrentLayer && PintaCore.LivePreview.IsEnabled)
					layer = CreateLivePreviewLayer (layer);

				// If the layer is offset, handle it here
				if (!layer.Offset.IsEmpty ())
					layer = CreateOffsetLayer (layer, offset);

				var src = layer.Surface;

				// Get the blend mode for this layer and opacity
				var blend_op = UserBlendOps.GetBlendOp (layer.BlendMode, layer.Opacity);
				
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

						if (checker) {
							// Blend it over the checkerboard background
							int v = ((checkerX ^ checkerY) & 8) * 8 + 191;
							a = a + (a >> 7);
							int vmia = v * (256 - a);

							r = ((r * a) + vmia) >> 8;
							g = ((g * a) + vmia) >> 8;
							b = ((b * a) + vmia) >> 8;

							dstPtr->Bgra = (uint)b + ((uint)g << 8) + ((uint)r << 16) + 0xff000000;
						} else {
							*dstPtr = blend_op.Apply (*dstPtr, ColorBgra.FromUInt32 ((uint)b + ((uint)g << 8) + ((uint)r << 16) + ((uint)a << 24)));
						}
					
						++dstPtr;
					}
				}

				// Only checker the first layer
				checker = false;
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
