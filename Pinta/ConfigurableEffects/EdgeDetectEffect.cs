// 
// EdgeDetectEffect.cs
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
	public class EdgeDetectEffect : ColorDifferenceEffect
	{
		private double[][] weights;
		
		public override string Icon {
			get { return "Menu.Effects.Stylize.EdgeDetect.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Edge Detect"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public EdgeDetectData Data { get { return EffectData as EdgeDetectData; } }
		
		public EdgeDetectEffect ()
		{
			EffectData = new EdgeDetectData ();
		}
		
		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}
		
		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			SetWeights ();
			base.RenderColorDifferenceEffect (weights, src, dest, rois);
		}
		
		private void SetWeights ()
		{
			weights = new double[3][];
            for (int i = 0; i < this.weights.Length; ++i) {
                this.weights[i] = new double[3];
            }

            // adjust and convert angle to radians
            double r = (double)Data.Angle * 2.0 * Math.PI / 360.0;

            // angle delta for each weight
            double dr = Math.PI / 4.0;

            // for r = 0 this builds an edge detect filter pointing straight left

            this.weights[0][0] = Math.Cos(r + dr);
            this.weights[0][1] = Math.Cos(r + 2.0 * dr);
            this.weights[0][2] = Math.Cos(r + 3.0 * dr);

            this.weights[1][0] = Math.Cos(r);
            this.weights[1][1] = 0;
            this.weights[1][2] = Math.Cos(r + 4.0 * dr);

            this.weights[2][0] = Math.Cos(r - dr);
            this.weights[2][1] = Math.Cos(r - 2.0 * dr);
            this.weights[2][2] = Math.Cos(r - 3.0 * dr);
		}
	}
	
	public class EdgeDetectData : EffectData
	{
		public double Angle = 45;
	}
}
