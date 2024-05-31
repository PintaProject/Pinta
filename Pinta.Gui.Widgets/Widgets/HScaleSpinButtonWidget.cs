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
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class HScaleSpinButtonWidget : Box
{
	private readonly Scale hscale;
	private readonly SpinButton spin;
	private readonly Button button;
	private readonly Label title_label;

	private int max_value;
	private int min_value;
	private int digits_value;
	private double inc_value;

	private readonly double initial_value;

	public HScaleSpinButtonWidget (double initialValue)
	{
		// Build

		const int spacing = 6;

		initial_value = initialValue;

		// Section label + line
		Box labelAndLine = new () { Spacing = spacing };
		labelAndLine.SetOrientation (Orientation.Horizontal);
		Label titleLabel = new ();
		titleLabel.AddCssClass (AdwaitaStyles.Title4);
		labelAndLine.Append (titleLabel);

		title_label = titleLabel;

		// Slider + spinner + reset button
		Box controls = new () { Spacing = spacing };
		controls.SetOrientation (Orientation.Horizontal);

		hscale = Scale.NewWithRange (Orientation.Horizontal, 2, 64, 1);
		hscale.CanFocus = true;
		hscale.DrawValue = false;
		hscale.Digits = 0;
		hscale.ValuePos = PositionType.Top;
		hscale.Hexpand = true;
		hscale.Halign = Align.Fill;

		controls.Append (hscale);

		spin = SpinButton.NewWithRange (0, 100, 1);
		spin.CanFocus = true;
		spin.ClimbRate = 1;
		spin.Numeric = true;
		spin.Adjustment!.PageIncrement = 10;

		controls.Append (spin);

		// Reset button
		button = new Button {
			IconName = Resources.StandardIcons.GoPrevious,
			WidthRequest = 28,
			HeightRequest = 24,
			CanFocus = true,
			UseUnderline = true
		};
		controls.Append (button);

		// Main layout
		SetOrientation (Orientation.Vertical);
		Spacing = spacing;
		Append (labelAndLine);
		Append (controls);

		// ---------------

		hscale.OnValueChanged += HandleHscaleValueChanged;
		spin.OnValueChanged += HandleSpinValueChanged;
		button.OnClicked += HandleButtonClicked;

		OnRealize += (_, _) => Value = initialValue;

		spin.SetActivatesDefault (true);
	}

	public string Label {
		get => title_label.GetText ();
		set => title_label.SetText (value);
	}

	public int MaximumValue {
		get => max_value;
		set {
			max_value = value;
			hscale.Adjustment!.Upper = value;
			spin.Adjustment!.Upper = value;
		}
	}

	public int MinimumValue {
		get => min_value;
		set {
			min_value = value;
			hscale.Adjustment!.Lower = value;
			spin.Adjustment!.Lower = value;
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
			hscale.Adjustment!.StepIncrement = value;
			spin.Adjustment!.StepIncrement = value;
		}
	}

	public int ValueAsInt => spin.GetValueAsInt ();

	public double Value {
		get => spin.Value;
		set {
			if (spin.Value != value) {
				spin.Value = value;
				OnValueChanged ();
			}
		}
	}

	private void HandleHscaleValueChanged (object? sender, EventArgs e)
	{
		if (spin.Value != hscale.GetValue ()) {
			spin.Value = hscale.GetValue ();
			OnValueChanged ();
		}
	}

	private void HandleSpinValueChanged (object? sender, EventArgs e)
	{
		if (hscale.GetValue () != spin.Value) {
			hscale.SetValue (spin.Value);
			OnValueChanged ();
		}
	}

	private void HandleButtonClicked (object? sender, EventArgs e)
	{
		Value = initial_value;
	}

	private void OnValueChanged () => ValueChanged?.Invoke (this, EventArgs.Empty);

	public event EventHandler? ValueChanged;
}
