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

public sealed class SepiaEffect : BaseEffect
{
	readonly UnaryPixelOp desat = new UnaryPixelOps.Desaturate ();
	readonly UnaryPixelOp level = new UnaryPixelOps.Desaturate ();

	public sealed override bool IsTileable => true;

	public override string Icon => Pinta.Resources.Icons.AdjustmentsSepia;

	public override string Name => Translations.GetString ("Sepia");

	public override string AdjustmentMenuKey => "E";

	public SepiaEffect (IServiceProvider _)
	{
		desat = new UnaryPixelOps.Desaturate ();
		level = new UnaryPixelOps.Level (
			ColorBgra.Black,
			ColorBgra.White,
			[1.2f, 1.0f, 0.8f],
			ColorBgra.Black,
			ColorBgra.White);
	}

	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		desat.Apply (dest, src, rois);
		level.Apply (dest, dest, rois);
	}
}
