// 
// GlowEffect.cs
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
	public class GlowEffect : BaseEffect
	{
		private GaussianBlurEffect blurEffect;
		private BrightnessContrastEffect contrastEffect;
		private UserBlendOps.ScreenBlendOp screenBlendOp;
		
		public override string Icon {
			get { return "Menu.Effects.Photo.Glow.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Glow"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public GlowData Data { get; private set; }
		
		public GlowEffect ()
		{
			Data = new GlowData ();
			
			blurEffect = new GaussianBlurEffect ();
			contrastEffect = new BrightnessContrastEffect ();
			screenBlendOp = new UserBlendOps.ScreenBlendOp ();
		}
		
		public override bool LaunchConfiguration ()
		{
			SimpleEffectDialog dialog = new SimpleEffectDialog (Text, PintaCore.Resources.GetIcon (Icon), Data);

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok) {
				dialog.Destroy ();
				return true;
			}

			dialog.Destroy ();

			return false;
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

		public class GlowData
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
