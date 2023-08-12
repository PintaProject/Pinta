/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class InvertColorsEffect : BaseEffect
{
	readonly UnaryPixelOp op = new UnaryPixelOps.Invert ();

	public override string Icon => Pinta.Resources.Icons.AdjustmentsInvertColors;

	public override string Name => Translations.GetString ("Invert Colors");

	public override string AdjustmentMenuKey => "I";

	public override void Render (ImageSurface src, ImageSurface dest, Core.RectangleI[] rois)
	{
		op.Apply (dest, src, rois);
	}
}
