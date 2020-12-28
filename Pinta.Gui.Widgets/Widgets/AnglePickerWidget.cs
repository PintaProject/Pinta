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
	public class AnglePickerWidget : FilledAreaBin
	{
		private AnglePickerGraphic anglepickergraphic1;
		private SpinButton spin;
		private Button button;
		private Label label;

		public AnglePickerWidget ()
		{
			Build ();

			anglepickergraphic1.ValueChanged += HandleAnglePickerValueChanged;
			spin.ValueChanged += HandleSpinValueChanged;
			button.Clicked += HandleButtonClicked;

			spin.ActivatesDefault = true;
		}

		public double DefaultValue { get; set; }

		public string Label {
			get => label.Text;
			set => label.Text = value;
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

		protected override void OnShown ()
		{
			base.OnShown ();

			anglepickergraphic1.ValueDouble = DefaultValue;
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
			// Section label + line
			var hbox1 = new HBox (false, 6);

			label = new Label ();
			hbox1.PackStart (label, false, false, 0);
			hbox1.PackStart (new HSeparator (), true, true, 0);

			// Angle graphic + spinner + reset button
			var hbox2 = new HBox (false, 6);

			anglepickergraphic1 = new AnglePickerGraphic ();
			hbox2.PackStart (anglepickergraphic1, true, false, 0);

			spin = new SpinButton (0, 360, 1) {
				CanFocus = true,
				ClimbRate = 1,
				Numeric = true
			};

			spin.Adjustment.PageIncrement = 10;

			var alignment = new Alignment (0.5F, 0F, 1F, 0F) {
				spin
			};

			hbox2.PackStart (alignment, false, false, 0);

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
