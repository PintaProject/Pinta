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

public sealed class SepiaEffect (IServiceProvider _) : BaseEffect
{
	static readonly UnaryPixelOp desaturate = new UnaryPixelOps.Desaturate ();
	static readonly UnaryPixelOp level = new UnaryPixelOps.Level (
			ColorBgra.Black,
			ColorBgra.White,
			[1.2f, 1.0f, 0.8f],
			ColorBgra.Black,
			ColorBgra.White);

	public sealed override bool IsTileable
		=> true;

	public override string Icon
		=> Resources.Icons.AdjustmentsSepia;

	public override string Name
		=> Translations.GetString ("Sepia");

	public override string AdjustmentMenuKey
		=> "E";

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		desaturate.Apply (destination, source, roi);
		level.Apply (destination, destination, roi);
	}
}
