/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Krzysztof Marecki <marecki.krzysztof@gmail.com>         //
/////////////////////////////////////////////////////////////////////////////////

// Additional code:
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
using Mono.Unix;
using Cairo;

using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects
{
	public partial class LevelsDialog : Gtk.Dialog
	{	
		private bool[] mask;
		
		public LevelsData EffectData { get; private set; }
		
		public LevelsDialog (LevelsData effectData) : base (Catalog.GetString ("Levels Adjustment"),
		                                                    PintaCore.Chrome.MainWindow, DialogFlags.Modal)
		{			
			this.Build ();
			
			EffectData = effectData;			
			mask = new bool[] {true, true, true};

			this.HasSeparator = false;
			//hack allowing adding hbox with rgb checkboxes into dialog action area
			VBox.Remove (hboxBottom);
			foreach (Widget widget in hboxBottom)
			{
                hboxBottom.Remove (widget);
				if (widget == buttonOk)
					AddActionWidget (widget, ResponseType.Ok);
				else
                    ActionArea.PackEnd (widget);
			}

			UpdateInputHistogram ();
			Reset ();
			UpdateLevels ();
			
			checkRed.Toggled += HandleCheckRedToggled;
			checkGreen.Toggled += HandleCheckGreenToggled;
			checkBlue.Toggled += HandleCheckBlueToggled;
			buttonReset.Clicked += HandleButtonResetClicked;
			buttonAuto.Clicked += HandleButtonAutoClicked;
			buttonCancel.Clicked += HandleButtonCancelClicked;
			buttonOk.Clicked += HandleButtonOkClicked;
			spinInLow.ValueChanged += HandleSpinInLowValueChanged;
			spinInHigh.ValueChanged +=  HandleSpinInHighValueChanged;
			spinOutLow.ValueChanged += HandleSpinOutLowValueChanged;
			spinOutGamma.ValueChanged += HandleSpinOutGammaValueChanged;
			spinOutHigh.ValueChanged += HandleSpinOutHighValueChanged;
			gradientInput.ValueChanged += HandleGradientInputValueChanged;
			gradientOutput.ValueChanged += HandleGradientOutputValueChanged;
			gradientInput.ButtonReleaseEvent += HandleGradientButtonReleaseEvent;
			gradientOutput.ButtonReleaseEvent += HandleGradientButtonReleaseEvent;
			gradientInput.ButtonPressEvent += HandleGradientButtonPressEvent;
			gradientOutput.ButtonPressEvent += HandleGradientButtonPressEvent;
			colorpanelInLow.ButtonPressEvent += HandleColorPanelButtonPressEvent;
			colorpanelInHigh.ButtonPressEvent += HandleColorPanelButtonPressEvent;
			colorpanelOutLow.ButtonPressEvent += HandleColorPanelButtonPressEvent;
			colorpanelOutHigh.ButtonPressEvent += HandleColorPanelButtonPressEvent;

			if (Gtk.Global.AlternativeDialogButtonOrder (this.Screen)) {
				hboxBottom.ReorderChild (buttonCancel, 0);
			}

			buttonOk.CanDefault = true;
			DefaultResponse = ResponseType.Ok;
			spinInLow.ActivatesDefault = true;
			spinInHigh.ActivatesDefault = true;
			spinOutGamma.ActivatesDefault = true;
			spinOutLow.ActivatesDefault = true;
			spinOutHigh.ActivatesDefault = true;
		}
		
		private UnaryPixelOps.Level Levels {
			get {
				if (EffectData == null)
					throw new InvalidOperationException ("Effect data not set on levels dialog.");
				
				return EffectData.Levels;
			}
			
			set {
				if (value == null)
					throw new ArgumentNullException ();
				
				EffectData.Levels = value;
			}
		}		
		
		private void UpdateLivePreview ()
		{
			if (EffectData != null)
				EffectData.FirePropertyChanged ("Levels");
		}
		
		private void UpdateInputHistogram ()
		{
			ImageSurface surface = PintaCore.Layers.CurrentLayer.Surface;
			Gdk.Rectangle rect =  PintaCore.Workspace.ActiveDocument.Selection.SelectionPath.GetBounds ();
			histogramInput.Histogram.UpdateHistogram (surface, rect);
			UpdateOutputHistogram ();
		}

		private void UpdateOutputHistogram ()
		{
			histogramOutput.Histogram.SetFromLeveledHistogram(histogramInput.Histogram, Levels);
		}
		
		private void Reset ()
		{
			histogramOutput.ResetHistogram ();
			
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
		
		private void UpdateFromLevelsOp ()
		{
			disable_updating = true;
			
			spinInHigh.Value = MaskAvg (Levels.ColorInHigh);
            		spinInLow.Value = MaskAvg (Levels.ColorInLow);
			
			float gamma = MaskGamma ();
            		int lo = MaskAvg (Levels.ColorOutLow);
            		int hi = MaskAvg (Levels.ColorOutHigh);
			
	            	spinOutHigh.Value = hi;
	            	spinOutGamma.Value = gamma;
	            	spinOutLow.Value = lo;
			
			disable_updating = false;
		}
		
		private void HandleButtonAutoClicked (object sender, EventArgs e)
		{
			Levels = histogramInput.Histogram.MakeLevelsAuto ();
			
			UpdateFromLevelsOp ();
			UpdateLevels ();
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
		
		private float MaskGamma () 
	        {
	            int count = 0;
	            float total = 0;

	            for (int c = 0; c < 3; c++) {
	                if (mask [c]) {
	                    total += Levels.GetGamma (c);
	                    count++;
	                }
	            }
	
	            if (count > 0) {
	                return total / count;
	            } else {
	                return 1;
	            }
	    
	        }
		
		private void UpdateGammaByMask (float val) 
		{
		    float average = -1;

		    if (!(mask [0] || mask [1] || mask [2]))
		        return;

		    do {
		        average = MaskGamma ();
		        float factor = val / average;
		
		        for (int c = 0; c < 3; c++) {
		            if (mask [c]) {
		                Levels.SetGamma (c, factor * Levels.GetGamma (c));
		            }
		        }
		    } while (Math.Abs (val - average) > 0.001);
		}
		
		private Color GetOutMidColor ()
		{
			return  Levels.Apply (histogramInput.Histogram.GetMeanColor ()).ToCairoColor ();
		}
		
		//hack to avoid reccurent invocation of UpdateLevels
		private bool disable_updating;
		//when user moves triangles inside gradient widget,
		//we don't want to redraw histogram each time Levels values change.
		//maximum number of skipped updates
		private const int max_skip = 5;
		//skipped updates counter
		private int skip_counter = max_skip;
		private bool button_down = false;
		
		private void UpdateLevels ()
		{
			if(disable_updating)
				return;
			
			disable_updating = true;
			
			if(skip_counter == max_skip || !button_down) {
				
				Levels.ColorOutHigh = UpdateByMask (Levels.ColorOutHigh, (byte)spinOutHigh.Value);
	            		Levels.ColorOutLow = UpdateByMask (Levels.ColorOutLow, (byte)spinOutLow.Value);
				UpdateGammaByMask ((float) spinOutGamma.Value);
				
	           		Levels.ColorInHigh = UpdateByMask (Levels.ColorInHigh, (byte)spinInHigh.Value);
	           		Levels.ColorInLow = UpdateByMask (Levels.ColorInLow, (byte)spinInLow.Value);
				
				colorpanelInLow.SetCairoColor (Levels.ColorInLow.ToCairoColor ());
				colorpanelInHigh.SetCairoColor (Levels.ColorInHigh.ToCairoColor ());
				
				colorpanelOutLow.SetCairoColor (Levels.ColorOutLow.ToCairoColor ());
				colorpanelOutMid.SetCairoColor (GetOutMidColor ());
				colorpanelOutHigh.SetCairoColor (Levels.ColorOutHigh.ToCairoColor ());
				
				UpdateOutputHistogram ();
				skip_counter = 0;
			} else
				skip_counter++;
				
			GdkWindow.Invalidate ();
			disable_updating = false;
			
			UpdateLivePreview ();
		}
		
		private void HandleGradientButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			button_down = true;	
		}
		
		private void HandleGradientButtonReleaseEvent (object o, ButtonReleaseEventArgs args)
		{
			button_down = false;
			
			if (skip_counter != 0)
				UpdateLevels ();
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
			if (gradientOutput.ValueIndex != -1 && gradientOutput.ValueIndex != e.Index)
				return;
			
			int val = gradientOutput.GetValue (e.Index);
			int hi = gradientOutput.GetValue (2);
			int lo = gradientOutput.GetValue (0);
			int med = FromGammaValue ();
			
			switch (e.Index) {
			case 0 : 
				spinOutLow.Value = val;
				gradientOutput.SetValue (1, med);
				break;
				
			case 1 :
				med = gradientOutput.GetValue (1);
				spinOutGamma.Value = Utility.Clamp(1 / Math.Log (0.5, (float)(med - lo) / (float)(hi - lo)), 0.1, 10.0);
				break;
			
			case 2 :
				spinOutHigh.Value = val;
				gradientOutput.SetValue (1, med);
				break;
			}
			
			UpdateLevels ();
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
		
		private void HandleColorPanelButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			if (args.Event.Type != Gdk.EventType.TwoButtonPress)
				return;
			
			Gtk.ColorSelectionDialog csd = new Gtk.ColorSelectionDialog ("Choose Color");
            
           		ColorPanelWidget panel = (ColorPanelWidget)sender;
			csd.ColorSelection.PreviousColor = panel.CairoColor.ToGdkColor ();
			csd.ColorSelection.CurrentColor = panel.CairoColor.ToGdkColor ();
			csd.ColorSelection.CurrentAlpha = panel.CairoColor.GdkColorAlpha ();	
                   
			int response = csd.Run ();

			if (response == (int)Gtk.ResponseType.Ok) {
                    
                ColorBgra col = csd.ColorSelection.CurrentColor.ToBgraColor ();

                if (panel == colorpanelInLow)	{
                    Levels.ColorInLow = col;
                } else if (panel == colorpanelInHigh) {
                    Levels.ColorInHigh = col;
                } else if (panel == colorpanelOutLow) {
                    Levels.ColorOutLow = col;
//                } else if (panel == colorpanelOutMid) {
//                    ColorBgra lo = Levels.ColorInLow;
//                    ColorBgra md = histogramInput.Histogram.GetMeanColor();
//                    ColorBgra hi = Levels.ColorInHigh;
//                    ColorBgra out_lo = Levels.ColorOutLow;
//                    ColorBgra out_hi = Levels.ColorOutHigh;
//
//                    for (int i = 0; i < 3; i++) {
//                        double logA = (col[i] - out_lo[i]) / (out_hi[i] - out_lo[i]);
//                        double logBase = (md[i] - lo[i]) / (hi[i] - lo[i]);
//                        double logVal = (logBase == 1.0) ? 0.0 : Math.Log (logA, logBase);
//
//                        Levels.SetGamma(i, (float)Utility.Clamp (logVal, 0.1, 10.0));
//                    }
                } else if (panel == colorpanelOutHigh) {
                    Levels.ColorOutHigh = col;
                } 
            }
			
			csd.Destroy ();
			UpdateFromLevelsOp ();
			UpdateLevels ();
		}
	}
}
