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

	private readonly bool enable_live_preview;
	private readonly bool enable_background_pattern;

	private Size source_size;
	private Size destination_size;
	private Fraction<int> scale_factor;
	private double scale_ratio;

	public CanvasRenderer (bool enableLivePreview, bool enableBackgroundPattern = true)
	{
		enable_live_preview = enableLivePreview;
		enable_background_pattern = enableBackgroundPattern;
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

		g.Translate (-offset.X, -offset.Y);

		// Create the transparent checkerboard background
		if (enable_background_pattern)
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

		dst.MarkDirty ();
	}
}
