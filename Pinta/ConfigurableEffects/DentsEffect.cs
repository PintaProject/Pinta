// 
// DentsEffect.cs
//  
// Author:
//       Olivier Dufour <olivier.duff@gmail.com>
// 
// Copyright (c) 2010 Olivier Dufour
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
	public class DentsEffect : WarpEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Distort.Dents.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Dents"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public DentsData Data {
			get { return EffectData as DentsData; }
		}

		public DentsEffect ()
		{
			EffectData = new DentsData ();
		}

		#region Algorithm Code Ported From PDN
		
		public override bool LaunchConfiguration ()
		{
			if (base.LaunchConfiguration ()) {
				Data.PropertyChanged += delegate {
					this.scaleR = (400.0 / base.DefaultRadius) / Data.Scale;
		            this.refractionScale = (Data.Refraction / 100.0) / scaleR;
		            this.theta = Math.PI * 2.0 * Data.Tension / 10.0;
		            //Data.Roughness = Data.Roughness / 100.0;
				}; 
				return true;
			}
			return false;
		}

		private double scaleR;
        private double refractionScale;
        private double theta;
		private double detail;
        
		protected override void InverseTransform(ref TransformData transData)
        {
            double x = transData.X;
            double y = transData.Y;

            double ix = x * scaleR;
            double iy = y * scaleR;

            double bumpAngle = this.theta * PerlinNoise2D.Noise(ix, iy, this.detail, Data.Roughness, (byte)Data.Seed);

            transData.X = x + (this.refractionScale * Math.Sin(-bumpAngle));
            transData.Y = y + (this.refractionScale * Math.Cos(bumpAngle));
        }
		#endregion

		public class DentsData : WarpEffect.WarpData
		{
			[MinimumValue(1), MaximumValue(100)]
			public double Scale = 25;

			[MinimumValue(0), MaximumValue(200)]
			public double Refraction = 50;

			[MinimumValue(0), MaximumValue(100)]
			public double Roughness = 10;

			[MinimumValue(0), MaximumValue(100)]
			public double Tension = 10;

			[MinimumValue(0), MaximumValue(255)]
			public int Seed = 0;

			public DentsData () : base()
			{
				EdgeBehavior = WarpEdgeBehavior.Reflect;
			}
			
		}
	}
}
