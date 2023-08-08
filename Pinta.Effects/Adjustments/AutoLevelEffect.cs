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

namespace Pinta.Effects
{
	public class AutoLevelEffect : BaseEffect
	{
		UnaryPixelOps.Level? op;

		public override string Icon => Pinta.Resources.Icons.AdjustmentsAutoLevel;

		public override string Name => Translations.GetString ("Auto Level");

		public override string AdjustmentMenuKey => "L";

		public override void Render (ImageSurface src, ImageSurface dest, RectangleI[] rois)
		{
			if (op == null) {
				HistogramRgb histogram = new HistogramRgb ();
				histogram.UpdateHistogram (src, new RectangleI (0, 0, src.Width, src.Height));

				op = histogram.MakeLevelsAuto ();
			}

			if (op.isValid)
				op.Apply (dest, src, rois);
		}
	}
}
