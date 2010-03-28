// 
// PencilSketchEffect.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Cairo;
using Pinta.Gui.Widgets;

namespace Pinta.Core
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

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Pencil Sketch"); }
		}

		public override bool IsConfigurable {
			get { return true; }
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
		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			bacAdjustment.Data.Brightness = -Data.ColorRange;
			bacAdjustment.Data.Contrast = -Data.ColorRange;
			bacAdjustment.RenderEffect (src, dest, rois);

			blurEffect.Data.Radius = Data.PencilTipSize;
			blurEffect.RenderEffect (src, dest, rois);

			invertEffect.RenderEffect (dest, dest, rois);
			desaturateOp.Apply (dest, dest, rois);

			ColorBgra* dst_dataptr = (ColorBgra*)dest.DataPtr;
			int dst_width = dest.Width;
			ColorBgra* src_dataptr = (ColorBgra*)src.DataPtr;
			int src_width = src.Width;
		
			foreach (Gdk.Rectangle roi in rois) {
				for (int y = roi.Top; y < roi.Bottom; ++y) {
					ColorBgra* srcPtr = src.GetPointAddressUnchecked (src_dataptr, src_width, roi.X, y);
					ColorBgra* dstPtr = dest.GetPointAddressUnchecked (dst_dataptr, dst_width, roi.X, y);

					for (int x = roi.Left; x < roi.Right; ++x) {
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
			
			[MinimumValue (-20), MaximumValue (20)]
			public int ColorRange = 0;
		}
	}
}
