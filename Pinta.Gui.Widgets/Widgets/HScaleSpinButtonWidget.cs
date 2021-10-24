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
using System.Diagnostics.CodeAnalysis;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class HScaleSpinButtonWidget : FilledAreaBin
	{
		private HScale hscale;
		private SpinButton spin;
		private Button button;
		private Label label;

		private int max_value;
		private int min_value;
		private int digits_value;
		private double inc_value;

		public HScaleSpinButtonWidget ()
		{
			Build ();

			hscale.ValueChanged += HandleHscaleValueChanged;
			spin.ValueChanged += HandleSpinValueChanged;
			button.Clicked += HandleButtonClicked;

			spin.ActivatesDefault = true;
		}

		public string Label {
			get => label.Text;
			set => label.Text = value;
		}

		public double DefaultValue { get; set; }

		public int MaximumValue {
			get => max_value;
			set {
				max_value = value;
				hscale.Adjustment.Upper = value;
				spin.Adjustment.Upper = value;
			}
		}

		public int MinimumValue {
			get => min_value;
			set {
				min_value = value;
				hscale.Adjustment.Lower = value;
				spin.Adjustment.Lower = value;
			}
		}

		public int DigitsValue {
			get => digits_value;
			set {
				if (value > 0) {
					digits_value = value;
					hscale.Digits = value;
					spin.Digits = Convert.ToUInt32 (value);
				}
			}
		}

		public double IncrementValue {
			get => inc_value;
			set {
				inc_value = value;
				hscale.Adjustment.StepIncrement = value;
				spin.Adjustment.StepIncrement = value;
			}
		}

		public int ValueAsInt => spin.ValueAsInt;

		public double Value {
			get => spin.Value;
			set {
				if (spin.Value != value) {
					spin.Value = value;
					OnValueChanged ();
				}
			}
		}

		protected override void OnShown ()
		{
			base.OnShown ();

			Value = DefaultValue;
		}

		private void HandleHscaleValueChanged (object? sender, EventArgs e)
		{
			if (spin.Value != hscale.Value) {
				spin.Value = hscale.Value;
				OnValueChanged ();
			}
		}

		private void HandleSpinValueChanged (object? sender, EventArgs e)
		{
			if (hscale.Value != spin.Value) {
				hscale.Value = spin.Value;
				OnValueChanged ();
			}
		}

		private void HandleButtonClicked (object? sender, EventArgs e)
		{
			Value = DefaultValue;
		}

		protected void OnValueChanged () => ValueChanged?.Invoke (this, EventArgs.Empty);

		public event EventHandler? ValueChanged;

		[MemberNotNull (nameof (label), nameof (hscale), nameof (spin), nameof (button))]
		private void Build ()
		{
			// Section label + line
			var hbox1 = new HBox (false, 6);

			label = new Label ();
			hbox1.PackStart (label, false, false, 0);
			hbox1.PackStart (new HSeparator (), true, true, 0);

			// Slider + spinner + reset button
			var hbox2 = new HBox (false, 6);

			hscale = new HScale (2, 64, 1) {
				CanFocus = true,
				DrawValue = false,
				Digits = 0,
				ValuePos = PositionType.Top
			};

			hbox2.PackStart (hscale, true, true, 0);

			spin = new SpinButton (0, 100, 1) {
				CanFocus = true,
				ClimbRate = 1,
				Numeric = true
			};
			spin.Adjustment.PageIncrement = 10;

			hbox2.PackStart (spin, false, false, 0);

			// Reset button
			button = new Button {
				WidthRequest = 28,
				HeightRequest = 24,
				CanFocus = true,
				UseUnderline = true
			};

			var button_image = new Image (IconTheme.Default.LoadIcon (Resources.StandardIcons.GoPrevious, 16));
			button.Add (button_image);

			var alignment2 = new Alignment (0.5F, 0F, 1F, 0F) {
				button
			};

			hbox2.PackStart (alignment2, false, false, 0);

			// Main layout
			var vbox = new VBox (false, 6) {
				hbox1,
				hbox2
			};

			Add (vbox);

			vbox.ShowAll ();
		}
	}
}
