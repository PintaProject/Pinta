// 
// PolarInversionEffect.cs
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
	public class PolarInversionEffect : WarpEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Distort.PolarInversion.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Polar Inversion"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public PolarInversionData Data {
			get { return EffectData as PolarInversionData; }
		}

		public PolarInversionEffect ()
		{
			EffectData = new PolarInversionData ();
		}

		#region Algorithm Code Ported From PDN
		protected override void InverseTransform (ref TransformData transData)
		{
			double x = transData.X;
			double y = transData.Y;
			
			// NOTE: when x and y are zero, this will divide by zero and return NaN
			double invertDistance = Utility.Lerp (1.0, DefaultRadius2 / ((x * x) + (y * y)), Data.Amount);
			
			transData.X = x * invertDistance;
			transData.Y = y * invertDistance;
		}
		#endregion

		public class PolarInversionData : WarpEffect.WarpData
		{
			[MinimumValue(-4), MaximumValue(4)]
			public double Amount = 0;

			public PolarInversionData () : base()
			{
				EdgeBehavior = WarpEdgeBehavior.Reflect;
			}
			
		}
	}
}
