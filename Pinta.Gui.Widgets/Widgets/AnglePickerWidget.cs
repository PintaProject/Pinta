/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class AnglePickerWidget : Box
{
	private readonly AnglePickerGraphic anglepickergraphic1;
	private readonly SpinButton spin;
	private readonly Button button;
	private readonly Label label;

	public AnglePickerWidget ()
	{
		const int spacing = 6;

		// Section label + line
		var hbox1 = new Box { Spacing = spacing };
		hbox1.SetOrientation (Orientation.Horizontal);

		label = new Label ();
		label.AddCssClass (AdwaitaStyles.Title4);

		hbox1.Append (label);

		// Angle graphic + spinner + reset button
		var hbox2 = new Box { Spacing = spacing };
		hbox2.SetOrientation (Orientation.Horizontal);

		anglepickergraphic1 = new AnglePickerGraphic {
			Hexpand = true,
			Halign = Align.Center
		};
		hbox2.Append (anglepickergraphic1);

		spin = SpinButton.NewWithRange (0, 360, 1);
		spin.Configure (spin.GetAdjustment (), 1, 2);
		spin.CanFocus = true;
		spin.Numeric = true;
		spin.Adjustment!.PageIncrement = 10;
		spin.Valign = Align.Start;

		hbox2.Append (spin);

		// Reset button
		button = new Button {
			IconName = Resources.StandardIcons.GoPrevious,
			WidthRequest = 28,
			HeightRequest = 24,
			CanFocus = true,
			UseUnderline = true,
			Valign = Align.Start
		};
		hbox2.Append (button);

		// Main layout
		SetOrientation (Orientation.Vertical);
		Spacing = spacing;
		Append (hbox1);
		Append (hbox2);

		anglepickergraphic1.ValueChanged += HandleAnglePickerValueChanged;
		spin.OnValueChanged += HandleSpinValueChanged;
		button.OnClicked += HandleButtonClicked;

		OnRealize += (_, _) => anglepickergraphic1.Value = DefaultValue;

		spin.SetActivatesDefault (true);
	}

	public DegreesAngle DefaultValue { get; set; }

	public string Label {
		get => label.GetText ();
		set => label.SetText (value);
	}

	public DegreesAngle Value {
		get => anglepickergraphic1.Value;
		set {
			if (anglepickergraphic1.Value == value)
				return;

			anglepickergraphic1.Value = value;
			OnValueChanged ();
		}
	}

	private void HandleAnglePickerValueChanged (object? sender, EventArgs e)
	{
		if (spin.Value == anglepickergraphic1.Value.Degrees)
			return;

		spin.Value = anglepickergraphic1.Value.Degrees;
		OnValueChanged ();
	}

	private void HandleSpinValueChanged (object? sender, EventArgs e)
	{
		if (anglepickergraphic1.Value.Degrees == spin.Value)
			return;

		anglepickergraphic1.Value = new DegreesAngle (spin.Value);
		OnValueChanged ();
	}

	private void HandleButtonClicked (object? sender, EventArgs e)
	{
		Value = DefaultValue;
	}

	private void OnValueChanged () => ValueChanged?.Invoke (this, EventArgs.Empty);

	public event EventHandler? ValueChanged;
}
