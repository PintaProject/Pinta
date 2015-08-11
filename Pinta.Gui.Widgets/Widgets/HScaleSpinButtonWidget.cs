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
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem(true)]
	public class HScaleSpinButtonWidget : FilledAreaBin
	{
        private HScale hscale;
        private SpinButton spin;
        private Button button;
        private Label label;

        [Category ("Custom Properties")]
		public string Label {
			get { return label.Text; }
			set { label.Text = value; }
		}

		[Category("Custom Properties")]
		public double DefaultValue { get; set; }

        private int max_value;
		[Category("Custom Properties")]
        public int MaximumValue
        {
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
        private int digits_value;
        [Category("Custom Properties")]
        public int DigitsValue
        {
            get { return digits_value; }
            set
            {
                if (value > 0)
                {
                    
                    digits_value = value;
                    hscale.Digits = value;
                    spin.Digits = Convert.ToUInt32(value);
                }
            }
        }

        private double inc_value;
        [Category("Custom Properties")]
        public double IncrementValue
        {
            get { return inc_value; }
            set
            {
                inc_value = value;
                hscale.Adjustment.StepIncrement = value;
                spin.Adjustment.StepIncrement = value;
            }
        }
        
        [Category("Custom Properties")]
        public int ValueAsInt
        {
            get { return spin.ValueAsInt; }
        }

		[Category("Custom Properties")]
		public double Value {
			get { return spin.Value; }
			set {
				if (spin.Value != value) {
					spin.Value = value;
					OnValueChanged ();
				}
			}
		}

		public HScaleSpinButtonWidget ()
		{
			this.Build ();
			
			hscale.ValueChanged += HandleHscaleValueChanged;
			spin.ValueChanged += HandleSpinValueChanged;
			button.Clicked += HandleButtonClicked;

			spin.ActivatesDefault = true;
		}

		protected override void OnShown ()
		{
			base.OnShown ();
			
			Value = DefaultValue;
		}

		private void HandleHscaleValueChanged (object sender, EventArgs e)
		{
			if (spin.Value != hscale.Value) {
				spin.Value = hscale.Value;
				OnValueChanged ();
			}
		}

		private void HandleSpinValueChanged (object sender, EventArgs e)
		{
			if (hscale.Value != spin.Value) {
				hscale.Value = spin.Value;
				OnValueChanged ();
			}
		}

		private void HandleButtonClicked (object sender, EventArgs e)
		{
			Value = DefaultValue;
		}

		#region Protected Methods
		protected void OnValueChanged ()
		{
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}
		#endregion

		#region Public Events
		public event EventHandler ValueChanged;
        #endregion

        private void Build ()
        {
            // Section label + line
            var hbox1 = new HBox (false, 6);

            label = new Label ();
            hbox1.PackStart (label, false, false, 0);
            hbox1.PackStart (new HSeparator (), true, true, 0);

            // Slider + spinner + reset button
            var hbox2 = new HBox (false, 6);

            hscale = new HScale (2, 64, 1);
            hscale.CanFocus = true;
            hscale.DrawValue = false;
            hscale.Digits = 0;
            hscale.ValuePos = PositionType.Top;
            hbox2.PackStart (hscale, true, true, 0);

            spin = new SpinButton (0, 100, 1);
            spin.CanFocus = true;
            spin.Adjustment.PageIncrement = 10;
            spin.ClimbRate = 1;
            spin.Numeric = true;
            hbox2.PackStart (spin, false, false, 0);

            // Reset button
            button = new Button ();
            button.WidthRequest = 28;
            button.HeightRequest = 24;
            button.CanFocus = true;
            button.UseUnderline = true;

            var button_image = new Image (PintaCore.Resources.GetIcon (Stock.GoBack, 16));
            button.Add (button_image);

            var alignment2 = new Alignment (0.5F, 0F, 1F, 0F);
            alignment2.Add (button);

            hbox2.PackStart (alignment2, false, false, 0);

            // Main layout
            var vbox = new VBox (false, 6);

            vbox.Add (hbox1);
            vbox.Add (hbox2);

            Add (vbox);

            vbox.ShowAll ();
        }
    }
}
