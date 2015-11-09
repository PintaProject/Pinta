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

namespace Pinta.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem (true)]
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

		#region Public Properties
		public double DefaultValue { get; set; }

		public string Label {
			get { return label.Text; }
			set { label.Text = value; }
		}

		public double Value {
			get { return anglepickergraphic1.ValueDouble; }
			set {
				if (anglepickergraphic1.ValueDouble != value) {
					anglepickergraphic1.ValueDouble = value;
					OnValueChanged ();
				}
			}
		}
		#endregion

		#region Event Handlers
		protected override void OnShown ()
		{
			base.OnShown ();

			anglepickergraphic1.ValueDouble = DefaultValue;
		}

		private void HandleAnglePickerValueChanged (object sender, EventArgs e)
		{
			if (spin.Value != anglepickergraphic1.ValueDouble) {
				spin.Value = anglepickergraphic1.ValueDouble;
				OnValueChanged ();
			}
		}

		private void HandleSpinValueChanged (object sender, EventArgs e)
		{
			if (anglepickergraphic1.ValueDouble != spin.Value) {
				anglepickergraphic1.ValueDouble = spin.Value;
				OnValueChanged ();
			}
		}

		private void HandleButtonClicked (object sender, EventArgs e)
		{
			Value = DefaultValue;
		}
		#endregion

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

            // Angle graphic + spinner + reset button
            var hbox2 = new HBox (false, 6);

            anglepickergraphic1 = new AnglePickerGraphic ();
            hbox2.PackStart (anglepickergraphic1, true, false, 0);

            spin = new SpinButton (0, 360, 1);
            spin.CanFocus = true;
            spin.Adjustment.PageIncrement = 10;
            spin.ClimbRate = 1;
            spin.Numeric = true;

            var alignment = new Alignment (0.5F, 0F, 1F, 0F);
            alignment.Add (spin);
            hbox2.PackStart (alignment, false, false, 0);

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
