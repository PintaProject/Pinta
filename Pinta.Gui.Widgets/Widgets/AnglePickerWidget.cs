/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics.CodeAnalysis;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

[GObject.Subclass<Gtk.Box>]
public sealed partial class AnglePickerWidget
{
	private AnglePickerGraphic angle_picker_graphic;
	private Gtk.SpinButton numeric_spin;
	private Gtk.Label widget_label;
	private DegreesAngle initial_angle;

	public static AnglePickerWidget NewWithAngle (DegreesAngle initialAngle)
	{
		AnglePickerWidget widget = NewWithProperties ([]);
		widget.initial_angle = initialAngle;
		widget.Value = initialAngle;
		return widget;
	}

	[MemberNotNull (nameof (angle_picker_graphic))]
	[MemberNotNull (nameof (numeric_spin))]
	[MemberNotNull (nameof (widget_label))]
	partial void Initialize ()
	{
		const int SPACING = 6;

		Gtk.Label widgetLabel = Gtk.Label.New (null);
		widgetLabel.AddCssClass (AdwaitaStyles.Title4);

		Gtk.Box labelBox = Gtk.Box.New (Gtk.Orientation.Horizontal, SPACING);
		labelBox.Append (widgetLabel);

		AnglePickerGraphic anglePickerGraphic = AnglePickerGraphic.New ();
		anglePickerGraphic.Halign = Gtk.Align.Center;
		anglePickerGraphic.Hexpand = true;
		anglePickerGraphic.ValueChanged += HandleAnglePickerValueChanged;

		Gtk.SpinButton numericSpin = Gtk.SpinButton.NewWithRange (-360, 360, 1);
		numericSpin.Value = 0;
		numericSpin.Configure (numericSpin.GetAdjustment (), 1, 2);
		numericSpin.CanFocus = true;
		numericSpin.Numeric = true;
		numericSpin.Adjustment!.PageIncrement = 10;
		numericSpin.Valign = Gtk.Align.Start;
		numericSpin.OnValueChanged += HandleSpinValueChanged;
		numericSpin.SetActivatesDefaultImmediate (true);

		Gtk.Button resetButton = Gtk.Button.NewFromIconName (Resources.StandardIcons.GoPrevious);
		resetButton.WidthRequest = 28;
		resetButton.HeightRequest = 24;
		resetButton.CanFocus = true;
		resetButton.UseUnderline = true;
		resetButton.Valign = Gtk.Align.Start;
		resetButton.OnClicked += HandleButtonClicked;

		Gtk.Box controlsBox = Gtk.Box.New (Gtk.Orientation.Horizontal, SPACING);
		controlsBox.Append (anglePickerGraphic);
		controlsBox.Append (numericSpin);
		controlsBox.Append (resetButton);

		// --- Initialization (Gtk.Box)

		SetOrientation (Gtk.Orientation.Vertical);
		Spacing = SPACING;
		Append (labelBox); // Section label + line
		Append (controlsBox); // Angle graphic + spinner + reset button

		// --- References to keep

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
