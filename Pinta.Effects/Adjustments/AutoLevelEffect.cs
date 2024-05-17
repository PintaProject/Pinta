/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class AutoLevelEffect : BaseEffect<DBNull>
{
	UnaryPixelOps.Level? op;

	public sealed override bool IsTileable
		=> true;

	public override string Icon
		=> Pinta.Resources.Icons.AdjustmentsAutoLevel;

	public override string Name
		=> Translations.GetString ("Auto Level");

	public override string AdjustmentMenuKey
		=> "L";

	public AutoLevelEffect (IServiceProvider _) { }

	public override DBNull GetPreRender (ImageSurface src, ImageSurface dst)
		=> DBNull.Value;

	public override void Render (
		DBNull preRender,
		ImageSurface src,
		ImageSurface dest,
		ReadOnlySpan<RectangleI> rois)
	{
		if (op is null) {
			HistogramRgb histogram = new ();
			histogram.UpdateHistogram (src, new RectangleI (0, 0, src.Width, src.Height));

			op = histogram.MakeLevelsAuto ();
		}

		if (op.IsValid)
			op.Apply (dest, src, rois);
	}
}
