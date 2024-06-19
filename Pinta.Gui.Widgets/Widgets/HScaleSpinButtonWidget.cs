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
	private readonly Button reset_button;
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
		hscale = CreateSlider ();
		spin = CreateSpin ();
		reset_button = CreateResetButton ();
		Box valueControls = new () { Spacing = spacing };
		valueControls.SetOrientation (Orientation.Horizontal);
		valueControls.Append (hscale);
		valueControls.Append (spin);
		valueControls.Append (reset_button);

		// Main layout
		SetOrientation (Orientation.Vertical);
		Spacing = spacing;
		Append (labelAndLine);
		Append (valueControls);

		// ---------------

		OnRealize += (_, _) => Value = initialValue;

		spin.SetActivatesDefaultImmediate (true);
	}

	private Scale CreateSlider ()
	{
		Scale result = Scale.NewWithRange (Orientation.Horizontal, 2, 64, 1);
		result.CanFocus = true;
		result.DrawValue = false;
		result.Digits = 0;
		result.ValuePos = PositionType.Top;
		result.Hexpand = true;
		result.Halign = Align.Fill;
		result.OnValueChanged += HandleHscaleValueChanged;
		return result;
	}

	private Button CreateResetButton ()
	{
		Button result = new () {
			IconName = Resources.StandardIcons.GoPrevious,
			WidthRequest = 28,
			HeightRequest = 24,
			CanFocus = true,
			UseUnderline = true,
		};
		result.OnClicked += HandleButtonClicked;
		return result;
	}

	private SpinButton CreateSpin ()
	{
		SpinButton result = SpinButton.NewWithRange (0, 100, 1);
		result.CanFocus = true;
		result.ClimbRate = 1;
		result.Numeric = true;
		result.Adjustment!.PageIncrement = 10;
		result.OnValueChanged += HandleSpinValueChanged;
		return result;
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
			if (value <= 0) return;
			digits_value = value;
			hscale.Digits = value;
			spin.Digits = Convert.ToUInt32 (value);
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
			if (spin.Value == value) return;
			spin.Value = value;
			OnValueChanged ();
		}
	}

	private void HandleHscaleValueChanged (object? sender, EventArgs e)
	{
		if (spin.Value == hscale.GetValue ()) return;
		spin.Value = hscale.GetValue ();
		OnValueChanged ();
	}

	private void HandleSpinValueChanged (object? sender, EventArgs e)
	{
		if (hscale.GetValue () == spin.Value) return;
		hscale.SetValue (spin.Value);
		OnValueChanged ();
	}

	private void HandleButtonClicked (object? sender, EventArgs e)
	{
		Value = initial_value;
	}

	private void OnValueChanged ()
		=> ValueChanged?.Invoke (this, EventArgs.Empty);

	public event EventHandler? ValueChanged;
}
