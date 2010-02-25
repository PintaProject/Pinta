// 
// HueSaturationEffect.cs
//  
// Author:
//       Krzysztof Marecki <marecki.krzysztof@gmail.com>
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
	public class HueSaturationEffect : BaseEffect
	{
		private int hue_delta;
		private int sat_delta;
		private int lightness;
		
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

		public override bool LaunchConfiguration ()
		{
			HueSaturationData data = new HueSaturationData ();
			SimpleEffectDialog dialog = new SimpleEffectDialog (Text, PintaCore.Resources.GetIcon (Icon), data);
	
			int response = dialog.Run ();
			
			if (response == (int)Gtk.ResponseType.Ok) {
				hue_delta = data.Hue;
				sat_delta = data.Saturation;
				lightness = data.Lightness;
				
				dialog.Destroy ();

				// Don't trigger anything if no options were changed
				return !data.IsDefault;
			}

			dialog.Destroy ();

			return false;
		}

		public override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			if (hue_delta == 0 && sat_delta == 100 && lightness == 0)
				op = new UnaryPixelOps.Identity ();
			else
				op = new UnaryPixelOps.HueSaturationLightness (hue_delta, sat_delta, lightness);

			op.Apply (dest, src, rois);
		}

		private class HueSaturationData
		{
			[MinimumValue (-180), MaximumValue (180)]
			public int Hue = 0;
			
			[MinimumValue (0), MaximumValue (200)]
			public int Saturation = 100;

			public int Lightness = 0;
			
			[Skip]
			public bool IsDefault {
				get { return Hue == 0 && Saturation == 100 && Lightness == 0; }
			}
		}
	}
}
