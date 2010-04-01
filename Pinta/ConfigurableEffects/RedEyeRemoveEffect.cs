// 
// RedEyeRemoveEffect.cs
//  
// Author:
//       Krzysztof Marecki <marecki.krzysztof@gmail.com>
// 
// Copyright (c) 2010 Krzysztof Marecki
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
	public class RedEyeRemoveEffect : BaseEffect
	{
		private UnaryPixelOp op;
		
		public override string Icon {
			get { return "Menu.Effects.Photo.RedEyeRemove.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Red Eye Removal"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public RedEyeRemoveData Data { get { return EffectData as RedEyeRemoveData; } }
		
		public RedEyeRemoveEffect ()
		{
			EffectData = new RedEyeRemoveData ();
		}
		
		public override bool LaunchConfiguration ()
		{
			SimpleEffectDialog dialog = new SimpleEffectDialog (Text, PintaCore.Resources.GetIcon (Icon), Data);

			// Hookup event handling for live preview.
			dialog.EffectDataChanged += (o, e) => {
				if (EffectData != null) {
					op = new UnaryPixelOps.RedEyeRemove (Data.Tolerance, Data.Saturation);
					EffectData.FirePropertyChanged (e.PropertyName);
				}
			};
			
			int response = dialog.Run ();
			bool ret = (response == (int)Gtk.ResponseType.Ok);
			dialog.Destroy ();
			
			return ret;
		}
		
		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			op.Apply (dest, src, rois);
		}
	}
	
	public class RedEyeRemoveData : EffectData
	{
		[MinimumValue (0), MaximumValue (100)]
		public int Tolerance = 70;
		
		[MinimumValue (0), MaximumValue (100)]
		[Caption ("Saturation percentage")]
		[Hint ("Hint : For best results, first use selection tools to select each eye")]
		public int Saturation = 90;
	}
}

