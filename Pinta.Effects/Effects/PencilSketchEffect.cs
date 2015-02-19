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
			get { return Catalog.GetString ("Pencil Sketch"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Artistic"); }
		}

		public PencilSketchData Data { get { return EffectData as PencilSketchData; } }
		
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
		public unsafe override void Render (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			bacAdjustment.Data.Brightness = -Data.ColorRange;
			bacAdjustment.Data.Contrast = -Data.ColorRange;
			bacAdjustment.Render (src, dest, rois);

			blurEffect.Data.Radius = Data.PencilTipSize;
			blurEffect.Render (src, dest, rois);

			invertEffect.Render (dest, dest, rois);
			desaturateOp.Apply (dest, dest, rois);

			ColorBgra* dst_dataptr = (ColorBgra*)dest.DataPtr;
			int dst_width = dest.Width;
			ColorBgra* src_dataptr = (ColorBgra*)src.DataPtr;
			int src_width = src.Width;
		
			foreach (Gdk.Rectangle roi in rois) {
				for (int y = roi.Top; y <= roi.GetBottom (); ++y) {
					ColorBgra* srcPtr = src.GetPointAddressUnchecked (src_dataptr, src_width, roi.X, y);
					ColorBgra* dstPtr = dest.GetPointAddressUnchecked (dst_dataptr, dst_width, roi.X, y);

					for (int x = roi.Left; x <= roi.GetRight (); ++x) {
						ColorBgra srcGrey = desaturateOp.Apply (*srcPtr);
						ColorBgra sketched = colorDodgeOp.Apply (srcGrey, *dstPtr);
						*dstPtr = sketched;

						++srcPtr;
						++dstPtr;
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
