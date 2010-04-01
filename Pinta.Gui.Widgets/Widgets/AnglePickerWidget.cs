// 
// AnglePickerWidget.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
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
