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
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class AnglePickerWidget : Box
	{
		private AnglePickerGraphic anglepickergraphic1;
		private SpinButton spin;
		private Button button;
		private Label label;

		public AnglePickerWidget ()
		{
			Build ();

			anglepickergraphic1.ValueChanged += HandleAnglePickerValueChanged;
			spin.OnValueChanged += HandleSpinValueChanged;
			button.OnClicked += HandleButtonClicked;

			OnRealize += (_, _) => anglepickergraphic1.ValueDouble = DefaultValue;

			spin.SetActivatesDefault (true);
		}

		public double DefaultValue { get; set; }

		public string Label {
			get => label.GetText ();
			set => label.SetText (value);
		}

		public double Value {
			get => anglepickergraphic1.ValueDouble;
			set {
				if (anglepickergraphic1.ValueDouble != value) {
					anglepickergraphic1.ValueDouble = value;
					OnValueChanged ();
				}
			}
		}

		private void HandleAnglePickerValueChanged (object? sender, EventArgs e)
		{
			if (spin.Value != anglepickergraphic1.ValueDouble) {
				spin.Value = anglepickergraphic1.ValueDouble;
				OnValueChanged ();
			}
		}

		private void HandleSpinValueChanged (object? sender, EventArgs e)
		{
			if (anglepickergraphic1.ValueDouble != spin.Value) {
				anglepickergraphic1.ValueDouble = spin.Value;
				OnValueChanged ();
			}
		}

		private void HandleButtonClicked (object? sender, EventArgs e)
		{
			Value = DefaultValue;
		}

		protected void OnValueChanged () => ValueChanged?.Invoke (this, EventArgs.Empty);

		public event EventHandler? ValueChanged;

		[MemberNotNull (nameof (anglepickergraphic1), nameof (spin), nameof (button), nameof (label))]
		private void Build ()
		{
			const int spacing = 6;

			// Section label + line
			var hbox1 = new Box () { Spacing = spacing };
			hbox1.SetOrientation (Orientation.Horizontal);

			label = new Label ();
			label.AddCssClass (AdwaitaStyles.Title4);

			hbox1.Append (label);

			// Angle graphic + spinner + reset button
			var hbox2 = new Box () { Spacing = spacing };
			hbox2.SetOrientation (Orientation.Horizontal);

			anglepickergraphic1 = new AnglePickerGraphic {
				Hexpand = true,
				Halign = Align.Center
			};
			hbox2.Append (anglepickergraphic1);

			spin = SpinButton.NewWithRange (0, 360, 1);
			spin.CanFocus = true;
			spin.ClimbRate = 1;
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
		}
	}
}
