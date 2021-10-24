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
using System.Diagnostics.CodeAnalysis;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class PointPickerWidget : FilledAreaBin
	{
		private Label label;
		private PointPickerGraphic pointpickergraphic1;
		private Button button1;
		private Button button2;
		private SpinButton spin_x;
		private SpinButton spin_y;

		bool active = true;

		public PointPickerWidget ()
		{
			Build ();

			spin_x.Adjustment.Upper = PintaCore.Workspace.ImageSize.Width;
			spin_y.Adjustment.Upper = PintaCore.Workspace.ImageSize.Height;
			spin_x.Adjustment.Lower = 0;
			spin_y.Adjustment.Lower = 0;

			spin_x.ActivatesDefault = true;
			spin_y.ActivatesDefault = true;
		}

		public string Label {
			get => label.Text;
			set => label.Text = value;
		}

		public Gdk.Point DefaultPoint { get; set; }

		public Gdk.Point Point {
			get => new Gdk.Point (spin_x.ValueAsInt, spin_y.ValueAsInt);
			set {
				if (value.X != spin_x.ValueAsInt || value.Y != spin_y.ValueAsInt) {
					spin_x.Value = value.X;
					spin_y.Value = value.Y;
					OnPointPicked ();
				}
			}
		}

		public Cairo.PointD DefaultOffset {
			get {
				return new Cairo.PointD ((DefaultPoint.X * 2.0 / PintaCore.Workspace.ImageSize.Width) - 1.0,
							 (DefaultPoint.Y * 2.0 / PintaCore.Workspace.ImageSize.Height) - 1.0);
			}
			set {
				DefaultPoint = new Gdk.Point ((int) ((value.X + 1.0) * PintaCore.Workspace.ImageSize.Width / 2.0),
							      (int) ((value.Y + 1.0) * PintaCore.Workspace.ImageSize.Height / 2.0));
			}
		}

		public Cairo.PointD Offset
			=> new Cairo.PointD ((spin_x.Value * 2.0 / PintaCore.Workspace.ImageSize.Width) - 1.0, (spin_y.Value * 2.0 / PintaCore.Workspace.ImageSize.Height) - 1.0);

		private void HandlePointpickergraphic1PositionChanged (object? sender, EventArgs e)
		{
			if (Point != pointpickergraphic1.Position) {
				active = false;
				spin_x.Value = pointpickergraphic1.Position.X;
				spin_y.Value = pointpickergraphic1.Position.Y;
				active = true;
				OnPointPicked ();
			}
		}

		private void HandleSpinXValueChanged (object? sender, EventArgs e)
		{
			if (active) {
				pointpickergraphic1.Position = Point;
				OnPointPicked ();
			}
		}

		private void HandleSpinYValueChanged (object? sender, EventArgs e)
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

			spin_x.ValueChanged += HandleSpinXValueChanged;
			spin_y.ValueChanged += HandleSpinYValueChanged;
			pointpickergraphic1.PositionChanged += HandlePointpickergraphic1PositionChanged;
			button1.Pressed += HandleButton1Pressed;
			button2.Pressed += HandleButton2Pressed;

			pointpickergraphic1.Init (DefaultPoint);
		}

		private void HandleButton1Pressed (object? sender, EventArgs e)
		{
			spin_x.Value = DefaultPoint.X;
		}

		private void HandleButton2Pressed (object? sender, EventArgs e)
		{
			spin_y.Value = DefaultPoint.Y;
		}

		protected void OnPointPicked () => PointPicked?.Invoke (this, EventArgs.Empty);

		public event EventHandler? PointPicked;

		[MemberNotNull (nameof (label), nameof (spin_x), nameof (spin_y), nameof (button1), nameof (button2), nameof (pointpickergraphic1))]
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
			var label2 = new Label {
				LabelProp = "X:"
			};

			spin_x = new SpinButton (0, 100, 1) {
				CanFocus = true,
				ClimbRate = 1,
				Numeric = true
			};
			spin_x.Adjustment.PageIncrement = 10;

			button1 = new Button {
				WidthRequest = 28,
				HeightRequest = 24,
				CanFocus = true,
				UseUnderline = true
			};

			var button_image = new Image (IconTheme.Default.LoadIcon (Resources.StandardIcons.GoPrevious, 16));
			button1.Add (button_image);

			var alignment1 = new Alignment (0.5F, 0.5F, 1F, 0F) {
				button1
			};

			var x_hbox = new HBox (false, 6);

			x_hbox.PackStart (label2, false, false, 0);
			x_hbox.PackStart (spin_x, false, false, 0);
			x_hbox.PackStart (alignment1, false, false, 0);

			// Y spinner
			var label3 = new Label {
				LabelProp = "Y:"
			};

			spin_y = new SpinButton (0, 100, 1) {
				CanFocus = true,
				ClimbRate = 1,
				Numeric = true
			};
			spin_y.Adjustment.PageIncrement = 10;

			button2 = new Button {
				WidthRequest = 28,
				HeightRequest = 24,
				CanFocus = true,
				UseUnderline = true
			};

			var button_image2 = new Image (Gtk.IconTheme.Default.LoadIcon (Resources.StandardIcons.GoPrevious, 16));
			button2.Add (button_image2);

			var alignment2 = new Alignment (0.5F, 0.5F, 1F, 0F) {
				button2
			};

			var y_hbox = new HBox (false, 6);

			y_hbox.PackStart (label3, false, false, 0);
			y_hbox.PackStart (spin_y, false, false, 0);
			y_hbox.PackStart (alignment2, false, false, 0);

			// Vbox for spinners
			var spin_vbox = new VBox {
				x_hbox,
				y_hbox
			};

			hbox2.PackStart (spin_vbox, false, false, 0);

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
