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
using Pinta.Gui.Widgets;
using Pinta.Effects;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Effects
{
	//[System.ComponentModel.Composition.Export (typeof (BaseEffect))]
	public class GlowEffect : BaseEffect
	{
		private GaussianBlurEffect blurEffect;
		private BrightnessContrastEffect contrastEffect;
		private UserBlendOps.ScreenBlendOp screenBlendOp;
		
		public override string Icon {
			get { return "Menu.Effects.Photo.Glow.png"; }
		}

		public override string Text {
			get { return Catalog.GetString ("Glow"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Photo"); }
		}

		public GlowData Data { get { return EffectData as GlowData; } }
		
		public GlowEffect ()
		{
			EffectData = new GlowData ();
			
			blurEffect = new GaussianBlurEffect ();
			contrastEffect = new BrightnessContrastEffect ();
			screenBlendOp = new UserBlendOps.ScreenBlendOp ();
		}
		
		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			blurEffect.Data.Radius = Data.Radius;
			blurEffect.RenderEffect (src, dest, rois);

			contrastEffect.Data.Brightness = Data.Brightness;
			contrastEffect.Data.Contrast = Data.Contrast;
			contrastEffect.RenderEffect (dest, dest, rois);

			foreach (Gdk.Rectangle roi in rois) {
				for (int y = roi.Top; y < roi.Bottom; ++y) {
					ColorBgra* dstPtr = dest.GetPointAddressUnchecked (roi.Left, y);
					ColorBgra* srcPtr = src.GetPointAddressUnchecked (roi.Left, y);

					screenBlendOp.Apply (dstPtr, srcPtr, dstPtr, roi.Width);
				}
			}
		}
		#endregion

		public class GlowData : EffectData
		{
			[MinimumValue (1), MaximumValue (20)]
			public int Radius = 6;
			[MinimumValue (-100), MaximumValue (100)]
			public int Brightness = 10;
			[MinimumValue (-100), MaximumValue (100)]
			public int Contrast = 10;
		}
	}
}
