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
using Pinta.Core;

namespace Pinta.Gui.Widgets;

[GObject.Subclass<Gtk.Box>]
public sealed partial class HScaleSpinButtonWidget
{
	private Gtk.Scale h_scale;
	private Gtk.SpinButton spin_button;
	private Gtk.Label title_label;

	private int max_value;
	private int min_value;
	private int digits_value;
	private double inc_value;

	private double initial_value;

	[MemberNotNull (nameof (h_scale), nameof (spin_button), nameof (title_label))]
	partial void Initialize ()
	{
		const int SPACING = 6;

		Gtk.Label titleLabel = Gtk.Label.New (null);
		titleLabel.AddCssClass (AdwaitaStyles.Title4);

		Gtk.Box labelAndLine = Gtk.Box.New (Gtk.Orientation.Horizontal, SPACING);
		labelAndLine.Append (titleLabel);

		Gtk.Scale hScale = Gtk.Scale.NewWithRange (Gtk.Orientation.Horizontal, 2, 64, 1);
		hScale.CanFocus = true;
		hScale.DrawValue = false;
		hScale.Digits = 0;
		hScale.ValuePos = Gtk.PositionType.Top;
		hScale.Hexpand = true;
		hScale.Halign = Gtk.Align.Fill;
		hScale.OnValueChanged += HandleHscaleValueChanged;

		Gtk.SpinButton spinButton = Gtk.SpinButton.NewWithRange (0, 100, 1);
		spinButton.CanFocus = true;
		spinButton.ClimbRate = 1;
		spinButton.Numeric = true;
		spinButton.Adjustment!.PageIncrement = 10;
		spinButton.OnValueChanged += HandleSpinValueChanged;
		spinButton.SetActivatesDefaultImmediate (true);

		Gtk.Button resetButton = Gtk.Button.NewFromIconName (Resources.StandardIcons.GoPrevious);
		resetButton.WidthRequest = 28;
		resetButton.HeightRequest = 24;
		resetButton.CanFocus = true;
		resetButton.UseUnderline = true;
		resetButton.OnClicked += HandleResetButtonClicked;

		Gtk.Box valueControls = Gtk.Box.New (Gtk.Orientation.Horizontal, SPACING);
		valueControls.Append (hScale);
		valueControls.Append (spinButton);
		valueControls.Append (resetButton);

		// --- Initialization (Gtk.Box)

		SetOrientation (Gtk.Orientation.Vertical);
		Spacing = SPACING;
		Append (labelAndLine);
		Append (valueControls);

		// --- References to keep

		title_label = titleLabel;
		h_scale = hScale;
		spin_button = spinButton;
	}

	public static HScaleSpinButtonWidget New (double initialValue)
	{
		HScaleSpinButtonWidget widget = NewWithProperties ([]);
		widget.OnRealize += (_, _) => widget.Value = initialValue;
		widget.initial_value = initialValue;
		return widget;
	}

	public string Label {
		get => title_label.GetText ();
		set => title_label.SetText (value);
	}

	public int MaximumValue {
		get => max_value;
		set {
			max_value = value;
			h_scale.Adjustment!.Upper = value;
			spin_button.Adjustment!.Upper = value;
		}
	}

	public int MinimumValue {
		get => min_value;
		set {
			min_value = value;
			h_scale.Adjustment!.Lower = value;
			spin_button.Adjustment!.Lower = value;
		}
	}

	public int DigitsValue {
		get => digits_value;
		set {
			if (value <= 0) return;
			digits_value = value;
			h_scale.Digits = value;
			spin_button.Digits = Convert.ToUInt32 (value);
		}
	}

	public double IncrementValue {
		get => inc_value;
		set {
			inc_value = value;
			h_scale.Adjustment!.StepIncrement = value;
			spin_button.Adjustment!.StepIncrement = value;
		}
	}

	public int ValueAsInt => spin_button.GetValueAsInt ();

	public double Value {
		get => spin_button.Value;
		set {
			if (spin_button.Value == value) return;
			spin_button.Value = value;
			OnValueChanged ();
		}
	}

	private void HandleHscaleValueChanged (Gtk.Range sender, EventArgs args)
	{
		if (spin_button.Value == h_scale.GetValue ()) return;
		spin_button.Value = h_scale.GetValue ();
		OnValueChanged ();
	}

	private void HandleSpinValueChanged (Gtk.SpinButton sender, EventArgs e)
	{
		if (h_scale.GetValue () == spin_button.Value) return;
		h_scale.SetValue (spin_button.Value);
		OnValueChanged ();
	}

	private void HandleResetButtonClicked (Gtk.Button sender, EventArgs e)
	{
		Value = initial_value;
	}

	private void OnValueChanged ()
		=> ValueChanged?.Invoke (this, EventArgs.Empty);

	public event EventHandler? ValueChanged;
}
