//
// ReliefEffect.cs
//  
// Author:
//       Marco Rolappe <m_rolappe@gmx.net>
// 
// Copyright (c) 2010 Marco Rolappe
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

using Pinta.Gui.Widgets;


namespace Pinta.Core
{
	public class ReliefEffect : ColorDifferenceEffect
	{
		public ReliefEffect () {
			EffectData = new ReliefData ();
		}

		public ReliefData Data {
			get { return EffectData as ReliefData; }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override bool LaunchConfiguration () {
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		public override string Icon {
			get { return "Menu.Effects.Stylize.Relief.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Relief"); }
		}

		#region Algorithm Code Ported From PDN
		public override void RenderEffect (Cairo.ImageSurface src, Cairo.ImageSurface dst, Gdk.Rectangle[] rois) {
			base.RenderColorDifferenceEffect (Weights, src, dst, rois);
		}

		private double[][] Weights {
			get {
				// adjust and convert angle to radians
				double r = (double)Data.Angle * 2.0 * Math.PI / 360.0;
				
				// angle delta for each weight
				double dr = Math.PI / 4.0;
				
				// for r = 0 this builds an Relief filter pointing straight left
				double[][] weights = new double[3][];
				
				for (uint idx = 0; idx < 3; ++idx) {
					weights[idx] = new double[3];
				}
				
				weights[0][0] = Math.Cos (r + dr);
				weights[0][1] = Math.Cos (r + 2.0 * dr);
				weights[0][2] = Math.Cos (r + 3.0 * dr);
				
				weights[1][0] = Math.Cos (r);
				weights[1][1] = 1;
				weights[1][2] = Math.Cos (r + 4.0 * dr);
				
				weights[2][0] = Math.Cos (r - dr);
				weights[2][1] = Math.Cos (r - 2.0 * dr);
				weights[2][2] = Math.Cos (r - 3.0 * dr);
				
				return weights;
			}
		}
		#endregion
	}


	public class ReliefData : EffectData
	{
		public double Angle = 45;
	}
}
