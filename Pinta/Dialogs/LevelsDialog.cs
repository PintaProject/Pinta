// 
// LevelsDialog.cs
//  
// Author:
//      Krzysztof Marecki <marecki.krzysztof@gmail.com>
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
using Gtk;
using Cairo;

using Pinta.Core;

namespace Pinta
{
	public partial class LevelsDialog : Gtk.Dialog
	{	
		private bool[] mask;
			
		public UnaryPixelOps.Level Levels { get; private set; }
		
		public LevelsDialog ()
		{
			this.Build ();
			this.Levels = new UnaryPixelOps.Level ();
			mask = new bool[] {true, true, true};
		
			this.HasSeparator = false;
			//hack allowing adding hbox with rgb checkboxes into dialog action area
			VBox.Remove(hboxBottom);
			AddActionWidget(hboxBottom, ResponseType.None);
			
			checkRed.Toggled += HandleCheckRedToggled;
			checkGreen.Toggled += HandleCheckGreenToggled;
			checkBlue.Toggled += HandleCheckBlueToggled;
			buttonReset.Clicked += HandleButtonResetClicked;
			buttonCancel.Clicked += HandleButtonCancelClicked;
			buttonOk.Clicked += HandleButtonOkClicked;
			spinInLow.ValueChanged += HandleSpinInLowValueChanged;
			spinInHigh.ValueChanged +=  HandleSpinInHighValueChanged;
			spinOutLow.ValueChanged += HandleSpinOutLowValueChanged;
			spinOutGamma.ValueChanged += HandleSpinOutGammaValueChanged;
			spinOutHigh.ValueChanged += HandleSpinOutHighValueChanged;
			gradientInput.ValueChanged += HandleGradientInputValueChanged;
			gradientOutput.ValueChanged += HandleGradientOutputValueChanged;
			
			MotionNotifyEvent += HandleMotionNotifyEvent;
			
			UpdateInputHistogram ();
			Reset ();
		}
		
		private void UpdateInputHistogram ()
		{
			ImageSurface surface = PintaCore.Layers.CurrentLayer.Surface;
			Gdk.Rectangle rect =  PintaCore.Layers.SelectionPath.GetBounds ();
			histogramInput.Histogram.UpdateHistogram (surface, rect);
			UpdateOutputHistogram ();
		}

		private void UpdateOutputHistogram()
		{
			histogramOutput.Histogram.SetFromLeveledHistogram(histogramInput.Histogram, Levels);
		}
		
		private void Reset ()
		{
			spinInLow.Value = 0;
			spinInHigh.Value = 255;
			spinOutLow.Value = 0;
			spinOutGamma.Value = 1.0;
			spinOutHigh.Value = 255;
			
		}

		private void HandleButtonResetClicked (object sender, EventArgs e)
		{
			Reset ();
		}

		private void HandleSpinInLowValueChanged (object sender, EventArgs e)
		{
			gradientInput.SetValue (0, spinInLow.ValueAsInt);
		}

		private void HandleSpinInHighValueChanged (object sender, EventArgs e)
		{
			gradientInput.SetValue (1, spinInHigh.ValueAsInt);
		}

		private void HandleSpinOutLowValueChanged (object sender, EventArgs e)
		{
			gradientOutput.SetValue (0, spinOutLow.ValueAsInt);
		}
		
		private int FromGammaValue ()
		{
			int lo = gradientOutput.GetValue (0);
			int hi = gradientOutput.GetValue (2);
			int med = (int)(lo + (hi - lo) * Math.Pow (0.5, spinOutGamma.Value));
			
			return med;
		}

		private void HandleSpinOutGammaValueChanged (object sender, EventArgs e)
		{
			gradientOutput.SetValue (1, FromGammaValue ());
		}

		private void HandleSpinOutHighValueChanged (object sender, EventArgs e)
		{
			gradientOutput.SetValue (2, spinOutHigh.ValueAsInt);
		}
		
		private int MaskAvg(ColorBgra before) 
		{
            int count = 0, total = 0;   

            for (int c = 0; c < 3; c++) {
                if (mask [c]) {
                    total += before [c];
                    count++;
                }
            }

            if (count > 0) {
                return total / count;
            } 
            else {
                return 0;
            }
        }
		
