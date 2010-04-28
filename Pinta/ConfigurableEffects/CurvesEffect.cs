//
// CurvesEffect.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using Cairo;

namespace Pinta.Core
{

	public class CurvesEffect : BaseEffect
	{			
		public override string Icon {
			get { return "Menu.Adjustments.Curves.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Curves"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}
		
		public CurvesData Data { get { return EffectData as CurvesData; } }
		
		public CurvesEffect ()
		{
			EffectData = new CurvesData ();
		}
		
		public override bool LaunchConfiguration ()
		{
			var dialog = new CurvesDialog (Data);
			dialog.Title = Text;
			dialog.Icon = PintaCore.Resources.GetIcon (Icon);
			
			int response = dialog.Run ();
			
			dialog.Destroy ();
			
			return (response == (int)Gtk.ResponseType.Ok);
		}
		
		public override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			UnaryPixelOp op = MakeUop ();
			
			op.Apply (dest, src, rois);
		}
		
		private UnaryPixelOp MakeUop()
        {
            UnaryPixelOp op;
            byte[][] transferCurves;
            int entries;

            switch (Data.Mode) {
                case ColorTransferMode.Rgb:
                    UnaryPixelOps.ChannelCurve cc = new UnaryPixelOps.ChannelCurve();
                    transferCurves = new byte[][] { cc.CurveR, cc.CurveG, cc.CurveB };
                    entries = 256;
                    op = cc;
                    break;

                case ColorTransferMode.Luminosity:
                    UnaryPixelOps.LuminosityCurve lc = new UnaryPixelOps.LuminosityCurve();
                    transferCurves = new byte[][] { lc.Curve };
                    entries = 256;
                    op = lc;
                    break;

                default:
                    throw new InvalidEnumArgumentException();
            }

            
            int channels = transferCurves.Length;

            for (int channel = 0; channel < channels; channel++) {
                SortedList<int, int> channelControlPoints = Data.ControlPoints[channel];
                IList<int> xa = channelControlPoints.Keys;
                IList<int> ya = channelControlPoints.Values;
                SplineInterpolator interpolator = new SplineInterpolator();
                int length = channelControlPoints.Count;

                for (int i = 0; i < length; i++) {
                    interpolator.Add(xa[i], ya[i]);
                }

                for (int i = 0; i < entries; i++) {
                    transferCurves[channel][i] = Utility.ClampToByte(interpolator.Interpolate(i));
                }
            }

            return op;
        }
	}
	
	public class CurvesData : EffectData
	{
		public SortedList<int, int>[] ControlPoints { get; set; }
		
		public ColorTransferMode Mode { get; set; }
		
		public override EffectData Clone ()
		{
//			Not sure if we have to copy contents of ControlPoints
//			var controlPoints = new SortedList<int, int> [ControlPoints.Length];
//			
//			for (int i = 0; i < ControlPoints.Length; i++)
//				controlPoints[i] = new SortedList<int, int> (ControlPoints[i]);
			
			return new CurvesData () {
				Mode = Mode,
				ControlPoints = ControlPoints
			};
		}
	}
}
