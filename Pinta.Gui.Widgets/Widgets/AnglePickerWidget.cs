/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class AnglePickerWidget : Gtk.Box
{
	private readonly AnglePickerGraphic angle_picker_graphic;
	private readonly Gtk.SpinButton numeric_spin;
	private readonly Gtk.Label widget_label;
	private readonly DegreesAngle initial_angle;

	public AnglePickerWidget (DegreesAngle initialAngle)
	{
		const int SPACING = 6;

		Gtk.Label widgetLabel = new ();
		widgetLabel.AddCssClass (AdwaitaStyles.Title4);

		Gtk.Box labelBox = new () { Spacing = SPACING };
		labelBox.SetOrientation (Gtk.Orientation.Horizontal);
		labelBox.Append (widgetLabel);

		AnglePickerGraphic anglePickerGraphic = new () {
			Hexpand = true,
			Halign = Gtk.Align.Center,
		};
		anglePickerGraphic.ValueChanged += HandleAnglePickerValueChanged;

		Gtk.SpinButton numericSpin = Gtk.SpinButton.NewWithRange (-360, 360, 1);
		numericSpin.Configure (numericSpin.GetAdjustment (), 1, 2);
		numericSpin.CanFocus = true;
		numericSpin.Numeric = true;
		numericSpin.Adjustment!.PageIncrement = 10;
		numericSpin.Valign = Gtk.Align.Start;
		numericSpin.OnValueChanged += HandleSpinValueChanged;
		numericSpin.SetActivatesDefaultImmediate (true);
		numericSpin.Wrap = true;

		Gtk.Button resetButton = new () {
			IconName = Resources.StandardIcons.GoPrevious,
			WidthRequest = 28,
			HeightRequest = 24,
			CanFocus = true,
			UseUnderline = true,
			Valign = Gtk.Align.Start,
		};
		resetButton.OnClicked += HandleButtonClicked;

		Gtk.Box controlsBox = new () { Spacing = SPACING };
		controlsBox.SetOrientation (Gtk.Orientation.Horizontal);
		controlsBox.Append (anglePickerGraphic);
		controlsBox.Append (numericSpin);
		controlsBox.Append (resetButton);

		// --- Initialization (Gtk.Widget)

		OnRealize += (_, _) => anglePickerGraphic.Value = initialAngle;

		// --- Initialization (Gtk.Box)

		SetOrientation (Gtk.Orientation.Vertical);
		Spacing = SPACING;
		Append (labelBox); // Section label + line
		Append (controlsBox); // Angle graphic + spinner + reset button

		// --- References to keep

		initial_angle = initialAngle;
		widget_label = widgetLabel;
		angle_picker_graphic = anglePickerGraphic;
		numeric_spin = numericSpin;
	}

	public string Label {
		get => widget_label.GetText ();
		set => widget_label.SetText (value);
	}

	public DegreesAngle Value {
		get => angle_picker_graphic.Value;
		set {
			if (angle_picker_graphic.Value == value) return;
			angle_picker_graphic.Value = value;
			OnValueChanged ();
		}
	}

	private void HandleAnglePickerValueChanged (object? sender, EventArgs e)
	{
		if (numeric_spin.Value == angle_picker_graphic.Value.Degrees) return;
		numeric_spin.Value = angle_picker_graphic.Value.Degrees;
		OnValueChanged ();
	}

	private void HandleSpinValueChanged (object? sender, EventArgs e)
	{
		if (angle_picker_graphic.Value.Degrees == numeric_spin.Value) return;
		angle_picker_graphic.Value = new DegreesAngle (numeric_spin.Value);
		OnValueChanged ();
	}

	private void HandleButtonClicked (object? sender, EventArgs e)
	{
		Value = initial_angle;
	}

	private void OnValueChanged () => ValueChanged?.Invoke (this, EventArgs.Empty);

	public event EventHandler? ValueChanged;
}