		private ColorBgra UpdateByMask (ColorBgra before, byte val) 
        {
            ColorBgra after = before;
            int average = -1, oldaverage = -1;

            if (!(mask [0] || mask [1] || mask [2])) {
                return before;
            }

            do {
                float factor;

                oldaverage = average;
                average = MaskAvg (after);

                if (average == 0) {
                    break;
                }
                factor = (float)val / average;

                for (int c = 0; c < 3; c++) {
                    if (mask [c]) {
                        after [c] = (byte)Utility.ClampToByte (after [c] * factor);
                    }
                }
            } while (average != val && oldaverage != average);

            while (average != val) {
                average = MaskAvg (after);
                int diff = val - average;

                for (int c = 0; c < 3; c++) {
                    if (mask [c]) {
                        after [c] = (byte)Utility.ClampToByte(after [c] + diff);
                    }
                }
            }

            after.A = 255;
            return after;           
        }
		
		private void UpdateLevels ()
		{
			Levels.ColorOutHigh = UpdateByMask (Levels.ColorOutHigh, (byte)spinOutHigh.Value);
            Levels.ColorOutLow = UpdateByMask (Levels.ColorOutLow, (byte)spinOutLow.Value);

            Levels.ColorInHigh = UpdateByMask (Levels.ColorInHigh, (byte)spinInHigh.Value);
            Levels.ColorInLow = UpdateByMask (Levels.ColorInLow, (byte)spinInLow.Value);
			
			UpdateOutputHistogram ();
		}
		
		private void HandleGradientInputValueChanged (object sender, IndexEventArgs e)
		{
			int val = gradientInput.GetValue (e.Index);
		
			if (e.Index == 0)
				spinInLow.Value = val;
			else
				spinInHigh.Value = val;
			
			UpdateLevels ();
		}
		
		private void HandleGradientOutputValueChanged (object sender, IndexEventArgs e)
		{
			int val = gradientOutput.GetValue (e.Index);
			int hi = gradientOutput.GetValue (2);
			int lo = gradientOutput.GetValue (0);
			int med = FromGammaValue ();
			
			switch (e.Index) {
			case 0 : 
				spinOutLow.Value = val;
				break;
				
			case 1 :
				med = gradientOutput.GetValue (1);
				spinOutGamma.Value = Utility.Clamp(1 / Math.Log(0.5, (float)(med - lo) / (float)(hi - lo)), 0.1, 10.0);;
				break;
			
			case 2 :
				spinOutHigh.Value = val;
				break;
			}
			
			gradientOutput.SetValue (1, med);
			
			UpdateLevels ();
		}
		
		private void HandleMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			//gradientInput.MotionNotify ();
			//gradientOutput.MotionNotify ();
		}

		private void MaskChanged ()
		{
			ColorBgra max = ColorBgra.Black;

            max.Bgra |= mask[0] ? (uint)0xFF0000 : 0;
            max.Bgra |= mask[1] ? (uint)0xFF00 : 0;
            max.Bgra |= mask[2] ? (uint)0xFF : 0;
			
			Color maxcolor = max.ToCairoColor ();
			gradientInput.MaxColor = maxcolor;
			gradientOutput.MaxColor = maxcolor;
			
			for (int i = 0; i < 3; i++) {
                histogramInput.SetSelected (i, mask[i]);
                histogramOutput.SetSelected (i, mask[i]);
            }
			
			GdkWindow.Invalidate ();
		}
		
		private void HandleCheckRedToggled (object sender, EventArgs e)
		{
			mask [0] = checkRed.Active;
			MaskChanged();
		}
		
		private void HandleCheckGreenToggled (object sender, EventArgs e)
		{
			mask [1] = checkGreen.Active;
			MaskChanged ();
		}

		private void HandleCheckBlueToggled (object sender, EventArgs e)
		{
			mask [2] = checkBlue.Active;
			MaskChanged ();
		}
		
		private void HandleButtonOkClicked (object sender, EventArgs e)
		{
			Respond (ResponseType.Ok);
		}

		private void HandleButtonCancelClicked (object sender, EventArgs e)
		{
			Respond (ResponseType.Cancel);
		}
	}
}
