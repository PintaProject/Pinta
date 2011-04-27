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
	public class AutoLevelEffect : BaseEffect
	{
		UnaryPixelOps.Level op;

		public override string Icon {
			get { return "Menu.Adjustments.AutoLevel.png"; }
		}

		public override string Name {
			get { return Mono.Unix.Catalog.GetString ("Auto Level"); }
		}

		public override Gdk.Key AdjustmentMenuKey {
			get { return Gdk.Key.L; }
		}

		public override void Render (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			if (op == null) {
				HistogramRgb histogram = new HistogramRgb ();
				histogram.UpdateHistogram (src, new Gdk.Rectangle (0, 0, src.Width, src.Height));
				
				op = histogram.MakeLevelsAuto ();
			}
			
			if (op.isValid)
				op.Apply (dest, src, rois);
		}
	}
}
