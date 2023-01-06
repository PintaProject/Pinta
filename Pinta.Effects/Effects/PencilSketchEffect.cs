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
	public class PencilSketchEffect : BaseEffect
	{
		private GaussianBlurEffect blurEffect;
		private UnaryPixelOps.Desaturate desaturateOp;
		private InvertColorsEffect invertEffect;
		private BrightnessContrastEffect bacAdjustment;
		private UserBlendOps.ColorDodgeBlendOp colorDodgeOp;

		public override string Icon {
			get { return "Menu.Effects.Artistic.PencilSketch.png"; }
		}

		public override string Name {
			get { return Translations.GetString ("Pencil Sketch"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Translations.GetString ("Artistic"); }
		}

		public PencilSketchData Data { get { return (PencilSketchData) EffectData!; } } // NRT - Set in constructor

		public PencilSketchEffect ()
		{
			EffectData = new PencilSketchData ();

			blurEffect = new GaussianBlurEffect ();
			desaturateOp = new UnaryPixelOps.Desaturate ();
			invertEffect = new InvertColorsEffect ();
			bacAdjustment = new BrightnessContrastEffect ();
			colorDodgeOp = new UserBlendOps.ColorDodgeBlendOp ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public override void Render (ImageSurface src, ImageSurface dest, Core.RectangleI[] rois)
		{
			bacAdjustment.Data.Brightness = -Data.ColorRange;
			bacAdjustment.Data.Contrast = -Data.ColorRange;
			bacAdjustment.Render (src, dest, rois);

			blurEffect.Data.Radius = Data.PencilTipSize;
			blurEffect.Render (src, dest, rois);

			invertEffect.Render (dest, dest, rois);
			desaturateOp.Apply (dest, dest, rois);

			var dst_data = dest.GetPixelData ();
			int dst_width = dest.Width;
			var src_data = src.GetReadOnlyPixelData ();
			int src_width = src.Width;

			foreach (Core.RectangleI roi in rois) {
				for (int y = roi.Top; y <= roi.Bottom; ++y) {
					var src_row = src_data.Slice (y * src_width, src_width);
					var dst_row = dst_data.Slice (y * dst_width, dst_width);

					for (int x = roi.Left; x <= roi.Right; ++x) {
						ColorBgra srcGrey = desaturateOp.Apply (src_row[x]);
						dst_row[x] = colorDodgeOp.Apply (srcGrey, dst_row[x]);
					}
				}
			}
		}
		#endregion

		public class PencilSketchData : EffectData
		{
			[Caption ("Pencil Tip Size"), MinimumValue (1), MaximumValue (20)]
			public int PencilTipSize = 2;

			[Caption ("Color Range"), MinimumValue (-20), MaximumValue (20)]
			public int ColorRange = 0;
		}
	}
}
