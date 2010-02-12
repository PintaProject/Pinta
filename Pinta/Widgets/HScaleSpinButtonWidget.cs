// 
// HScaleSpinButtonWidget.cs
//  
// Author:
//       Krzysztof Marecki
// 
// Copyright (c) 2010 Krzysztof Marecki <marecki.krzysztof@gmail.com>
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
using System.ComponentModel;

namespace Pinta
{

	[System.ComponentModel.ToolboxItem(true)]
	public partial class HScaleSpinButtonWidget : Gtk.Bin
	{

		[Category("Custom Properties")]
		public string Label {
			get { return label.Text; }
			set { label.Text = value; }
		}
		
		[Category("Custom Properties")]
		public int DefaultValue { get; set; }
		
		private int max_value;
		[Category("Custom Properties")]
		public int MaximumValue {
			get { return max_value; }
			set {
				max_value = value;
				hscale.Adjustment.Upper = value;
				spin.Adjustment.Upper = value;
			}
		}
	
		private int min_value;
		[Category("Custom Properties")]
		public int MinimumValue {
			get { return min_value; }
			set {
				min_value = value;
				hscale.Adjustment.Lower = value;
				spin.Adjustment.Lower = value;
			}
		}
		
		public int Value {
			get { return spin.ValueAsInt; }
			set { spin.Value = value; }
		}
		
		public HScaleSpinButtonWidget ()
		{
			this.Build ();
			
			hscale.ValueChanged += HandleHscaleValueChanged;
			spin.ValueChanged +=  HandleSpinValueChanged;
			button.Clicked +=  HandleButtonClicked;
			
			Value = DefaultValue;
		}

		private void HandleHscaleValueChanged (object sender, EventArgs e)
		{
			spin.Value = hscale.Value;
		}
		
		private void HandleSpinValueChanged (object sender, EventArgs e)
		{
			hscale.Value = spin.Value;
		}
		
		private void HandleButtonClicked (object sender, EventArgs e)
		{
			Value = DefaultValue;
		}
	}
}
