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

public sealed class InvertColorsEffect : BaseEffect<DBNull>
{
	readonly UnaryPixelOp op = new UnaryPixelOps.Invert ();

	public sealed override bool IsTileable
		=> true;

	public override string Icon
		=> Pinta.Resources.Icons.AdjustmentsInvertColors;

	public override string Name
		=> Translations.GetString ("Invert Colors");

	public override string AdjustmentMenuKey
		=> "I";

	public InvertColorsEffect (IServiceProvider _) { }

	public override DBNull GetPreRender (ImageSurface src, ImageSurface dst)
		=> DBNull.Value;

	public override void Render (
		DBNull preRender,
		ImageSurface src,
		ImageSurface dest,
		ReadOnlySpan<RectangleI> rois)
		=> op.Apply (dest, src, rois);
}
