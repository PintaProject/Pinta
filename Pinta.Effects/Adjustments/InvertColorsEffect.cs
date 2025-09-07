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

public sealed class InvertColorsEffect (IServiceProvider _) : BaseEffect
{
	static readonly UnaryPixelOp invert = new UnaryPixelOps.Invert ();

	public sealed override bool IsTileable
		=> true;

	public override string Icon
		=> Resources.Icons.AdjustmentsInvertColors;

	public override string Name
		=> Translations.GetString ("Invert Colors");

	public override string AdjustmentMenuKey
		=> "I";

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
		=> invert.Apply (destination, source, roi);
}
