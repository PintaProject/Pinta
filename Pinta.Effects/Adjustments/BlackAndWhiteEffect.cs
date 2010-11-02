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

namespace Pinta.Effects
{
	//[System.ComponentModel.Composition.Export (typeof (BaseEffect))]
	public class BlackAndWhiteEffect : BaseEffect
	{
		UnaryPixelOp op = new UnaryPixelOps.Desaturate ();

		public override string Icon {
			get { return "Menu.Adjustments.BlackAndWhite.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Black and White"); }
		}

		public override EffectAdjustment EffectOrAdjustment {
			get { return EffectAdjustment.Adjustment; }
		}

		public override Gdk.Key AdjustmentMenuKey {
			get { return Gdk.Key.L; }
		}
		
		public override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			op.Apply (dest, src, rois);
		}
	}
}
