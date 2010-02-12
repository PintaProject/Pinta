// 
// HueSaturationDialog.cs
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
using Pinta.Core;

namespace Pinta
{
	public partial class HueSaturationDialog : Gtk.Dialog
	{
		private int hueLevel = 0;
		private int saturationLevel = 100;
		private int lightnessLevel = 0;
		
		public int HueLevel {
			get { return hueLevel; }
		}
		
		public int SaturationLevel {
			get { return saturationLevel; }
		}
		
		public int LightnessLevel {
			get { return lightnessLevel; }
		}
		
		public HueSaturationDialog ()
		{
			this.Build ();
			
			Icon = PintaCore.Resources.GetIcon ("Menu.Adjustments.HueAndSaturation.png");
			
			hscaleHue.ValueChanged += hscaleHue_ValueChanged;
			hscaleSaturation.ValueChanged += hscaleSaturation_ValueChanged;
			hscaleLightness.ValueChanged += hscaleLightness_ValueChanged;
			
			spinHue.ValueChanged += spinHue_ValueChanged;
			spinSaturation.ValueChanged += spinSaturation_ValueChanged;
			spinLightness.ValueChanged += spinLightness_ValueChanged;
			
			spinHue.Value = hueLevel;
			spinSaturation.Value = saturationLevel;
			spinLightness.Value = lightnessLevel;
			
			buttonHue.Clicked += buttonHue_Clicked;
			buttonSaturation.Clicked += buttonSaturation_Clicked;
			buttonLightness.Clicked += buttonLightness_Clicked;
		}
		
		#region Private Methods
		void hscaleHue_ValueChanged (object o, EventArgs args)
		{
			spinHue.Value = hscaleHue.Value;
		}
		
		void hscaleSaturation_ValueChanged (object o, EventArgs args)
		{
			spinSaturation.Value = hscaleSaturation.Value;
		}
		
		void hscaleLightness_ValueChanged (object o, EventArgs args)
		{
			spinLightness.Value = hscaleLightness.Value;
		}
		
		void spinHue_ValueChanged (object o, EventArgs args)
		{
			hscaleHue.Value = spinHue.Value;
			hueLevel = spinHue.ValueAsInt;
		}
		
		void spinSaturation_ValueChanged (object o, EventArgs args)
		{
			hscaleSaturation.Value = spinSaturation.Value;
			saturationLevel = spinSaturation.ValueAsInt;
		}
		
		void spinLightness_ValueChanged (object o, EventArgs args)
		{
			hscaleLightness.Value = spinLightness.Value;
			lightnessLevel = spinLightness.ValueAsInt;
		}
		
		void buttonHue_Clicked (object o, EventArgs args)
		{
			spinHue.Value = 0;	
		}
		
		void buttonSaturation_Clicked (object o, EventArgs args)
		{
			spinSaturation.Value = 100;
		}
		
		void buttonLightness_Clicked (object o, EventArgs args)
		{
			spinLightness.Value = 0;
		}
		#endregion
	}
}
