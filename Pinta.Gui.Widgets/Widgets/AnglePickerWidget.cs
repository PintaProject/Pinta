/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;

namespace Pinta.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class AnglePickerWidget : Gtk.Bin
	{
		public AnglePickerWidget ()
		{
			this.Build ();

			anglepickergraphic1.ValueChanged += HandleAnglePickerValueChanged;
			spin.ValueChanged += HandleSpinValueChanged;
			button.Clicked += HandleButtonClicked;
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
	}
}
