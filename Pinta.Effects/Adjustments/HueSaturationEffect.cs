/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Krzysztof Marecki <marecki.krzysztof@gmail.com>         //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Gui.Widgets;
using Pinta.Core;

namespace Pinta.Effects
{
	//[System.ComponentModel.Composition.Export (typeof (BaseEffect))]
	public class HueSaturationEffect : BaseEffect
	{		
		UnaryPixelOp op;

		public override string Icon {
			get { return "Menu.Adjustments.HueAndSaturation.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Hue / Saturation"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override EffectAdjustment EffectOrAdjustment {
			get { return EffectAdjustment.Adjustment; }
		}

		public override Gdk.Key AdjustmentMenuKey {
			get { return Gdk.Key.U; }
		}

		public HueSaturationEffect ()
		{
			EffectData = new HueSaturationData ();
		}		
		
		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		public override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			int hue_delta = Data.Hue;
			int sat_delta =  Data.Saturation;
			int lightness = Data.Lightness;
			
			if (op == null) {
				if (hue_delta == 0 && sat_delta == 100 && lightness == 0)
					op = new UnaryPixelOps.Identity ();
				else
					op = new UnaryPixelOps.HueSaturationLightness (hue_delta, sat_delta, lightness);
			}
			
			op.Apply (dest, src, rois);
		}

		private HueSaturationData Data { get { return EffectData as HueSaturationData; } }
		
		private class HueSaturationData : EffectData
		{
			[MinimumValue (-180), MaximumValue (180)]
			public int Hue = 0;
			
			[MinimumValue (0), MaximumValue (200)]
			public int Saturation = 100;

			public int Lightness = 0;
			
			[Skip]
			public override bool IsDefault {
				get { return Hue == 0 && Saturation == 100 && Lightness == 0; }
			}
		}
	}
}
