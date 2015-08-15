// 
// PointPicker.cs
//  
// Author:
//       Olivier Dufour <olivier.duff@gmail.com>
// 
// Copyright (c) 2010 Olivier Dufour
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
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem(true)]
	public class PointPickerWidget : FilledAreaBin
	{
        private Label label;
        private PointPickerGraphic pointpickergraphic1;
        private Button button1;
        private Button button2;
        private SpinButton spinX;
        private SpinButton spinY;

        bool active = true; 
			
		[Category("Custom Properties")]
		public string Label {
			get { return label.Text; }
			set { label.Text = value; }
		}

		[Category("Custom Properties")]
		public Gdk.Point DefaultPoint { get; set; }

		[Category("Custom Properties")]
		public Gdk.Point Point {
			get { return new Gdk.Point (spinX.ValueAsInt, spinY.ValueAsInt); }
			set {
				if (value.X != spinX.ValueAsInt || value.Y != spinY.ValueAsInt) {
					spinX.Value = value.X;
					spinY.Value = value.Y;
					OnPointPicked ();
				}
			}
		}

		[Category("Custom Properties")]
		public Cairo.PointD DefaultOffset { 
			get { return new Cairo.PointD ( (DefaultPoint.X * 2.0 /PintaCore.Workspace.ImageSize.Width) - 1.0,
											(DefaultPoint.Y * 2.0 / PintaCore.Workspace.ImageSize.Height) - 1.0);}
			set {DefaultPoint = new Gdk.Point ( (int) ((value.X + 1.0) * PintaCore.Workspace.ImageSize.Width / 2.0 ),
				                                (int) ((value.Y + 1.0) * PintaCore.Workspace.ImageSize.Height / 2.0 ) );} 
		}

		public Cairo.PointD Offset {
			get { return new Cairo.PointD ((spinX.Value * 2.0 / PintaCore.Workspace.ImageSize.Width) - 1.0, (spinY.Value * 2.0 / PintaCore.Workspace.ImageSize.Height) - 1.0); }
		}

		public PointPickerWidget ()
		{
			Build ();

			spinX.Adjustment.Upper = PintaCore.Workspace.ImageSize.Width;
			spinY.Adjustment.Upper = PintaCore.Workspace.ImageSize.Height;
			spinX.Adjustment.Lower = 0;
			spinY.Adjustment.Lower = 0;

			spinX.ActivatesDefault = true;
			spinY.ActivatesDefault = true;
		}

		void HandlePointpickergraphic1PositionChanged (object sender, EventArgs e)
		{
			if (Point != pointpickergraphic1.Position) {
				active = false;
				spinX.Value = pointpickergraphic1.Position.X;
				spinY.Value = pointpickergraphic1.Position.Y;
				active = true;
				OnPointPicked ();
			}
		}
		
		private void HandleSpinXValueChanged (object sender, EventArgs e)
		{
			if (active) {
				pointpickergraphic1.Position = Point; 
				OnPointPicked ();
			}
		}
		
		private void HandleSpinYValueChanged (object sender, EventArgs e)
		{
			if (active) {
				pointpickergraphic1.Position = Point;
				OnPointPicked ();
			}
		}
		
		protected override void OnShown ()
		{
			base.OnShown ();
			Point = DefaultPoint;
			
			spinX.ValueChanged += HandleSpinXValueChanged;
			spinY.ValueChanged += HandleSpinYValueChanged;
			pointpickergraphic1.PositionChanged += HandlePointpickergraphic1PositionChanged;
			button1.Pressed += HandleButton1Pressed;
			button2.Pressed += HandleButton2Pressed;
			
			pointpickergraphic1.Init (DefaultPoint);
		}

		void HandleButton1Pressed (object sender, EventArgs e)
		{
			spinX.Value = DefaultPoint.X;
		}

		void HandleButton2Pressed (object sender, EventArgs e)
		{
			spinY.Value = DefaultPoint.Y;
		}
		
		#region Protected Methods
		protected void OnPointPicked ()
		{
			if (PointPicked != null)
				PointPicked (this, EventArgs.Empty);
		}
		#endregion

		#region Public Events
		public event EventHandler PointPicked;
        #endregion

        private void Build ()
        {
            // Section label + line
            var hbox1 = new HBox (false, 6);

            label = new Label ();
            hbox1.PackStart (label, false, false, 0);
            hbox1.PackStart (new HSeparator (), true, true, 0);

            // PointPickerGraphic
            var hbox2 = new HBox (false, 6);

            pointpickergraphic1 = new PointPickerGraphic ();
            hbox2.PackStart (pointpickergraphic1, true, false, 0);

            // X spinner
            var label2 = new Label ();
            label2.LabelProp = "X:";

            spinX = new SpinButton (0, 100, 1);
            spinX.CanFocus = true;
            spinX.Adjustment.PageIncrement = 10;
            spinX.ClimbRate = 1;
            spinX.Numeric = true;

            button1 = new Button ();
            button1.WidthRequest = 28;
            button1.HeightRequest = 24;
            button1.CanFocus = true;
            button1.UseUnderline = true;

            var button_image = new Image (PintaCore.Resources.GetIcon (Stock.GoBack, 16));
            button1.Add (button_image);

            var alignment1 = new Alignment (0.5F, 0.5F, 1F, 0F);
            alignment1.Add (button1);

            var x_hbox = new HBox (false, 6);

            x_hbox.PackStart (label2, false, false, 0);
            x_hbox.PackStart (spinX, false, false, 0);
            x_hbox.PackStart (alignment1, false, false, 0);

            // Y spinner
            var label3 = new Label ();
            label3.LabelProp = "Y:";

            spinY = new SpinButton (0, 100, 1);
            spinY.CanFocus = true;
            spinY.Adjustment.PageIncrement = 10;
            spinY.ClimbRate = 1;
            spinY.Numeric = true;

            button2 = new Button ();
            button2.WidthRequest = 28;
            button2.HeightRequest = 24;
            button2.CanFocus = true;
            button2.UseUnderline = true;

            var button_image2 = new Image (PintaCore.Resources.GetIcon (Stock.GoBack, 16));
            button2.Add (button_image2);

            var alignment2 = new Alignment (0.5F, 0.5F, 1F, 0F);
            alignment2.Add (button2);

            var y_hbox = new HBox (false, 6);

            y_hbox.PackStart (label3, false, false, 0);
            y_hbox.PackStart (spinY, false, false, 0);
            y_hbox.PackStart (alignment2, false, false, 0);

            // Vbox for spinners
            var spin_vbox = new VBox ();

            spin_vbox.Add (x_hbox);
            spin_vbox.Add (y_hbox);

            hbox2.PackStart (spin_vbox, false, false, 0);

            // Main layout
            var vbox = new VBox (false, 6);

            vbox.Add (hbox1);
            vbox.Add (hbox2);

            Add (vbox);

            vbox.ShowAll ();
        }
    }
}
