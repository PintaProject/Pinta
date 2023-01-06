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
using Pinta.Effects;
using Pinta.Gui.Widgets;

namespace Pinta.Effects
{
	public class GlowEffect : BaseEffect
	{
		private GaussianBlurEffect blurEffect;
		private BrightnessContrastEffect contrastEffect;
		private UserBlendOps.ScreenBlendOp screenBlendOp;

		public override string Icon {
			get { return "Menu.Effects.Photo.Glow.png"; }
		}

		public override string Name {
			get { return Translations.GetString ("Glow"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Translations.GetString ("Photo"); }
		}

		public GlowData Data { get { return (GlowData) EffectData!; } } // NRT - Set in constructor

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
		public override void Render (ImageSurface src, ImageSurface dest, Core.RectangleI[] rois)
		{
			blurEffect.Data.Radius = Data.Radius;
			blurEffect.Render (src, dest, rois);

			contrastEffect.Data.Brightness = Data.Brightness;
			contrastEffect.Data.Contrast = Data.Contrast;
			contrastEffect.Render (dest, dest, rois);

			var dst_data = dest.GetPixelData ();
			var src_data = src.GetReadOnlyPixelData ();
			int src_width = src.Width;
			int dst_width = dest.Width;

			foreach (Core.RectangleI roi in rois) {
				for (int y = roi.Top; y <= roi.Bottom; ++y) {
					var dst_row = dst_data.Slice (y * dst_width + roi.Left, roi.Width);
					var src_row = dst_data.Slice (y * src_width + roi.Left, roi.Width);
					screenBlendOp.Apply (dst_row, src_row, dst_row);
				}
			}
		}
		#endregion

		public class GlowData : EffectData
		{
			[Caption ("Radius"), MinimumValue (1), MaximumValue (20)]
			public int Radius = 6;
			[Caption ("Brightness"), MinimumValue (-100), MaximumValue (100)]
			public int Brightness = 10;
			[Caption ("Contrast"), MinimumValue (-100), MaximumValue (100)]
			public int Contrast = 10;
		}
	}
}
