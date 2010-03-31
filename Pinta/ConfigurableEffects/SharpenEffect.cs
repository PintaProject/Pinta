// 
// Sharpen Effect.cs
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
	public class SharpenEffect : LocalHistogramEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Photo.Sharpen.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Sharpen"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}
		
		public SharpenData Data { get { return EffectData as SharpenData; } }
		
		public SharpenEffect ()
		{
			EffectData = new SharpenData ();
		}
		
		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}
		
		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			foreach (Gdk.Rectangle rect in rois)
				RenderRect (Data.Amount, src, dest, rect);
		}
		
		public unsafe override ColorBgra Apply (ColorBgra src, int area, int* hb, int* hg, int* hr, int* ha)
		{
			ColorBgra median = GetPercentile(50, area, hb, hg, hr, ha);
			return ColorBgra.Lerp(src, median, -0.5f);
		}
	}
	
	public class SharpenData : EffectData
	{
		[MinimumValue (1), MaximumValue (20)]
		public int Amount = 2;
	}
}

